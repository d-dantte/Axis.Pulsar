using Axis.Luna.Common;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Atomic;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Tests.Grammar.Atomic
{
    [TestClass]
    public class CharacterRangesTests
    {
        [TestMethod]
        public void Constructor_Tests()
        {
            var ranges = CharacterRanges.Of("id", "1-3", "a-t");
            Assert.IsNotNull(ranges);
            Assert.AreEqual("id", ranges.Id);
            Assert.AreEqual(2, ranges.IncludeList.Length);
            Assert.AreEqual(0, ranges.ExcludeList.Length);

            ranges = CharacterRanges.Of(
                "id",
                ArrayUtil.Of<CharRange>("1-3", "a-t"),
                ArrayUtil.Of<CharRange>("u-z", "\u0111-\u1000"));
            Assert.IsNotNull(ranges);
            Assert.AreEqual("id", ranges.Id);
            Assert.AreEqual(2, ranges.IncludeList.Length);
            Assert.AreEqual(2, ranges.ExcludeList.Length);

            Assert.ThrowsException<ArgumentNullException>(
                () => new CharacterRanges("id", null!, Enumerable.Empty<CharRange>()));

            Assert.ThrowsException<ArgumentNullException>(
                () => new CharacterRanges("id", Enumerable.Empty<CharRange>(), null!));
        }

        [TestMethod]
        public void TryRecognize_Tests()
        {
            var ranges = CharacterRanges.Of("id", "1-3", "a-t");
            var ranges2 = CharacterRanges.Of(
                "id",
                ArrayUtil.Of<CharRange>("1-3", "a-t"),
                ArrayUtil.Of<CharRange>("v-z", "\u0111-\u1000"));

            Assert.ThrowsException<ArgumentNullException>(
                () => ranges.TryRecognize(null!, "abc", null!, out _));

            var recognized = ranges.TryRecognize("", "abc", null!, out var result);
            Assert.IsFalse(recognized);
            Assert.IsTrue(result.Is(out FailedRecognitionError fre));
            Assert.AreEqual(0, fre.TokenSegment.Offset);

            recognized = ranges.TryRecognize("v", "abc", null!, out result);
            Assert.IsFalse(recognized);
            Assert.IsTrue(result.Is(out fre));
            Assert.AreEqual(0, fre.TokenSegment.Offset);

            recognized = ranges.TryRecognize("u", "abc", null!, out result);
            Assert.IsFalse(recognized);
            Assert.IsTrue(result.Is(out fre));
            Assert.AreEqual(0, fre.TokenSegment.Offset);

            recognized = ranges.TryRecognize("a", "abc", null!, out result);
            Assert.IsTrue(recognized);
            Assert.IsTrue(result.Is(out ISymbolNode node));
            Assert.IsTrue(node is ISymbolNode.Atom);
            Assert.AreEqual<Tokens>("a", node.Tokens);
            Assert.AreEqual(0, node.Tokens.Segment.Offset);
        }

        [TestMethod]
        public void ParseRanges_Tests()
        {
            var ranges = CharacterRanges.ParseRanges("3-5, a-c");
            Assert.AreEqual(2, ranges.Length);

            Assert.ThrowsException<FormatException>(() => CharacterRanges.ParseRanges("a-v, "));
        }
    }
}
