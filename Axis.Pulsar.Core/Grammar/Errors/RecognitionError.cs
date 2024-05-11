using Axis.Luna.Common.Segments;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Composite.Group;

namespace Axis.Pulsar.Core.Grammar.Errors
{

    /// <summary>
    /// 
    /// </summary>
    public interface INodeRecognitionError
    {
        public Segment TokenSegment { get; }

        public SymbolPath Symbol { get; }
    }

    /// <summary>
    /// Represents a failure to recognize input tokens based on a given rule
    /// </summary>
    public readonly struct FailedRecognitionError : INodeRecognitionError
    {
        public Segment TokenSegment { get; }

        public SymbolPath Symbol { get; }

        public FailedRecognitionError(
            SymbolPath symbol,
            int position)
        {
            TokenSegment = Segment.Of(position);
            Symbol = symbol;
        }

        public static FailedRecognitionError Of(
            SymbolPath symbol,
            int position)
            => new(symbol, position);
    }

    /// <summary>
    /// Represents a failure to recognize input tokens based on a given rule, AFTER a given number
    /// of VITAL tokens/symbols have been recognized. This signals a fatal error and all recognition
    /// is halted.
    /// </summary>
    public readonly struct PartialRecognitionError : INodeRecognitionError
    {
        public Segment TokenSegment { get; }

        public SymbolPath Symbol { get; }

        public PartialRecognitionError(
            SymbolPath symbol,
            int position,
            int length)
        {
            TokenSegment = Segment.Of(position, length);
            Symbol = symbol;
        }

        public static PartialRecognitionError Of(
            SymbolPath symbol,
            int position,
            int length)
            => new(symbol, position, length);

        public static PartialRecognitionError Of(
            SymbolPath symbol,
            Segment segment)
            => new(symbol, segment.Offset, segment.Count);
    }

    /// <summary>
    /// Represents failure to aggregate recognized symbols. Raised by instances of <see cref="Grammar.Composite.Group.IAggregationRule"/>.
    /// </summary>
    public readonly struct SymbolAggregationError
    {
        public INodeRecognitionError Cause { get; }

        public int ElementCount { get; }

        public SymbolAggregationError(
            INodeRecognitionError cause,
            ISymbolNodeAggregation nodeAggregation)
        {
            ArgumentNullException.ThrowIfNull(nodeAggregation);

            (Cause, ElementCount) = cause switch
            {
                FailedRecognitionError => (cause, nodeAggregation.RequiredNodeCount()),
                PartialRecognitionError => (cause, nodeAggregation.NodeCount()),
                _ => throw new InvalidOperationException(
                    $"Invalid cause: {cause?.GetType()}")
            };
        }

        public SymbolAggregationError(
            INodeRecognitionError cause,
            int elementCount)
        {
            ElementCount = elementCount.ThrowIf(
                i => i < 0,
                _ => new ArgumentOutOfRangeException(nameof(elementCount)));

            Cause = cause switch
            {
                FailedRecognitionError
                or PartialRecognitionError => cause,
                _ => throw new InvalidOperationException(
                    $"Invalid cause: {cause?.GetType()}")
            };
        }

        public static SymbolAggregationError Of(
            INodeRecognitionError cause,
            ISymbolNodeAggregation aggregation)
            => new(cause, aggregation);

        public static SymbolAggregationError Of(
            INodeRecognitionError cause,
            int elementCount)
            => new(cause, elementCount);
    }
}