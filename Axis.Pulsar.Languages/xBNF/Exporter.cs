using Axis.Luna.Extensions;
using Axis.Pulsar.Grammar.IO;
using Axis.Pulsar.Grammar.Language;
using Axis.Pulsar.Grammar.Language.Rules;
using Axis.Pulsar.Grammar.Language.Rules.CustomTerminals;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Axis.Pulsar.Languages.xBNF
{
    public class Exporter : IExporter
    {
        private Dictionary<string, GroupFilter> _filters = new Dictionary<string, GroupFilter>();


        public Exporter(params GroupFilter[] filters)
        {
            foreach(var filter in filters)
            {
                if (!_filters.TryAdd(filter.UniqueName, filter))
                    throw new ArgumentException($"Duplicate filter name found: {filter.UniqueName}");
            }
        }

        public void ExportGrammar(
            Grammar.Language.Grammar grammar,
            Stream outputStream)
        {
            if (grammar == null)
                throw new ArgumentNullException(nameof(grammar));

            var text = ToGrammarString(grammar);
            var writer = new StreamWriter(outputStream);
            writer.Write(text);
        }

        public async Task ExportGrammarAsync(
            Grammar.Language.Grammar grammar,
            Stream outputStream,
            CancellationToken? token = null)
        {
            if (grammar == null)
                throw new ArgumentNullException(nameof(grammar));

            var text = ToGrammarString(grammar);
            var writer = new StreamWriter(outputStream);
            await writer.WriteAsync(text);
            await writer.FlushAsync();
        }

        internal string ToGrammarString(Grammar.Language.Grammar grammar)
        {
            return grammar.Productions
                .GroupBy(GroupProduction)
                .Select(ToProductionBlockString)
                .Aggregate(new StringBuilder(), (sb, next) => sb.AppendLine(next))
                .ToString()
                .Trim();
        }

        internal string ToProductionString(Production production)
        {
            return new StringBuilder()
                .Append($"${production.Symbol}") //lhs
                .Append(" -> ")
                .Append(ToRuleString(production.Rule)) //rhs
                .ToString();
        }

        internal string ToRuleString(IRule rule)
        {
            return rule switch
            {
                EOF eof => ToEOFString(eof),
                Literal l => ToLiteralString(l),
                Pattern p => ToPatternString(p),
                ICustomTerminal ct => ToCustomTerminalString(ct),
                ProductionRef pr => ToProductionRefString(pr),
                Choice c => ToChoiceString(c),
                Sequence seq => ToSequenceString(seq),
                Set set => ToSetString(set),
                ProductionRule prule => ToProductionRuleString(prule),

                _ => throw new ArgumentException($"Invalid rule type: {rule?.GetType()}")
            };
        }

        internal string ToEOFString(EOF eof) => "EOF";

        internal string ToLiteralString(Literal literal) => literal.ToString();

        internal string ToPatternString(Pattern pattern) => pattern.ToString();

        internal string ToCustomTerminalString(ICustomTerminal customTerminal) => customTerminal.ToString();

        internal string ToProductionRefString(ProductionRef @ref)
            => $"${@ref.ProductionSymbol}{ToCardinalityString(@ref.Cardinality)}";

        internal string ToProductionRuleString(ProductionRule rule)
        {
            var threshold = rule.RecognitionThreshold > 0
                ? $">{rule.RecognitionThreshold}"
                : "";

            var innerRuleString = ToRuleString(rule.Rule);
            return $"{innerRuleString}{threshold}";
        }

        internal string ToChoiceString(Choice choice)
        {
            var ruleStrings = choice.Rules
                .Select(ToRuleString)
                .JoinUsing(" ");

            return $"?[{ruleStrings}]{ToCardinalityString(choice.Cardinality)}";
        }

        internal string ToSequenceString(Sequence sequence)
        {
            var ruleStrings = sequence.Rules
                .Select(ToRuleString)
                .JoinUsing(" ");

            return $"+[{ruleStrings}]{ToCardinalityString(sequence.Cardinality)}";
        }

        internal string ToSetString(Set set)
        {
            var ruleStrings = set.Rules
                .Select(ToRuleString)
                .JoinUsing(" ");

            return $"#{set.MinRecognitionCount}[{ruleStrings}]{ToCardinalityString(set.Cardinality)}";
        }


        internal string ToCardinalityString(Cardinality cardinality) => cardinality.ToString();

        private GroupFilter GroupProduction(Production production)
            => _filters.Values.FirstOrDefault(f => f.Filter.Invoke(production));

        private string ToProductionBlockString(IGrouping<GroupFilter, Production> productionGroup)
        {
            var sb = productionGroup.Key.GroupComment?
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => $"# {line}")
                .JoinUsing(Environment.NewLine)
                .ApplyTo(lines => new StringBuilder().AppendLine(lines))
                ?? new StringBuilder();

            return productionGroup
                .Select(ToProductionString)
                .Aggregate(sb, (_sb, next) => _sb.AppendLine(next))
                .ToString();
        }


        public readonly struct GroupFilter
        {
            /// <summary>
            /// A unique name for this group
            /// </summary>
            public string UniqueName { get; }


            /// <summary>
            /// The comment to be placed on top this group
            /// </summary>
            public string GroupComment { get; }

            /// <summary>
            /// The filter that selects productions that belong to this group
            /// </summary>
            public Func<Production, bool> Filter { get; }


            public GroupFilter(
                string uniqueName,
                string comment,
                Func<Production, bool> filter)
            {
                UniqueName = uniqueName.ThrowIf(
                    string.IsNullOrWhiteSpace,
                    new ArgumentException($"Invalid {nameof(uniqueName)}"));

                Filter = filter ?? throw new ArgumentNullException(nameof(filter));

                GroupComment = comment;
            }

            public GroupFilter(
                string uniqueName,
                Func<Production, bool> filter)
                : this(uniqueName, null, filter)
            { }


            public override bool Equals(object obj)
            {
                return obj is GroupFilter other
                    && EqualityComparer<string>.Default.Equals(other.UniqueName, UniqueName)
                    && EqualityComparer<string>.Default.Equals(other.GroupComment, GroupComment)
                    && EqualityComparer<Func<Production, bool>>.Default.Equals(other.Filter, Filter);
            }

            public override int GetHashCode() => HashCode.Combine(UniqueName, GroupComment, Filter);

            public static bool operator ==(GroupFilter first, GroupFilter second) => first.Equals(second);

            public static bool operator !=(GroupFilter first, GroupFilter second) => !(first == second);
        }
    }
}
