using Axis.Luna.Common.Segments;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using System.Collections.Immutable;

namespace Axis.Pulsar.Core.Grammar.Errors
{
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
    /// Represents failure to aggregate recognized symbols. Raised by instances of <see cref="Rules.Aggregate.IAggregation"/>.
    /// </summary>
    public readonly struct AggregateRecognitionError
    {
        public INodeRecognitionError Cause { get; }

        public ImmutableArray<ISymbolNode> RecognizedNodes { get; }

        public int RequiredNodeCount => !RecognizedNodes.IsDefault
            ? RecognizedNodes.Sum(node => node.RequiredNodeCount())
            : 0;

        public AggregateRecognitionError(
            INodeRecognitionError cause,
            params ISymbolNode[] nodes)
            : this(cause, nodes.AsEnumerable())
        {
        }

        public AggregateRecognitionError(
            INodeRecognitionError cause,
            IEnumerable<ISymbolNode> nodes)
        {
            Cause = cause.ThrowIfNull(() => new ArgumentNullException(nameof(cause)));
            RecognizedNodes = nodes
                .ThrowIfNull(
                    () => new ArgumentNullException(nameof(nodes)))
                .ThrowIfAny(
                    node => node is null,
                    _ => new InvalidOperationException($"Invalid node: null"))
                .ToImmutableArray();
        }

        public static AggregateRecognitionError Of(
            INodeRecognitionError cause,
            params ISymbolNode[] nodes)
            => new(cause, nodes);

        public static AggregateRecognitionError Of(
            INodeRecognitionError cause,
            IEnumerable<ISymbolNode> nodes)
            => new(cause, nodes);
    }
}