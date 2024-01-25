using Axis.Luna.Common.Segments;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;

namespace Axis.Pulsar.Core.Grammar.Errors;

/// <summary>
/// 
/// </summary>
public interface INodeRecognitionError
{
    public Segment TokenSegment { get; }

    public SymbolPath Symbol { get; }
}

/// <summary>
/// 
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
/// 
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
/// 
/// </summary>
public readonly struct GroupRecognitionError
{
    public INodeRecognitionError Cause { get; }

    public int ElementCount { get; }


    public GroupRecognitionError(
        INodeRecognitionError cause,
        INodeSequence nodeSequence)
    {
        ArgumentNullException.ThrowIfNull(nodeSequence);

        (Cause, ElementCount) = cause switch
        {
            FailedRecognitionError => (cause, nodeSequence.RequiredNodeCount),
            PartialRecognitionError => (cause, nodeSequence.Count),
            _ => throw new InvalidOperationException(
                $"Invalid cause: {cause?.GetType()}")
        };
    }

    public GroupRecognitionError(
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

    public static GroupRecognitionError Of(
        INodeRecognitionError cause,
        int elementCount)
        => new(cause, elementCount);

    public static GroupRecognitionError Of(
        INodeRecognitionError cause)
        => new(cause, 0);

    public static GroupRecognitionError Of<TError>(
        TError cause,
        int elementCount)
        where TError : INodeRecognitionError
        => new(cause, elementCount);

    public static GroupRecognitionError Of<TError>(
        TError cause)
        where TError : INodeRecognitionError
        => new(cause, 0);

    public static GroupRecognitionError Of(
        INodeRecognitionError cause,
        INodeSequence sequence)
        => new(cause, sequence);

    public static GroupRecognitionError Of<TError>(
        TError cause,
        INodeSequence sequence)
        where TError : INodeRecognitionError
        => new(cause, sequence);
}
