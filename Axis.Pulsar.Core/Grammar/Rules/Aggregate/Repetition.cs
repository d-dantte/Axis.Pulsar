using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Grammar.Rules.Aggregate
{
    public class Repetition : IAggregationElement
    {
        /// <summary>
        /// Defines the repetition rule
        /// </summary>
        public Cardinality Cardinality { get; }

        public AggregationType Type => AggregationType.Repetition;

        public IAggregationElement Element { get; }

        public Repetition(Cardinality cardinality, IAggregationElement element)
        {
            ArgumentNullException.ThrowIfNull(element);

            Element = element;
            Cardinality = cardinality;
        }

        public static Repetition Of(
            Cardinality cardinality,
            IAggregationElement element)
            => new(cardinality, element);

        /// <summary>
        /// Repeats the recognition process for the given element until it fails, or until the appropriate amount of
        /// repetitions is reached.
        /// </summary>
        /// <param name="reader">The token reader</param>
        /// <param name="symbolPath">The path of the production that owns the <paramref name="element"/></param>
        /// <param name="element">The element to apply cardinality recognition to</param>
        /// <param name="result">The result of applying cardinality recognition</param>
        /// <returns>True if a valid number of repetitions succeed, false otherwise.</returns>
        public bool TryRecognize(
            TokenReader reader,
            SymbolPath symbolPath,
            ILanguageContext context,
            out NodeAggregationResult result)
        {
            ArgumentNullException.ThrowIfNull(reader);

            var repetitions = 0;
            var position = reader.Position;
            var items = new List<ISymbolNode>();
            NodeAggregationResult elementResult = default;

            while (Cardinality.CanRepeat(repetitions))
            {
                var stepPosition = reader.Position;
                if (Element.TryRecognize(reader, symbolPath, context, out elementResult))
                {
                    items.Add(elementResult.Get<ISymbolNode>());
                    repetitions++;
                }
                else
                {
                    reader.Reset(stepPosition);
                    break;
                }
            }

            if (Cardinality.IsValidRepetition(repetitions))
                result = NodeAggregationResult.Of(
                    new ISymbolNode.Aggregate(
                        AggregationType.Repetition,
                        Cardinality.IsZeroMinOccurence,
                        items));

            else
            {
                reader.Reset(position);
                result = elementResult
                    .Get<AggregateRecognitionError>()
                    .ApplyTo(err =>
                        AggregateRecognitionError.Of(
                            err.Cause,
                            items.Concat(err.RecognizedNodes)))
                    .ApplyTo(NodeAggregationResult.Of);
            }

            return result.Is(out ISymbolNode _);
        }
    }
}
