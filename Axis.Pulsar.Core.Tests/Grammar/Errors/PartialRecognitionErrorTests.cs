using Axis.Luna.Common.Segments;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Errors;

namespace Axis.Pulsar.Core.Tests.Grammar.Errors
{
    [TestClass]
    public class PartialRecognitionErrorTests
    {
        [TestMethod]
        public void Construction_Tests()
        {
            var error = PartialRecognitionError.Of("symbol", 0, 5);
            Assert.AreEqual<SymbolPath>("symbol", error.Symbol);
            Assert.AreEqual(Segment.Of(0, 5), error.TokenSegment);

            error = PartialRecognitionError.Of("x", Segment.Of(5, 3));
            Assert.AreEqual(Segment.Of(5, 3), error.TokenSegment);

        }
    }
}
