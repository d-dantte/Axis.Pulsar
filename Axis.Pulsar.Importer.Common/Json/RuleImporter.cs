using Axis.Pulsar.Importer.Common.Json.Models;
using Axis.Pulsar.Importer.Common.Json.Utils;
using Axis.Pulsar.Parser.Builders;
using Axis.Pulsar.Parser.Grammar;
using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Axis.Pulsar.Importer.Common.Json
{
    public class GrammarImporter : IGrammarImporter
    {
        private static readonly JsonSerializerSettings SerializerSettings = new()
        {
            Converters = new List<JsonConverter>
            {
                new RuleJsonConverter()
            }
        };

        public IGrammar ImportGrammar(
            Stream inputStream,
            Dictionary<string, IRuleValidator<Parser.Grammar.IRule>> validators = null)
        {
            using var reader = new StreamReader(inputStream);
            var json = reader.ReadToEnd();
            var grammar = JsonConvert.DeserializeObject<Models.Grammar>(json, SerializerSettings);

            return ToGrammar(grammar, validators);
        }

        public async Task<IGrammar> ImportGrammarAsync(
            Stream inputStream,
            Dictionary<string, IRuleValidator<Parser.Grammar.IRule>> validators = null)
        {
            using var reader = new StreamReader(inputStream);
            var json = await reader.ReadToEndAsync();
            var grammar = JsonConvert.DeserializeObject<Models.Grammar>(json, SerializerSettings);

            return ToGrammar(grammar, validators);
        }

        public static IGrammar ToGrammar(
            Grammar grammar,
            Dictionary<string, IRuleValidator<Parser.Grammar.IRule>> validators = null)
        {
            // Assume that the root production will always appear first in the production collection
            return grammar.Productions
                .Aggregate(
                    GrammarBuilder.NewBuilder(),
                    (builder, production) => builder.WithProduction(
                        production.Name,
                        ToRule(
                            production.Rule,
                            validators?.TryGetValue(production.Name, out var validator) == true ? validator: null)))
                .WithRoot(grammar.Productions[0].Name)
                .Build();
        }

        public static Parser.Grammar.IRule ToRule(Models.IRule rule, IRuleValidator<Parser.Grammar.IRule> validator)
        {
            return rule switch
            {
                Literal l => new LiteralRule(
                    l.Value,
                    l.IsCaseSensitive,
                    validator),

                Pattern p => new PatternRule(
                    new Regex(p.Regex, p.IsCaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase),
                    ToMatchType(p.MatchType),
                    validator),

                Expression e => new SymbolExpressionRule(
                    ToExpression(e.Grouping),
                    e.RecognitionThreshold,
                    validator),

                _ => throw new Exception($"Invalid base rule: {rule.GetType()}")
            };
        }

        public static ISymbolExpression ToExpression(Models.IRule expression)
        {
            return expression switch
            {
                Ref r => new ProductionRef(
                    r.Symbol,
                    Cardinality.Occurs(r.MinOccurs, r.MaxOCcurs)),

                Grouping g when g.Mode == GroupMode.Sequence => SymbolGroup.Sequence(
                    Cardinality.Occurs(g.MinOccurs, g.MaxOccurs),
                    g.Rules.Select(ToExpression).ToArray()),

                Grouping g when g.Mode == GroupMode.Set => SymbolGroup.Set(
                    Cardinality.Occurs(g.MinOccurs, g.MaxOccurs),
                    g.Rules.Select(ToExpression).ToArray()),

                Grouping g when g.Mode == GroupMode.Choice => SymbolGroup.Choice(
                    Cardinality.Occurs(g.MinOccurs, g.MaxOccurs),
                    g.Rules.Select(ToExpression).ToArray()),

                _ => throw new Exception($"Invalid Expression: {expression}")
            };
        }

        public static IPatternMatchType ToMatchType(IMatchType matchType)
        {
            return matchType switch
            {
                IMatchType.OpenMatchType open => new IPatternMatchType.Open(
                    open.MaxMismatch,
                    open.AllowsEmpty),

                IMatchType.ClosedMatchType closed => new IPatternMatchType.Closed(
                    closed.MinMatch,
                    closed.MaxMatch),

                null => IPatternMatchType.Open.DefaultMatch,

                _ => throw new ArgumentException($"Invalid match type: {matchType}")
            };
        }
    }
}
