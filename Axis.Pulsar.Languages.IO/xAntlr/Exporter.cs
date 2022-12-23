using Axis.Luna.Extensions;
using Axis.Pulsar.Grammar.IO;
using Axis.Pulsar.Grammar.Language;
using Axis.Pulsar.Grammar.Language.Rules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Axis.Pulsar.Languages.xAntlr
{
    public class Exporter : IExporter
    {
        private Dictionary<string, GroupFilter> _filters = new Dictionary<string, GroupFilter>();

        #region Language Keywords
        public const string PRODUCTION_OPTION_THRESHOLD = "threshold";
        #endregion

        public Exporter(params GroupFilter[] filters)
        {
            foreach (var filter in filters)
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

            var text = ToGrammarText(grammar);
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

            var text = ToGrammarText(grammar);
            var writer = new StreamWriter(outputStream);
            await writer.WriteAsync(text);
        }

        internal string ToGrammarText(Grammar.Language.Grammar grammar)
        {
            return grammar.Productions
                .GroupBy(GroupProduction)
                .Select(ToProductionBlockString)
                .Aggregate(new StringBuilder(), (sb, next) => sb.AppendLine(next))
                .ToString();
        }

        private GroupFilter GroupProduction(Production production)
            => _filters.Values.FirstOrDefault(f => f.Filter.Invoke(production));

        private string ToProductionBlockString(IGrouping<GroupFilter, Production> productionGroup)
        {
            var sb = productionGroup.Key.GroupComment?
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => $"# {line}")
                .JoinUsing(Environment.NewLine)
                .ApplyTo(lines => new StringBuilder(lines))
                ?? new StringBuilder();

            return productionGroup
                .Select(ToProductionLine)
                .Aggregate(sb, (_sb, next) => _sb.AppendLine(next))
                .ToString();
        }

        internal string ToProductionLine(Production production)
        {
            var sbuilder = new StringBuilder().AppendLine(production.Symbol);

            this.ToProductionOptions(production)
                .Select(option => $" -{option.Key} {option.Value}")
                .Aggregate((string)null, (line, option) => (line ?? "\t>") + option)
                .Consume(optionline => sbuilder.AppendLine(optionline));

            sbuilder
                .Append(":")
                .AppendLine(production.Rule.Rule switch
                {
                    Choice choice => ToAlternatives(choice),

                    Set set => throw new InvalidOperationException($"{typeof(Set)} rules aren't supported"),

                    _ => ToRuleListLine(production.Rule.Rule)
                })
                .AppendLine(";")
                .AppendLine();
        }

        internal string ToAlternatives(Choice choice)
        {

        }

        internal KeyValuePair<string, string>[] ToProductionOptions(Production production)
        {
            var options = new List<KeyValuePair<string, string>>();

            // threshold
            if (production.Rule.RecognitionThreshold != null)
                options.Add(new KeyValuePair<string, string>(
                    PRODUCTION_OPTION_THRESHOLD,
                    production.Rule.RecognitionThreshold.ToString()));

            // other options can come here.

            return options.ToArray();
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
