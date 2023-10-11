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

        public bool TryRecognize(
            TokenReader reader,
            ProductionPath parentPath,
            out IResult<NodeSequence> result)
        {
            ArgumentNullException.ThrowIfNull(reader);
            ArgumentNullException.ThrowIfNull(parentPath);

            var production = Grammar.GetProduction(Symbol);
            var position = reader.Position;
            if (!production.TryRecognize(reader, parentPath, out var refResult))
            {
                var error = refResult.AsError().ActualCause();
                result = error switch
                {
                    Errors.IRecognitionError ire => GroupError
                        .Of(ire, NodeSequence.Empty)
                        .ApplyTo(Result.Of<NodeSequence>),

                    _ => Errors.RuntimeError
                        .Of(parentPath, error)
                        .ApplyTo(ire => (ire, NodeSequence.Empty))
                        .ApplyTo(GroupError.Of)
                        .ApplyTo(Result.Of<NodeSequence>)
                };

                reader.Reset(position);
                return false;
            }

            result = refResult.Map(node => NodeSequence.Of(node));
            return true;
        }
    }
}
