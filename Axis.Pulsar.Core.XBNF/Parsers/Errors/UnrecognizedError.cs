using Axis.Luna.Extensions;

namespace Axis.Pulsar.Core.XBNF;

//public class UnmatchedError: Exception
//{
//    public string ExpectedSymbol { get; }
//    public int Position { get; }

//    public UnmatchedError(string expectedSymbol, int position)
//    {
//        ExpectedSymbol = expectedSymbol.ThrowIf(
//            string.IsNullOrWhiteSpace,
//            new ArgumentException("Invalid exectedSymbol"));

//        Position = position.ThrowIf(
//            p => p < 0,
//            new ArgumentException($"Invalid position: {position}"));
//    }
//}
