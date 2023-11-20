using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar;

namespace Axis.Pulsar.Core;

/// <summary>
/// 
/// </summary>
public interface IRecognitionError
{
    Segment TokenSegment { get; }

    string Symbol { get; }
}

/// <summary>
/// 
/// </summary>
public class FailedRecognitionError :
    Exception,
    IRecognitionError
{
    public Segment TokenSegment { get; }

    public string Symbol { get; }

    public FailedRecognitionError(
        string symbol,
        int position)
    {
        TokenSegment = Segment.Of(position);

        Symbol = symbol.ThrowIfNot(
            IProduction.SymbolPattern.IsMatch,
            new ArgumentException($"Invalid {nameof(symbol)}: symbol pattern mis-match"));
    }

    public static FailedRecognitionError Of(
        string symbol,
        int position)
        => new(symbol, position);
}

/// <summary>
/// 
/// </summary>
public class PartialRecognitionError :
    Exception,
    IRecognitionError
{
    public Segment TokenSegment { get; }

    public string Symbol { get; }

    public PartialRecognitionError(
        string symbol,
        int position,
        int length)
    {
        TokenSegment = Segment.Of(position, length);

        Symbol = symbol.ThrowIfNot(
            IProduction.SymbolPattern.IsMatch,
            new ArgumentException($"Invalid {nameof(symbol)}: symbol pattern mis-match"));
    }

    public static PartialRecognitionError Of(
        string symbol,
        int position,
        int length)
        => new(symbol, position, length);

    public static PartialRecognitionError Of(
        string symbol,
        Segment segment)
        => new(symbol, segment.Offset, segment.Length);
}
