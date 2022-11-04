using Axis.Pulsar.Parser.CST;
using Axis.Pulsar.Parser.Input;
using Moq;
using System.Collections.Generic;

namespace Axis.Pulsar.Parser.Tests
{
    internal static class Extensions
    {

        public static string Concat(this IEnumerable<string> strings) => string.Join("", strings);

        internal static Parser.Recognizers.IRecognizer CreateRecognizer(this Parser.Recognizers.IResult expectedResult)
        {
            var mock = new Mock<Parser.Recognizers.IRecognizer>();
            _ = mock.Setup(r => r.Recognize(It.IsAny<BufferedTokenReader>()))
                .Returns(expectedResult);

            return mock.Object;
        }

        internal static Parser.Recognizers.IRecognizer CreatePassingRecognizer(this (string symbol, string token)  tuple)
            => CreateRecognizer(new Parser.Recognizers.IResult.Success(ICSTNode.Of(tuple.symbol, tuple.token)));
    }
}
