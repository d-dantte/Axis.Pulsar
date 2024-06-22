using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Grammar.Rules.Atomic;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Tests.Grammar.Rules.Atomic
{
    [TestClass]
    public class EOFTests
    {
        [TestMethod]
        public void Constructor_Tests()
        {
            var eof = new EOF("abc");
            Assert.IsNotNull(eof);
            Assert.AreEqual("abc", eof.Id);

            Assert.ThrowsException<ArgumentException>(() => new EOF(""));
            Assert.ThrowsException<ArgumentNullException>(() => new EOF(null!));
        }

        [TestMethod]
        public void TryRecognize_Tests()
        {
            var eof = new EOF("abc");

            Assert.ThrowsException<ArgumentNullException>(() => eof.TryRecognize(null!, "abc", null!, out _));

            var reader = new TokenReader("");
            var recognized = eof.TryRecognize(reader, "abc", null!, out var result);
            Assert.IsTrue(recognized);
            Assert.IsTrue(result.Is(out ISymbolNode value));
            Assert.IsTrue(value.Tokens.IsEmpty);
            Assert.AreEqual(0, value.Tokens.Segment.Offset);

            reader = "the end of the show";
            reader.Advance(19);
            recognized = eof.TryRecognize(reader, "abc", null!, out result);
            Assert.IsTrue(recognized);
            Assert.IsTrue(result.Is(out value));
            Assert.IsTrue(value is ISymbolNode.Atom);
            Assert.IsTrue(value.Tokens.IsEmpty);
            Assert.AreEqual(19, value.Tokens.Segment.Offset);

            reader.Reset();
            recognized = eof.TryRecognize(reader, "abc", null!, out result);
            Assert.IsFalse(recognized);
            Assert.IsTrue(result.Is(out FailedRecognitionError fre));
            Assert.AreEqual(0, fre.TokenSegment.Offset);
            Assert.AreEqual(1, fre.TokenSegment.Count);
        }
    }
}
