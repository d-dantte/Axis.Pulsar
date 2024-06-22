using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Tests.Grammar.Errors
{
    [TestClass]
    public class RecognitionFormatExceptionTests
    {
        [TestMethod]
        public void Construction_Tests()
        {
            var error = RecognitionFormatException.Of(8, 2, "tokens\ntokens\ntokens\ntokens");
            Assert.IsNotNull(error);
            Assert.AreEqual(2, error.Line);
            Assert.AreEqual(2, error.Column);
            Assert.AreEqual<Tokens>("ok", error.ErrorSegment);
            Assert.AreEqual(
                $"Recognition error at line: {error.Line}, column: {error.Column}, of the input tokens: '{error.ErrorSegment}'.",
                error.Message);

            Assert.ThrowsException<ArgumentOutOfRangeException>(
                () => RecognitionFormatException.Of(0, -1, "abcd"));

            Assert.ThrowsException<ArgumentOutOfRangeException>(
                () => RecognitionFormatException.Of(-1, 1, "abcd"));
        }
    }
}
