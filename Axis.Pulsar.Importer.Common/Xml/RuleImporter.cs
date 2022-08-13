using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Grammar;
using Axis.Pulsar.Parser.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Axis.Pulsar.Importer.Common.Xml
{
    public class GrammarImporter : IGrammarImporter
    {
        public Grammar ImportGrammar(Stream inputStream)
        {
            var xml = XDocument.Load(inputStream);

            return XmlBuilder.CreateGrammar(xml);
        }

        public async Task<Grammar> ImportGrammarAsync(Stream inputStream)
        {
            var xml = await XDocument.LoadAsync(
                inputStream,
                LoadOptions.None,
                CancellationToken.None);

            return XmlBuilder.CreateGrammar(xml);
        }
    }

    internal static class XmlBuilder
    {

        public static Grammar CreateGrammar(XDocument document)
        {
            ValidateDocument(document);
            return ImportLanguage(document.Root);
        }

        internal static void ValidateDocument(XDocument xml)
        {
            var schemas = new XmlSchemaSet();
            schemas.Add("", XmlReader.Create(GetXsdResourceStream()));

            var errors = new List<XmlImporterException.Info>();
            xml.Validate(schemas, (source, args) => errors.Add(new(
                args.Message,
                args.Exception,
                args.Severity)));

            if (errors.Count > 0)
                throw new XmlImporterException(errors.ToArray());
        }

        internal static Grammar ImportLanguage(XElement rootElement)
        {
            var rootSymbol = rootElement.Attribute("root").Value;

            return rootElement
                .Elements()
                .Select(ToProduction)
                .Aggregate(
                    GrammarBuilder.NewBuilder(),
                    (builder, production) => builder.HasRoot
                        ? builder.WithProduction(production.Symbol, production.Rule)
                        : builder.WithRootProduction(production.Symbol, production.Rule))
                .Build();
        }

        internal static Production ToProduction(XElement element)
        {
            var name = element.Attribute("name").Value;
            var rule = element.Name.LocalName switch
            {
                "non-terminal" => ToRule(element.FirstChild()),
                "literal" => ToRule(element),
                "pattern" => ToRule(element),
                _ => throw new Exception($"Invalid element: {element.Name}")
            };

            return new(name, rule);
        }

        internal static IRule ToRule(XElement element)
        {
            return element.Name.LocalName switch
            {
                "pattern" => new PatternRule(
                    ExtractPatternRegex(element),
                    ExtractMatchCardinality(element)),

                "literal" => new LiteralRule(
                    element.Attribute(Legend.Enumerations.LiteralElement_Value).Value,
                    ExtractCaseSensitivity(element)),

                _ => new SymbolExpressionRule(ToExpression(element))
            };
        }

        internal static ISymbolExpression ToExpression(XElement element)
        {
            return element.Name.LocalName switch
            {
                "sequence" => SymbolGroup.Sequence(
                    ExtractCardinality(element),
                    element.Elements().Select(ToExpression).ToArray()),

                "set" => SymbolGroup.Set(
                    ExtractCardinality(element),
                    element.Elements().Select(ToExpression).ToArray()),

                "choice" => SymbolGroup.Choice(
                    ExtractCardinality(element),
                    element.Elements().Select(ToExpression).ToArray()),

                "symbol" => new ProductionRef(
                    element.Attribute(Legend.Enumerations.SymbolElement_Name).Value,
                    ExtractCardinality(element)),

                _ => throw new ArgumentException($"Invalid element: {element.Name}")
            };
        }


        public static Cardinality ExtractMatchCardinality(XElement patternElement)
        {
            var minOccurs = patternElement.Attribute(Legend.Enumerations.PatternElement_MinMatch)?.Value;
            var maxOccurs = patternElement.Attribute(Legend.Enumerations.PatternElement_MaxMatch)?.Value;

            if (minOccurs == null && maxOccurs == null)
                return Cardinality.OccursOnlyOnce();

            else
            {
                return new Cardinality(
                    int.Parse(minOccurs ?? "1"),
                    maxOccurs == null ? null :
                    maxOccurs.Equals("unbounded") ? null :
                    int.Parse(maxOccurs));
            }
        }

        public static Cardinality ExtractCardinality(XElement element)
        {
            var minOccurs = element.Attribute(Legend.Enumerations.ProductionElement_MinOccurs)?.Value;
            var maxOccurs = element.Attribute(Legend.Enumerations.ProductionElement_MaxOccurs)?.Value;

            if (minOccurs == null && maxOccurs == null)
                return Cardinality.OccursOnlyOnce();

            else
            {
                return new Cardinality(
                    int.Parse(minOccurs ?? "1"),
                    maxOccurs == null ? null :
                    maxOccurs.Equals("unbounded") ? null :
                    int.Parse(maxOccurs));
            }
        }

        public static Regex ExtractPatternRegex(XElement patternElement)
        {
            var regexPattern = patternElement.Attribute(Legend.Enumerations.PatternElement_Regex).Value;
            var options =
                !patternElement.TryAttribute(Legend.Enumerations.PatternElement_CaseSensitive, out var att) ? RegexOptions.IgnoreCase
                : !bool.Parse(att.Value?.ToLower()) ? RegexOptions.IgnoreCase
                : RegexOptions.None;

            return new Regex(regexPattern, options);
        }

        public static bool ExtractCaseSensitivity(XElement element)
        {
            return element.TryAttribute(Legend.Enumerations.LiteralElement_CaseSensitive, out var att)
                && bool.Parse(att.Value?.ToLower());
        }



        private static Stream GetXsdResourceStream()
        {
            return Assembly
                .GetExecutingAssembly()
                .GetManifestResourceStream($"{typeof(XmlBuilder).Namespace}.RuleDefinition.xsd");
        }
    }
}
