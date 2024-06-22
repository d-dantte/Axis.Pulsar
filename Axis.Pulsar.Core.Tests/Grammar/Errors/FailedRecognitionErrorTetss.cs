using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Errors;

namespace Axis.Pulsar.Core.Tests.Grammar.Errors
{
    [TestClass]
    public class FailedRecognitionErrorTetss
    {
        [TestMethod]
        public void Construction_Tests()
        {
            var error = FailedRecognitionError.Of("symbol", 0);
            Assert.AreEqual<SymbolPath>("symbol", error.Symbol);
            Assert.AreEqual(0, error.TokenSegment.Offset);
            Assert.AreEqual(1, error.TokenSegment.Count);
        }
    }
}
