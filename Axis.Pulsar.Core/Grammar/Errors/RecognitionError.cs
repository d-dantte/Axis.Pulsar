using Axis.Luna.Common.Segments;
using Axis.Luna.Extensions;

namespace Axis.Pulsar.Core;

/// <summary>
/// 
/// </summary>
public interface IRecognitionError
{
}

/// <summary>
/// 
/// </summary>
public struct FailedRecognitionError : IRecognitionError
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
public struct PartialRecognitionError : IRecognitionError
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
public struct GroupRecognitionError: IRecognitionError
{
    public IRecognitionError Cause { get; }

    public int ElementCount { get; }

    public GroupRecognitionError(
        IRecognitionError cause,
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
        IRecognitionError cause,
        int elementCount)
        => new(cause, elementCount);

    public static GroupRecognitionError Of(
        IRecognitionError cause)
        => new(cause, 0);

    public static GroupRecognitionError Of<TError>(
        TError cause,
        int elementCount)
        where TError : IRecognitionError
        => new(cause, elementCount);

    public static GroupRecognitionError Of<TError>(
        TError cause)
        where TError : IRecognitionError
        => new(cause, 0);
}


#region Old/Obsolete exception-based error
/// <summary>
/// 
/// </summary>
public interface IRecognitionError__
{
    #region Members
    /// <summary>
    /// 
    /// </summary>
    Segment TokenSegment { get; }

    /// <summary>
    /// 
    /// </summary>
    string Symbol { get; }
    #endregion
}

/// <summary>
/// 
/// </summary>
public class FailedRecognitionError__ :
    Exception,
    IRecognitionError__
{
    public Segment TokenSegment { get; }

    public string Symbol { get; }

    public FailedRecognitionError__(
        string symbol,
        int position)
    {
        TokenSegment = Segment.Of(position);
        Symbol = symbol;
    }

    public static FailedRecognitionError__ Of(
        string symbol,
        int position)
        => new(symbol, position);
}

/// <summary>
/// 
/// </summary>
public class PartialRecognitionError__ :
    Exception,
    IRecognitionError__
{
    public Segment TokenSegment { get; }

    public string Symbol { get; }

    public PartialRecognitionError__(
        string symbol,
        int position,
        int length)
    {
        TokenSegment = Segment.Of(position, length);
        Symbol = symbol;
    }

    public static PartialRecognitionError__ Of(
        string symbol,
        int position,
        int length)
        => new(symbol, position, length);

    public static PartialRecognitionError__ Of(
        string symbol,
        Segment segment)
        => new(symbol, segment.Offset, segment.Count);
}

#endregion
