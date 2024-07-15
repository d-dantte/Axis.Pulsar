using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.XBNF.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Axis.Pulsar.Core.XBNF.Tests.Parsers
{
    [TestClass]
    public class XBNFResultTests
    {
        [TestMethod]
        public void Constructor_Tests()
        {
            var result = XBNFResult<bool>.Of(true);
            Assert.AreEqual(true, result.Value);

            result = XBNFResult<bool>.Of(FailedRecognitionError.Of("abc", 3));
            Assert.IsInstanceOfType<FailedRecognitionError>(result.Value);

            result = XBNFResult<bool>.Of(PartialRecognitionError.Of("abc", 3, 8));
            Assert.IsInstanceOfType<PartialRecognitionError>(result.Value);

            result = new XBNFResult<bool>(null!);
            Assert.IsNull(result.Value);

            Assert.ThrowsException<InvalidOperationException>(
                () => new XBNFResult<bool>("bleh"));
        }

        [TestMethod]
        public void Is_Tests()
        {
            var vresult = XBNFResult<bool>.Of(true);
            var fresult = XBNFResult<bool>.Of(FailedRecognitionError.Of("abc", 3));
            var presult = XBNFResult<bool>.Of(PartialRecognitionError.Of("abc", 3, 8));
            var nresult = new XBNFResult<bool>(null!);

            #region Is Value
            Assert.IsTrue(vresult.Is(out bool b));
            Assert.IsTrue(b);
            Assert.IsFalse(fresult.Is(out b));
            #endregion

            #region Is Failed
            Assert.IsTrue(fresult.Is(out FailedRecognitionError fre));
            Assert.IsFalse(presult.Is(out fre));
            #endregion

            #region Is Partial
            Assert.IsTrue(presult.Is(out PartialRecognitionError pre));
            Assert.IsFalse(vresult.Is(out pre));
            #endregion

            #region Is Null
            Assert.IsTrue(nresult.IsNull());
            #endregion
        }

        [TestMethod]
        public void MapMatch_Tests()
        {
            var vresult = XBNFResult<bool>.Of(true);
            var fresult = XBNFResult<bool>.Of(FailedRecognitionError.Of("abc", 3));
            var presult = XBNFResult<bool>.Of(PartialRecognitionError.Of("abc", 3, 8));
            var nresult = new XBNFResult<bool>(null!);

            #region Match value
            var @out = vresult.MapMatch(
                r => "result",
                f => "failed",
                p => "partial",
                () => "null");
            Assert.AreEqual("result", @out);
            #endregion

            #region Match Failed
            @out = fresult.MapMatch(
                r => "result",
                f => "failed",
                p => "partial",
                () => "null");
            Assert.AreEqual("failed", @out);
            #endregion

            #region Match Partial
            @out = presult.MapMatch(
                r => "result",
                f => "failed",
                p => "partial",
                () => "null");
            Assert.AreEqual("partial", @out);
            #endregion

            #region Match null
            @out = nresult.MapMatch(
                r => "result",
                f => "failed",
                p => "partial",
                () => "null");
            Assert.AreEqual("null", @out);

            @out = nresult.MapMatch(
                r => "result",
                f => "failed",
                p => "partial");
            Assert.IsNull(@out);
            #endregion
        }

        [TestMethod]
        public void ConsumeMatch_Tests()
        {
            var vresult = XBNFResult<bool>.Of(true);
            var fresult = XBNFResult<bool>.Of(FailedRecognitionError.Of("abc", 3));
            var presult = XBNFResult<bool>.Of(PartialRecognitionError.Of("abc", 3, 8));
            var nresult = new XBNFResult<bool>(null!);

            #region Match value
            string @out = null!;
            vresult.ConsumeMatch(
                r => @out = "result",
                f => @out = "failed",
                p => @out = "partial",
                () => @out = "null");
            Assert.AreEqual("result", @out);
            #endregion

            #region Match Failed
            fresult.ConsumeMatch(
                r => @out = "result",
                f => @out = "failed",
                p => @out = "partial",
                () => @out = "null");
            Assert.AreEqual("failed", @out);
            #endregion

            #region Match Partial
            presult.ConsumeMatch(
                r => @out = "result",
                f => @out = "failed",
                p => @out = "partial",
                () => @out = "null");
            Assert.AreEqual("partial", @out);
            #endregion

            #region Match null
            nresult.ConsumeMatch(
                r => @out = "result",
                f => @out = "failed",
                p => @out = "partial",
                () => @out = "null");
            Assert.AreEqual("null", @out);

            @out = "missed";
            nresult.ConsumeMatch(
                r => @out = "result",
                f => @out = "failed",
                p => @out = "partial");
            Assert.AreEqual("missed", @out);
            #endregion
        }

        [TestMethod]
        public void WithMatch_Tests()
        {
            var vresult = XBNFResult<bool>.Of(true);
            var fresult = XBNFResult<bool>.Of(FailedRecognitionError.Of("abc", 3));
            var presult = XBNFResult<bool>.Of(PartialRecognitionError.Of("abc", 3, 8));
            var nresult = new XBNFResult<bool>(null!);

            #region Match value
            string @out = null!;
            vresult.WithMatch(
                r => @out = "result",
                f => @out = "failed",
                p => @out = "partial",
                () => @out = "null");
            Assert.AreEqual("result", @out);
            #endregion

            #region Match Failed
            fresult.WithMatch(
                r => @out = "result",
                f => @out = "failed",
                p => @out = "partial",
                () => @out = "null");
            Assert.AreEqual("failed", @out);
            #endregion

            #region Match Partial
            presult.WithMatch(
                r => @out = "result",
                f => @out = "failed",
                p => @out = "partial",
                () => @out = "null");
            Assert.AreEqual("partial", @out);
            #endregion

            #region Match null
            nresult.WithMatch(
                r => @out = "result",
                f => @out = "failed",
                p => @out = "partial",
                () => @out = "null");
            Assert.AreEqual("null", @out);

            @out = "missed";
            nresult.WithMatch(
                r => @out = "result",
                f => @out = "failed",
                p => @out = "partial");
            Assert.AreEqual("missed", @out);
            #endregion
        }
    }
}
