using Axis.Pulsar.Grammar.Builders;
using Axis.Pulsar.Grammar.IO;
using Axis.Pulsar.Grammar.Language;
using Axis.Pulsar.Grammar.Language.Rules;
using System;
using System.Collections.Concurrent;
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
using MatchType = Axis.Pulsar.Grammar.Language.MatchType;

namespace Axis.Pulsar.Languages.Xml
{
    public class Importer : IImporter, IValidatorRegistry, ICustomTerminalRegistry
    {
        private ConcurrentDictionary<string, IProductionValidator> _validatorMap = new();

        /// <inheritdoc/>
        public Grammar.Language.Grammar ImportGrammar(Stream inputStream)
        {
            var xml = XDocument.Load(inputStream);

            return ToGrammar(xml);
        }

        /// <inheritdoc/>
        public async Task<Grammar.Language.Grammar> ImportGrammarAsync(Stream inputStream)
        {
            var xml = await XDocument.LoadAsync(
                inputStream,
                LoadOptions.None,
                CancellationToken.None);

            return ToGrammar(xml);
        }

        #region Validator API
        public IValidatorRegistry RegisterValidator(string symbolName, IProductionValidator validator)
        {
            if (validator is null)
                throw new ArgumentNullException(nameof(validator));

            if (symbolName is null)
                throw new ArgumentNullException(nameof(symbolName));

            if (!SymbolHelper.SymbolPattern.IsMatch(symbolName))
                throw new ArgumentException($"Invalid {nameof(symbolName)}: {symbolName}");

            _ = _validatorMap.AddOrUpdate(symbolName, validator, (_, _) => validator);

            return this;
        }

        public string[] RegisteredSymbols() => _validatorMap.Keys.ToArray();

        public IProductionValidator RegisteredValidator(string symbolName) 
            => _validatorMap.TryGetValue(symbolName, out var validator)
                ? validator
                : null;
        #endregion

        #region Custom Terminal API
        ICustomTerminalRegistry RegisterTerminal(ICustomTerminal validator);7

        bool TryRegister(ICustomTerminal terminal);

        string[] RegisteredSymbols();

        ICustomTerminal RegisteredTerminal(string symbolName);
        #endregion

        private Grammar.Language.Grammar ToGrammar(XDocument xml)
        {
            ValidateDocument(xml);
            return ToGrammar(xml.Root);
        }

        internal static void ValidateDocument(XDocument xml)
        {
            var schemas = new XmlSchemaSet();
            schemas.Add("", XmlReader.Create(GetXsdResourceStream()));

            var errors = new List<XmlValidationException.ErrorInfo>();
            xml.Validate(
                schemas,
                (source, args) => errors.Add(new(
                    args.Message,
                    args.Exception,
                    args.Severity)));

            if (errors.Count > 0)
                throw new XmlValidationException(errors.ToArray());
        }

        private static Stream GetXsdResourceStream()
        {
            return Assembly
                .GetExecutingAssembly()
                .GetManifestResourceStream($"{typeof(Importer).Namespace}.RuleDefinition.xsd");
        }

        internal Grammar.Language.Grammar ToGrammar(XElement rootElement)
        {
            return rootElement
                .Elements()
                .Aggregate(
                    GrammarBuilder.NewBuilder(),
                    (builder, element) => builder.HavingProduction(
                        _builder => ConfigureProduction(_builder, element)))
                .WithRoot(rootElement.Attribute(Legend.LanguageElement_Root).Value)
                .Build();
        }

        internal void ConfigureProduction(
            ProductionBuilder productionBuilder,
            XElement element)
        {
            productionBuilder
                .WithSymbol(element.Attribute("name").Value)
                .WithRecognitionThreshold(ExtractThreshold(element))
                .WithValidator(RegisteredValidator(element.Attribute("name").Value))
                .WithRule(_builder => ConfigureRule(
                    _builder,
                    element.Name.LocalName switch
                    {
                        Legend.NonTerminalElement => element.FirstChild(),
                        _ => element
                    }));
        }

        internal void ConfigureRule(RuleBuilder builder, XElement element)
        {
            _ = element.Name.LocalName switch
            {
                Legend.EOFlement => builder.WithEOF(),

                Legend.OpenPatternElement => builder.WithPattern( 
                    ExtractPatternRegex(element),
                    ExtractOpenMatchType(element)),

                Legend.ClosedPatternElement => builder.WithPattern(
                    ExtractPatternRegex(element),
                    ExtractClosedMatchType(element)),

                Legend.LiteralElement => builder.WithLiteral(
                    element.Attribute(Legend.LiteralElement_Value).Value.ApplyEscape(),
                    ExtractCaseSensitivity(element)),

                Legend.SymbolElement => builder.WithRef(
                    element.Attribute(Legend.SymbolElement_Name).Value,
                    ExtractCardinality(element)),

                Legend.SequenceElement => builder.WithSequence(
                    cardinality: ExtractCardinality(element),
                    ruleListBuilderAction: _builder => BuildRuleList(_builder, element.Elements())),

                Legend.ChoiceElement => builder.WithChoice(
                    cardinality: ExtractCardinality(element),
                    ruleListBuilderAction: _builder => BuildRuleList(_builder, element.Elements())),

                Legend.SetElement => builder.WithSet(
                    cardinality: ExtractCardinality(element),
                    minRecognitionCount: ExtractMinRecognitionCount(element),
                    ruleListBuilderAction: _builder => BuildRuleList(_builder, element.Elements())),

                _ => throw new ArgumentException($"Invalid element: {element.Name}")
            };
        }

        internal void BuildRuleList(RuleListBuilder builder, IEnumerable<XElement> elements)
        {
            foreach (var element in elements)
            {
                _ = element.Name.LocalName switch
                {
                    Legend.EOFlement => builder.HavingEOF(),

                    Legend.OpenPatternElement => builder.HavingPattern(
                        ExtractPatternRegex(element),
                        ExtractOpenMatchType(element)),

                    Legend.ClosedPatternElement => builder.HavingPattern(
                        ExtractPatternRegex(element),
                        ExtractClosedMatchType(element)),

                    Legend.LiteralElement => builder.HavingLiteral(
                        element.Attribute(Legend.LiteralElement_Value).Value.ApplyEscape(),
                        ExtractCaseSensitivity(element)),

                    Legend.SymbolElement => builder.HavingRef(
                        element.Attribute(Legend.SymbolElement_Name).Value,
                        ExtractCardinality(element)),

                    Legend.SequenceElement => builder.HavingSequence(
                        cardinality: ExtractCardinality(element),
                        ruleListBuilderAction: _builder => BuildRuleList(_builder, element.Elements())),

                    Legend.ChoiceElement => builder.HavingChoice(
                        cardinality: ExtractCardinality(element),
                        ruleListBuilderAction: _builder => BuildRuleList(_builder, element.Elements())),

                    Legend.SetElement => builder.HavingSet(
                        cardinality: ExtractCardinality(element),
                        minRecognitionCount: ExtractMinRecognitionCount(element),
                        ruleListBuilderAction: _builder => BuildRuleList(_builder, element.Elements())),

                    _ => throw new ArgumentException($"Invalid element: {element.Name}")
                };
            }
        }

        internal static Regex ExtractPatternRegex(XElement patternElement)
        {
            var regexPattern = patternElement.Attribute(Legend.PatternElement_Regex).Value;
            var options = ExtractPatternOptions(patternElement);

            return new Regex(regexPattern, options);
        }

        internal static MatchType.Closed ExtractClosedMatchType(XElement patternElement)
        {
            var minMatch = patternElement.Attribute(Legend.PatternElement_MinMatch)?.Value;
            var maxMatch = patternElement.Attribute(Legend.PatternElement_MaxMatch)?.Value;

            return new MatchType.Closed(
                int.Parse(minMatch ?? "1"),
                int.Parse(maxMatch ?? "1"));
        }

        internal static MatchType.Open ExtractOpenMatchType(XElement patternElement)
        {
            var allowsEmpty = patternElement.Attribute(Legend.PatternElement_AllowsEmpty)?.Value;
            var maxMismatch = patternElement.Attribute(Legend.PatternElement_MaxMismatch)?.Value;

            return new MatchType.Open(
                int.Parse(maxMismatch ?? "1"),
                bool.Parse(allowsEmpty ?? "false"));
        }

        internal static RegexOptions ExtractPatternOptions(XElement patternElement)
        {
            var options = RegexOptions.Compiled;

            var attribute = Legend.PatternElement_CaseSensitive;
            options |= patternElement.TryAttribute(attribute, out var att) && !bool.Parse(att.Value)
                ? RegexOptions.IgnoreCase
                : RegexOptions.None;

            attribute = Legend.PatternElement_MultiLine;
            options |= patternElement.TryAttribute(attribute, out att) && bool.Parse(att.Value)
                ? RegexOptions.Multiline
                : RegexOptions.None;

            attribute = Legend.PatternElement_SingleLine;
            options |= patternElement.TryAttribute(attribute, out att) && bool.Parse(att.Value)
                ? RegexOptions.Singleline
                : RegexOptions.None;

            attribute = Legend.PatternElement_ExplicitCapture;
            options |= patternElement.TryAttribute(attribute, out att) && bool.Parse(att.Value)
                ? RegexOptions.ExplicitCapture
                : RegexOptions.None;

            attribute = Legend.PatternElement_IgnoreWhitespace;
            options |= patternElement.TryAttribute(attribute, out att) && bool.Parse(att.Value)
                ? RegexOptions.IgnorePatternWhitespace
                : RegexOptions.None;

            return options;
        }

        internal static bool ExtractCaseSensitivity(XElement element)
        {
            return element.TryAttribute(Legend.LiteralElement_CaseSensitive, out var att)
                && bool.Parse(att.Value?.ToLower());
        }

        internal static int? ExtractThreshold(XElement nonTerminal)
        {
            return nonTerminal.TryAttribute(Legend.NonTerminalElement_Threshold, out var attribute)
                ? int.Parse(attribute.Value)
                : null;
        }

        internal static int? ExtractMinRecognitionCount(XElement nonTerminal)
        {
            return nonTerminal.TryAttribute(Legend.SetElement_MinRecognitionCount, out var attribute)
                ? int.Parse(attribute.Value)
                : null;
        }

        internal static Cardinality ExtractCardinality(XElement element)
        {
            var minOccurs = element.Attribute(Legend.ProductionElement_MinOccurs)?.Value;
            var maxOccurs = element.Attribute(Legend.ProductionElement_MaxOccurs)?.Value;

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
    }
}
