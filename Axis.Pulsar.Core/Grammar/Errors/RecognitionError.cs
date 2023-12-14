using Axis.Luna.Common.Segments;
using Axis.Luna.Extensions;

namespace Axis.Pulsar.Core;

/// <summary>
/// 
/// </summary>
public interface INodeRecognitionError
{
    public Segment TokenSegment { get; }

    public string Symbol { get; }
}

/// <summary>
/// 
/// </summary>
public readonly struct FailedRecognitionError : INodeRecognitionError
{
    public Segment TokenSegment { get; }

    public string Symbol { get; }

    public FailedRecognitionError(
        string symbol,
        int position)
    {
        TokenSegment = Segment.Of(position);
        Symbol = symbol;
    }

    public static FailedRecognitionError Of(
        string symbol,
        int position)
        => new(symbol, position);
}

/// <summary>
/// 
/// </summary>
public readonly struct PartialRecognitionError : INodeRecognitionError
{
    public Segment TokenSegment { get; }

    public string Symbol { get; }

    public PartialRecognitionError(
        string symbol,
        int position,
        int length)
    {
        TokenSegment = Segment.Of(position, length);
        Symbol = symbol;
    }

    public static PartialRecognitionError Of(
        string symbol,
        int position,
        int length)
        => new(symbol, position, length);

    public static PartialRecognitionError Of(
        string symbol,
        Segment segment)
        => new(symbol, segment.Offset, segment.Count);
}

/// <summary>
/// 
/// </summary>
public readonly struct GroupRecognitionError//: IRecognitionError
{
    public INodeRecognitionError Cause { get; }

    public int ElementCount { get; }

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

    //public static GroupRecognitionError Of<TError>(
    //    TError cause,
    //    int elementCount)
    //    where TError : INodeRecognitionError
    //    => new(cause, elementCount);

    //public static GroupRecognitionError Of<TError>(
    //    TError cause)
    //    where TError : INodeRecognitionError
    //    => new(cause, 0);
}
