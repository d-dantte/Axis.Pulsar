using Axis.Luna.Extensions;
using Axis.Pulsar.Grammar.IO;
using Axis.Pulsar.Grammar.Language;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;


namespace Axis.Pulsar.Languages.Xml
{
    public class Exporter : IExporter
    {
        /// <inheritdoc/>
        public void ExportGrammar(Grammar.Language.Grammar grammar, Stream outputStream)
        {
            if (grammar == null)
                throw new ArgumentNullException(nameof(grammar));

            var writer = XmlWriter.Create(outputStream);
            this.ToGrammarElement(grammar)
                .WriteTo(writer);

            writer.Flush();
        }

        /// <inheritdoc/>
        public async Task ExportGrammarAsync(
            Grammar.Language.Grammar grammar,
            Stream outputStream,
            CancellationToken? token)
        {
            if (grammar == null)
                throw new ArgumentNullException(nameof(grammar));

            var writer = XmlWriter.Create(outputStream);
            await this.ToGrammarElement(grammar)
                .WriteToAsync(writer, token ?? CancellationToken.None);

            writer.Flush();
        }

        
        internal XElement ToGrammarElement(Grammar.Language.Grammar grammar)
        {
            return new XElement(
                Legend.LanguageElement,
                grammar.Productions
                    .Select(ToProductionElement)
                    .ToArray());
        }

        internal XElement ToProductionElement(Production production)
        {
            return production.Rule.Rule switch
            {
                Grammar.Language.Rules.Literal l => new XElement(
                    Legend.LiteralElement,
                    new XAttribute(Legend.LiteralElement_Name, production.Symbol),
                    new XAttribute(Legend.LiteralElement_Value, l.Value),
                    new XAttribute(Legend.LiteralElement_CaseSensitive, l.IsCaseSensitive)),

                Grammar.Language.Rules.Pattern p => new XElement(
                    ToPatternElementName(p),
                    Extensions
                        .Enumerate(
                            new XAttribute(Legend.PatternElement_Name, production.Symbol),
                            new XAttribute(Legend.PatternElement_Regex, p.Regex.ToString()))
                        .Concat(ToPatternOptionAttributes(p))
                        .Concat(ToPatternMatchTypeAttributes(p))),

                Grammar.Language.Rules.CustomTerminals.ICustomTerminal ct => new XElement(
                    Legend.CustomTerminalElement,
                    new XAttribute(Legend.CustomTerminallement_Symbol, ct.SymbolName)),

                _ => ToNonTerminalElement(production.Rule)
            };
        }

        internal XElement ToNonTerminalElement(
            Grammar.Language.Rules.ProductionRule rule)
        {
            return new XElement(
                Legend.NonTerminalElement,
                new XAttribute(Legend.NonTerminalElement_Name, rule.SymbolName),
                rule.RecognitionThreshold != null
                    ? new XAttribute(Legend.NonTerminalElement_Threshold, rule.RecognitionThreshold.Value)
                    : null,
                ToRuleElement(rule.Rule));
        }

        internal string ToPatternElementName(
            Grammar.Language.Rules.Pattern p,
            bool isInline = false)
        {
            return p.MatchType switch
            {
                Grammar.Language.MatchType.Closed => isInline
                    ? Legend.InlineClosedPatternElement
                    : Legend.ClosedPatternElement,

                Grammar.Language.MatchType.Open => isInline
                    ? Legend.InlineOpenPatternElement
                    : Legend.OpenPatternElement,

                _ => throw new ArgumentException($"Invalid match type: {p.MatchType?.GetType()}")
            };
        }

        internal IEnumerable<XAttribute> ToPatternOptionAttributes(
            Grammar.Language.Rules.Pattern p)
        {
            return p.Regex.Options
                .GetFlags()
                .Select(flag => flag switch
                {
                    RegexOptions.IgnoreCase => new XAttribute(
                        Legend.PatternElement_CaseSensitive,
                        false),

                    RegexOptions.IgnorePatternWhitespace => new XAttribute(
                        Legend.PatternElement_IgnoreWhitespace,
                        true),

                    RegexOptions.Multiline => new XAttribute(
                        Legend.PatternElement_MultiLine,
                        true),

                    RegexOptions.Singleline => new XAttribute(
                        Legend.PatternElement_SingleLine,
                        true),

                    RegexOptions.ExplicitCapture => new XAttribute(
                        Legend.PatternElement_ExplicitCapture,
                        true),

                    _ => null
                })
                .Where(xatt => xatt != null);
        }

        internal XElement ToRuleElement(IRule rule)
        {
            return rule switch
            {
                Grammar.Language.Rules.Literal l => new XElement(
                    Legend.InlineLiteralElement,
                    new XAttribute(Legend.LiteralElement_Value, l.Value),
                    new XAttribute(Legend.LiteralElement_CaseSensitive, l.IsCaseSensitive)),

                Grammar.Language.Rules.Pattern p => new XElement(
                    ToPatternElementName(p, true),
                    Extensions
                        .Enumerate(
                            new XAttribute(Legend.PatternElement_Regex, p.Regex.ToString()))
                        .Concat(ToPatternOptionAttributes(p))
                        .Concat(ToPatternMatchTypeAttributes(p))),

                Grammar.Language.Rules.CustomTerminals.ICustomTerminal ct => new XElement(
                    Legend.CustomTerminalElement,
                    new XAttribute(Legend.CustomTerminallement_Symbol, ct.SymbolName)),

                Grammar.Language.Rules.ProductionRef @ref => new XElement(
                    Legend.SymbolElement,
                    ToCardinalityAttributes(@ref)
                        .Concat(new XAttribute(Legend.SymbolElement_Name, @ref.ProductionSymbol))),

                Grammar.Language.Rules.EOF => new XElement(Legend.EOFlement),

                Grammar.Language.Rules.Choice choice => new XElement(
                    Legend.ChoiceElement,
                    choice.Rules
                        .Select(ToRuleElement)
                        .HardCast<XElement, object>()
                        .Concat(ToCardinalityAttributes(choice))
                        .ToArray()),

                Grammar.Language.Rules.Sequence sequence => new XElement(
                    Legend.SequenceElement,
                    sequence.Rules
                        .Select(ToRuleElement)
                        .HardCast<XElement, object>()
                        .Concat(ToCardinalityAttributes(sequence))
                        .ToArray()),

                Grammar.Language.Rules.Set set => new XElement(
                    Legend.SetElement,
                    set.Rules
                        .Select(ToRuleElement)
                        .HardCast<XElement, object>()
                        .Concat(ToCardinalityAttributes(set))
                        .Concat(new XAttribute(
                            Legend.SetElement_MinRecognitionCount,
                            set.MinRecognitionCount))
                        .ToArray()),

                _ => throw new ArgumentException($"Invalid rule type: {rule?.GetType()}")
            };
        }

        internal IEnumerable<XAttribute> ToCardinalityAttributes(IRepeatable repeatable)
        {
            var atts = new List<XAttribute>();

            if (repeatable.Cardinality.MaxOccurence != 1)
            {
                atts.Add(
                    new XAttribute(
                        Legend.ProductionElement_MaxOccurs,
                        repeatable.Cardinality.MaxOccurence == null
                            ? "unbounded"
                            : repeatable.Cardinality.MaxOccurence.Value.ToString()));
            }

            if (repeatable.Cardinality.MinOccurence != 1)
            {
                atts.Add(
                    new XAttribute(
                        Legend.ProductionElement_MinOccurs,
                        repeatable.Cardinality.MinOccurence.ToString()));
            }

            return atts;
        }

        internal IEnumerable<XAttribute> ToPatternMatchTypeAttributes(
            Grammar.Language.Rules.Pattern pattern)
        {
            return pattern.MatchType switch
            {
                Grammar.Language.MatchType.Open open => Extensions.Enumerate(
                    new XAttribute(
                        Legend.PatternElement_MaxMismatch,
                        open.MaxMismatch),
                    new XAttribute(
                        Legend.PatternElement_AllowsEmpty,
                        open.AllowsEmptyTokens)),

                Grammar.Language.MatchType.Closed closed => Extensions.Enumerate(
                    new XAttribute(
                        Legend.PatternElement_MinMatch,
                        closed.MinMatch),
                    new XAttribute(
                        Legend.PatternElement_MaxMatch,
                        closed.MaxMatch)),

                _ => throw new ArgumentException($"Invalid match type: {pattern.MatchType?.GetType()}")
            };
        }
    }
}
