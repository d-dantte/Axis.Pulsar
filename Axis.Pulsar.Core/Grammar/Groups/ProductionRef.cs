using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Exceptions;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Grammar.Groups
{
    public class ProductionRef : IGroupElement
    {
        /// <summary>
        /// The grammar to which the referred production belongs
        /// </summary>
        public IGrammar Grammar { get; }

        public Cardinality Cardinality { get; }

        /// <summary>
        /// The production symbol
        /// </summary>
        public string Symbol { get; }

        public ProductionRef(string productionSymbol, Cardinality cardinality, IGrammar grammar)
        {
            Grammar = grammar.ThrowIfNull(new ArgumentNullException(nameof(grammar)));
            Cardinality = cardinality.ThrowIfDefault(new ArgumentException($"Invalid {nameof(cardinality)}: default"));
            Symbol = productionSymbol
                .ThrowIfNot(
                    Production.SymbolPattern.IsMatch,
                    new ArgumentException($"Invalid {nameof(productionSymbol)}: '{productionSymbol}'"))
                .ThrowIfNot(
                    grammar.ContainsProduction,
                    new ArgumentException($"Invalid {nameof(productionSymbol)}: production not found"));
        }

        public static ProductionRef Of(
            string productionSymbol,
            Cardinality cardinality,
            IGrammar grammar)
            => new(productionSymbol, cardinality, grammar);

        public bool TryRecognize(
            TokenReader reader,
            ProductionPath parentPath,
            out IResult<NodeSequence> result)
        {
            ArgumentNullException.ThrowIfNull(reader);
            ArgumentNullException.ThrowIfNull(parentPath);

            var production = Grammar.GetProduction(Symbol);
            var position = reader.Position;
            if (!production.TryProcessRule(reader, parentPath, out var refResult))
            {
                reader.Reset(position);

                result = refResult.AsError().MapGroupError(
                    ute => GroupError.Of(ute, NodeSequence.Empty),
                    pte => GroupError.Of(pte, NodeSequence.Empty));

                return false;
            }

            result = refResult.Map(node => NodeSequence.Of(node));
            return true;
        }
    }
}
