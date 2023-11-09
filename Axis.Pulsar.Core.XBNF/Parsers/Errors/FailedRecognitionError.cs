using Axis.Luna.Extensions;

namespace Axis.Pulsar.Core.XBNF;

public class FaultyMatchError: Exception
{
    public string ExpectedSymbol { get; }

    public int Position { get; }

    public int Length { get; }

    public FaultyMatchError(
        string expectedSymbol,
        int position,
        int length)
    {
        ExpectedSymbol = expectedSymbol.ThrowIf(
            string.IsNullOrWhiteSpace,
            new ArgumentException("Invalid exectedSymbol"));

        Position = position.ThrowIf(
            p => p < 0,
            new ArgumentException($"Invalid position: {position}"));

        Length = length.ThrowIf(
            p => p < 0,
            new ArgumentException($"Invalid length: {length}"));
    }
}
