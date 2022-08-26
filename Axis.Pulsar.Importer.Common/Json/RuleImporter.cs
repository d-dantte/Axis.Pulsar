using Axis.Pulsar.Importer.Common.Json.Models;
using Axis.Pulsar.Importer.Common.Json.Utils;
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

        public IGrammar ImportGrammar(Stream inputStream)
        {
            using var reader = new StreamReader(inputStream);
            var json = reader.ReadToEnd();
            var grammar = JsonConvert.DeserializeObject<Models.Grammar>(json, SerializerSettings);

            return ToGrammar(grammar);
        }

        public async Task<IGrammar> ImportGrammarAsync(Stream inputStream)
        {
            using var reader = new StreamReader(inputStream);
            var json = await reader.ReadToEndAsync();
            var grammar = JsonConvert.DeserializeObject<Models.Grammar>(json, SerializerSettings);

            return ToGrammar(grammar);
        }

        public static Parser.Grammar.IGrammar ToGrammar(Models.Grammar grammar)
        {
            // Assume that the root production will always appear first in the production collection
            return grammar.Productions
                .Aggregate(
                    GrammarBuilder.NewBuilder(),
                    (builder, production) => builder.WithProduction(production.Name, ToRule(production.Rule)))
                .WithRoot(grammar.Productions[0].Name)
                .Build();
        }

        public static Parser.Grammar.IRule ToRule(Models.IRule rule)
        {
            return rule switch
            {
                Literal l => new LiteralRule(l.Value, l.IsCaseSensitive),

                Pattern p => new PatternRule(
                    new Regex(p.Regex, p.IsCaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase),
                    Cardinality.Occurs(p.MinMatch, p.MaxMatch)),

                Expression e => new SymbolExpressionRule(ToExpression(e.Grouping), e.RecognitionThreshold),

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
    }
}
