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
    public class RuleImporter : IRuleImporter
    {
        public Grammar ImportRule(Stream inputStream)
        {
            var xml = XDocument.Load(inputStream);

            return new RuleBuilder(xml).RuleMap;
        }

        public async Task<Grammar> ImportRuleAsync(Stream inputStream)
        {
            var xml = await XDocument.LoadAsync(
                inputStream,
                LoadOptions.None,
                CancellationToken.None);

            return new RuleBuilder(xml).RuleMap;
        }
    }

    internal class RuleBuilder
    {

        public Grammar RuleMap { get; }

        public RuleBuilder(XDocument document)
        {
            RuleMap = new Grammar();
            ValidateDocument(document);
            ImportLanguage(document.Root);
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

        internal void ImportLanguage(XElement rootElement)
        {
            var rootSymbol = rootElement.Attribute("root").Value;

            rootElement
                .Elements()
                .Select(ToProductionMap)
                .ForAll(map => RuleMap.AddRule(
                    map.Key,
                    map.Value,
                    map.Key.Equals(rootSymbol, StringComparison.InvariantCulture)));

            RuleMap.Validate();
        }

        internal KeyValuePair<string, IRule> ToProductionMap(XElement element)
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

        internal IRule ToRule(XElement element)
        {
            return element.Name.LocalName switch
            {
                "sequence" => SymbolExpressionRule.Sequence(
                    ExtractCardinality(element),
                    element.Elements().Select(ToRule).ToArray()),

                "set" => SymbolExpressionRule.Set(
                    ExtractCardinality(element),
                    element.Elements().Select(ToRule).ToArray()),

                "choice" => SymbolExpressionRule.Choice(
                    ExtractCardinality(element),
                    element.Elements().Select(ToRule).ToArray()),

                "pattern" => new PatternRule(
                    ExtractPatternRegex(element),
                    ExtractMatchCardinality(element)),

                "literal" => new LiteralRule(
                    element.Attribute(Legend.Enumerations.LiteralElement_Value).Value,
                    ExtractCaseSensitivity(element)),

                "symbol" => new RuleRef(
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
                .GetManifestResourceStream($"{typeof(RuleBuilder).Namespace}.RuleDefinition.xsd");
        }
    }
}
