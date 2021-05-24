using System;
using System.Linq;

namespace Axis.Pulsar.Parser.Builder
{
    public static class ProductionParserBuilder
    {
        public static ProductionParser BuildParser(Language.Production production)
        {
            return production.Mode switch
            {
                Language.GroupingMode.Choice => new ChoiceParser(
                    production.Cardinality,
                    production.Members.Select(BuildParser).ToArray()),

                Language.GroupingMode.Sequence => new SequenceParser(
                    production.Cardinality,
                    production.Members.Select(BuildParser).ToArray()),

                Language.GroupingMode.Set => new SetParser(
                    production.Cardinality,
                    production.Members.Select(BuildParser).ToArray()),

                Language.GroupingMode.Single => new SingleSymbolParser(
                    production.Cardinality,
                    RuleParserBuilder.BuildParser(production.Rule)),

                _ => throw new ArgumentException($"Invalid production grouping mode: {production.Mode}"),
            };
        }
    }
}
