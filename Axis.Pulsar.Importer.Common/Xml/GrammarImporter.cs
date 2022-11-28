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
using Axis.Pulsar.Parser.Builders;
using Axis.Pulsar.Parser;

namespace Axis.Pulsar.Importer.Common.Xml
{
    public class GrammarImporter : IGrammarImporter
    {
        public IGrammar ImportGrammar(Stream inputStream, Dictionary<string, IRuleValidator<IRule>> validators = null)
        {
            var xml = XDocument.Load(inputStream);

            return XmlBuilder.CreateGrammar(xml, validators);
        }

        public async Task<IGrammar> ImportGrammarAsync(Stream inputStream, Dictionary<string, IRuleValidator<IRule>> validators = null)
        {
            var xml = await XDocument.LoadAsync(
                inputStream,
                LoadOptions.None,
                CancellationToken.None);

            return XmlBuilder.CreateGrammar(xml, validators);
        }
    }

    internal static class XmlBuilder
    {
        public static IGrammar CreateGrammar(XDocument document, Dictionary<string, IRuleValidator<IRule>> validators = null)
        {
            ValidateDocument(document);
            return ImportLanguage(document.Root, validators);
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

        internal static IGrammar ImportLanguage(XElement rootElement, Dictionary<string, IRuleValidator<IRule>> validators = null)
        {
            return rootElement
                .Elements()
                .Select(element => ToProduction(element, validators))
                .Aggregate(
                    GrammarBuilder.NewBuilder(),
                    (builder, production) => builder.WithProduction(production.Symbol, production.Rule))
                .WithRoot(rootElement.Attribute("root").Value)
                .Build();
        }

        internal static Production ToProduction(XElement element, Dictionary<string, IRuleValidator<IRule>> validators = null)
        {
            var name = element.Attribute("name").Value;
            var rule = element.Name.LocalName switch
            {
                "non-terminal" => ToRule(element, validators),
                "literal" => ToRule(element, validators),
                "open-pattern" => ToRule(element, validators),
                "closed-pattern" => ToRule(element, validators),
                _ => throw new Exception($"Invalid element: {element.Name}")
            };

            return new(name, rule);
        }

        internal static IRule ToRule(XElement element, Dictionary<string, IRuleValidator<IRule>> validators = null)
        {
            var ruleName = element.Attribute("name").Value;
            return element.Name.LocalName switch
            {
                "open-pattern" => new PatternRule(
                    ExtractPatternRegex(element),
                    ExtractOpenMatchType(element),
                    validators?.TryGetValue(ruleName, out var validator) == true ? validator : null),

                "closed-pattern" => new PatternRule(
                    ExtractPatternRegex(element),
                    ExtractClosedMatchType(element),
                    validators?.TryGetValue(ruleName, out var validator) == true ? validator : null),

                "literal" => new LiteralRule(
                    element.Attribute(Legend.Enumerations.LiteralElement_Value).Value.ApplyEscape(),
                    ExtractCaseSensitivity(element),
                    validators?.TryGetValue(ruleName, out var validator) == true ? validator : null),

                _ => new SymbolExpressionRule(
                    ToExpression(element.FirstChild()),
                    ExtractThreshold(element),
                    validators?.TryGetValue(ruleName, out var validator) == true ? validator : null)
            };
        }

        internal static ISymbolExpression ToExpression(XElement element)
        {
            return element.Name.LocalName switch
            {
                "sequence" => new SymbolGroup.Sequence(
                    ExtractCardinality(element),
                    element.Elements().Select(ToExpression).ToArray()),

                "set" => new SymbolGroup.Set(
                    ExtractCardinality(element),
                    element.TryAttribute(Legend.Enumerations.SetElement_MaxContentCount, out var att)
                        ? int.Parse(att.Value)
                        : null,
                    element.Elements().Select(ToExpression).ToArray()),

                "choice" => new SymbolGroup.Choice(
                    ExtractCardinality(element),
                    element.Elements().Select(ToExpression).ToArray()),

                "symbol" => new ProductionRef(
                    element.Attribute(Legend.Enumerations.SymbolElement_Name).Value,
                    ExtractCardinality(element)),

                "eof" => new EOF(),

                _ => throw new ArgumentException($"Invalid element: {element.Name}")
            };
        }

        internal static IPatternMatchType.Closed ExtractClosedMatchType(XElement patternElement)
        {
            var minMatch = patternElement.Attribute(Legend.Enumerations.PatternElement_MinMatch)?.Value;
            var maxMatch = patternElement.Attribute(Legend.Enumerations.PatternElement_MaxMatch)?.Value;

            return new IPatternMatchType.Closed(
                int.Parse(minMatch ?? "1"),
                int.Parse(maxMatch ?? "1"));
        }

        internal static IPatternMatchType.Open ExtractOpenMatchType(XElement patternElement)
        {
            var allowsEmpty = patternElement.Attribute(Legend.Enumerations.PatternElement_AllowsEmpty)?.Value;
            var maxMismatch = patternElement.Attribute(Legend.Enumerations.PatternElement_MaxMismatch)?.Value;

            return new IPatternMatchType.Open(
                int.Parse(maxMismatch ?? "1"),
                bool.Parse(allowsEmpty ?? "false"));
        }

        internal static Cardinality ExtractCardinality(XElement element)
        {
            var minOccurs = element.Attribute(Legend.Enumerations.ProductionElement_MinOccurs)?.Value;
            var maxOccurs = element.Attribute(Legend.Enumerations.ProductionElement_MaxOccurs)?.Value;

            if (minOccurs == null && maxOccurs == null)
                return Cardinality.OccursOnlyOnce();

            else
            {
                return Cardinality.Occurs(
                    int.Parse(minOccurs ?? "1"),
                    maxOccurs == null ? 1 :
                    maxOccurs.Equals("unbounded") ? null :
                    int.Parse(maxOccurs));
            }
        }

        internal static Regex ExtractPatternRegex(XElement patternElement)
        {
            var regexPattern = patternElement.Attribute(Legend.Enumerations.PatternElement_Regex).Value;
            var options = ExtractPatternOptions(patternElement);

            return new Regex(regexPattern, options);
        }

        internal static bool ExtractCaseSensitivity(XElement element)
        {
            return element.TryAttribute(Legend.Enumerations.LiteralElement_CaseSensitive, out var att)
                && bool.Parse(att.Value?.ToLower());
        }

        internal static int? ExtractThreshold(XElement nonTerminal)
        {
            return nonTerminal.TryAttribute(Legend.Enumerations.NonTerminalElement_Threshold, out var attribute)
                ? int.Parse(attribute.Value)
                : null;
        }

        internal static RegexOptions ExtractPatternOptions(XElement patternElement)
        {
            var options = RegexOptions.Compiled;

            var attribute = Legend.Enumerations.PatternElement_CaseSensitive;
            options |= patternElement.TryAttribute(attribute, out var att) && !bool.Parse(att.Value)
                ? RegexOptions.IgnoreCase
                : RegexOptions.None;

            attribute = Legend.Enumerations.PatternElement_MultiLine;
            options |= patternElement.TryAttribute(attribute, out att) && bool.Parse(att.Value)
                ? RegexOptions.Multiline
                : RegexOptions.None;

            attribute = Legend.Enumerations.PatternElement_SingleLine;
            options |= patternElement.TryAttribute(attribute, out att) && bool.Parse(att.Value)
                ? RegexOptions.Singleline
                : RegexOptions.None;

            attribute = Legend.Enumerations.PatternElement_ExplicitCapture;
            options |= patternElement.TryAttribute(attribute, out att) && bool.Parse(att.Value)
                ? RegexOptions.ExplicitCapture
                : RegexOptions.None;

            attribute = Legend.Enumerations.PatternElement_IgnoreWhitespace;
            options |= patternElement.TryAttribute(attribute, out att) && bool.Parse(att.Value)
                ? RegexOptions.IgnorePatternWhitespace
                : RegexOptions.None;

            return options;
        }


        private static Stream GetXsdResourceStream()
        {
            return Assembly
                .GetExecutingAssembly()
                .GetManifestResourceStream($"{typeof(XmlBuilder).Namespace}.RuleDefinition.xsd");
        }
    }
}
