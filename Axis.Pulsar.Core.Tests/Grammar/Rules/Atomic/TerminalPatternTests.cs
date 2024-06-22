using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Grammar.Rules.Atomic;
using Axis.Pulsar.Core.Utils;
using System.Text.RegularExpressions;

namespace Axis.Pulsar.Core.Tests.Grammar.Rules.Atomic
{
    [TestClass]
    public class TerminalPatternTests
    {
        [TestMethod]
        public void Constructor_Tests()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () => new TerminalPattern("id", null!, IMatchType.Open.Default));

            Assert.ThrowsException<ArgumentNullException>(
                () => new TerminalPattern("id", new Regex("abc"), null!));

            Assert.ThrowsException<InvalidOperationException>(
                () => new TerminalPattern("id", new Regex("abc"), new FauxMatchType()));

            Assert.ThrowsException<ArgumentException>(
                () => new TerminalPattern("...", new Regex("abc"), IMatchType.Open.Default));

            var pattern = TerminalPattern.Of("id", new Regex("abc"), IMatchType.Open.Default);
            Assert.IsNotNull(pattern);
            Assert.IsNotNull(pattern.Pattern);
            Assert.IsNotNull(pattern.MatchType);
            Assert.AreEqual("id", pattern.Id);
        }

        [TestMethod]
        public void TryRecognizeWithOpen_Tests()
        {
            var open = TerminalPattern.Of("id", new Regex("^\\w+$"), IMatchType.Of(0));
            var openEmpty = TerminalPattern.Of("id", new Regex("^\\w+$"), IMatchType.Of(0, true));
            var open1 = TerminalPattern.Of("id", new Regex("^\\.\\w+$"), IMatchType.Of(1, true));

            Assert.ThrowsException<ArgumentNullException>(
                () => open.TryRecognize(null!, "abcd", null!, out _));

            var recognized = open.TryRecognize("abcd123 ", "sym", null!, out var result);
            Assert.IsTrue(recognized);
            Assert.IsTrue(result.Is(out ISymbolNode node));
            Assert.AreEqual<Tokens>("abcd123", node.Tokens);

            recognized = open.TryRecognize("abcd123", "sym", null!, out result);
            Assert.IsTrue(recognized);
            Assert.IsTrue(result.Is(out node));
            Assert.AreEqual<Tokens>("abcd123", node.Tokens);

            recognized = open.TryRecognize("", "sym", null!, out result);
            Assert.IsFalse(recognized);
            Assert.IsTrue(result.Is(out FailedRecognitionError fre));
            Assert.AreEqual(0, fre.TokenSegment.Offset);

            recognized = openEmpty.TryRecognize("", "sym", null!, out result);
            Assert.IsTrue(recognized);
            Assert.IsTrue(result.Is(out node));
            Assert.IsTrue(node.Tokens.IsEmpty);

            recognized = open1.TryRecognize(".me", "sym", null!, out result);
            Assert.IsTrue(recognized);
            Assert.IsTrue(result.Is(out node));
            Assert.AreEqual<Tokens>(".me", node.Tokens);
        }

        [TestMethod]
        public void TryRecognizeWithClosed_Tests()
        {
            var close = TerminalPattern.Of("id", new Regex("^abc$"), IMatchType.Of(3, 3));
            var close2 = TerminalPattern.Of("id", new Regex("abc(xyz)?"), IMatchType.Of(3, 6));

            var recognized = close.TryRecognize("abc", "sym", null!, out var result);
            Assert.IsTrue(recognized);
            Assert.IsTrue(result.Is(out ISymbolNode node));
            Assert.AreEqual<Tokens>("abc", node.Tokens);

            recognized = close2.TryRecognize("abcdef", "sym", null!, out result);
            Assert.IsTrue(recognized);
            Assert.IsTrue(result.Is(out node));
            Assert.AreEqual<Tokens>("abc", node.Tokens);

            recognized = close.TryRecognize("xyz", "sym", null!, out result);
            Assert.IsFalse(recognized);
            Assert.IsTrue(result.Is(out FailedRecognitionError fre));
            Assert.AreEqual(0, fre.TokenSegment.Offset);
        }

        internal class FauxMatchType : IMatchType
        {
        }
    }

    [TestClass]
    public class OpenMatchTypeTests
    {
        [TestMethod]
        public void Constructor_Tests()
        {
            var open = new IMatchType.Open(3, true);
            Assert.AreEqual(3, open.MaxMismatch);
            Assert.IsTrue(open.AllowsEmptyTokens);
            Assert.IsFalse(open.IsDefault);

            open = IMatchType.Open.Default;
            Assert.AreEqual(0, open.MaxMismatch);
            Assert.IsFalse(open.AllowsEmptyTokens);
            Assert.IsTrue(open.IsDefault);

            Assert.ThrowsException<ArgumentOutOfRangeException>(
                () => new IMatchType.Open(-1, false));
        }

        [TestMethod]
        public void GetHashCode_Tests()
        {
            var open = new IMatchType.Open(3, true);
            var hash = HashCode.Combine(open.MaxMismatch, open.AllowsEmptyTokens);
            Assert.AreEqual(hash, open.GetHashCode());
        }

        [TestMethod]
        public void Equals_Tests()
        {
            var open = new IMatchType.Open(3, true);
            var defaultOpen = IMatchType.Open.Default;

            Assert.IsTrue(open.Equals((object)open));
            Assert.IsFalse(open.Equals(new object()));
            Assert.IsFalse(open.Equals(new IMatchType.Open(2, false)));
            Assert.IsFalse(open.Equals(new IMatchType.Open(3, false)));
            Assert.IsFalse(open == defaultOpen);
            Assert.IsTrue(open != defaultOpen);
        }

        [TestMethod]
        public void ToString_Tests()
        {
            Assert.AreEqual(
                "3,*",
                new IMatchType.Open(3, true).ToString());
            Assert.AreEqual(
                "3,+",
                new IMatchType.Open(3, false).ToString());
        }
    }

    [TestClass]
    public class CloseMatchTypeTests
    {
        [TestMethod]
        public void Constructor_Tests()
        {
            var closed = new IMatchType.Closed(1, 3);
            Assert.AreEqual(1, closed.MinMatch);
            Assert.AreEqual(3, closed.MaxMatch);
            Assert.IsFalse(closed.IsDefault);
            Assert.IsTrue(IMatchType.Closed.Default.IsDefault);

            Assert.ThrowsException<ArgumentOutOfRangeException>(
                () => new IMatchType.Closed(-1, 3));
            Assert.ThrowsException<ArgumentOutOfRangeException>(
                () => new IMatchType.Closed(2, -3));
            Assert.ThrowsException<ArgumentException>(
                () => new IMatchType.Closed(5, 2));
        }

        [TestMethod]
        public void GetHashCode_Tests()
        {
            var closed = new IMatchType.Closed(3, 6);
            var hash = HashCode.Combine(closed.MinMatch, closed.MaxMatch);
            Assert.AreEqual(hash, closed.GetHashCode());
        }

        [TestMethod]
        public void Equals_Tests()
        {
            var closed = new IMatchType.Closed(3, 6);
            var defaultClosed = IMatchType.Closed.Default;

            Assert.IsTrue(closed.Equals((object)closed));
            Assert.IsFalse(closed.Equals(new object()));
            Assert.IsFalse(closed.Equals(new IMatchType.Closed(2, 6)));
            Assert.IsFalse(closed.Equals(new IMatchType.Closed(3, 7)));
            Assert.IsFalse(closed == defaultClosed);
            Assert.IsTrue(closed != IMatchType.Closed.DefaultMatch);
        }

        [TestMethod]
        public void ToString_Tests()
        {
            Assert.AreEqual(
                "3,6",
                new IMatchType.Closed(3, 6).ToString());
        }
    }
}
