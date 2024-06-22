using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Grammar.Rules.Atomic;
using Axis.Pulsar.Core.Utils;
using static Axis.Pulsar.Core.Grammar.Rules.Atomic.DelimitedContent;

namespace Axis.Pulsar.Core.Tests.Grammar.Rules.Atomic
{
    [TestClass]
    public class DelimitedContentTests
    {
        [TestMethod]
        public void Constructor_Tests()
        {
            var delimiter = new DelimiterInfo("'");
            var constraint = new LegalCharacterRanges(delimiter, "a", "t-v");

            var dc = new DelimitedContent("xyz", true, delimiter, delimiter, constraint);
            Assert.IsNotNull(dc);
            Assert.AreEqual(delimiter, dc.StartDelimiter);
            Assert.AreEqual(delimiter, dc.EndDelimiter);
            Assert.AreEqual(constraint, dc.ContentConstraint);
            Assert.IsTrue(dc.AcceptsEmptyContent);
            Assert.AreEqual("xyz", dc.Id);

            dc = new DelimitedContent(
                "xyz", true, delimiter,
                new LegalCharacterRanges(delimiter, "a", "t-v"));
            Assert.IsNotNull(dc);

            dc = Of(
                "xyz", true, delimiter, delimiter,
                new LegalCharacterRanges(delimiter, "a", "t-v"));
            Assert.IsNotNull(dc);

            dc = Of(
                "xyz", true, delimiter,
                new LegalCharacterRanges(delimiter, "a", "t-v"));
            Assert.IsNotNull(dc);
        }

        [TestMethod]
        public void TryRecognizeDelimiter_Tests()
        {
            var delimiter = new DelimiterInfo("'", "\\'");

            var recognized = TryRecognizeDelimiter("", delimiter, out var tokens);
            Assert.IsFalse(recognized);
            Assert.IsTrue(tokens.IsEmpty);

            recognized = TryRecognizeDelimiter("adg", delimiter, out tokens);
            Assert.IsFalse(recognized);
            Assert.IsTrue(tokens.IsEmpty);

            recognized = TryRecognizeDelimiter("'", delimiter, out tokens);
            Assert.IsTrue(recognized);
            Assert.AreEqual<Tokens>("'", tokens);
        }

        [TestMethod]
        public void TryRecognizeContent_Tests()
        {
            var delimiter = new DelimiterInfo("'", "\\'");
            var constraint = new LegalCharacterRanges(delimiter, "\0-\uffff");

            var recognized = TryRecognizeContent("", constraint, true, out var tokens);
            Assert.IsTrue(recognized);
            Assert.IsTrue(tokens.IsEmpty);

            recognized = TryRecognizeContent("", constraint, false, out tokens);
            Assert.IsFalse(recognized);
            Assert.IsTrue(tokens.IsEmpty);

            recognized = TryRecognizeContent("abcd\uf2fc", constraint, false, out tokens);
            Assert.IsTrue(recognized);
            Assert.AreEqual<Tokens>("abcd\uf2fc", tokens);
        }

        [TestMethod]
        public void TryRecognize_Tests()
        {
            var delimiter = new DelimiterInfo("'", "\\'");
            var constraint = new LegalCharacterRanges(delimiter, "\0-\uffff");
            var dc = new DelimitedContent("xyz", true, delimiter, delimiter, constraint);
            var dc2 = new DelimitedContent("xyz", false, delimiter, delimiter, constraint);

            Assert.ThrowsException<ArgumentNullException>(() => dc.TryRecognize(null!, "abc", null!, out _));

            var recognized = dc.TryRecognize("", "abc", null!, out var result);
            Assert.IsFalse(recognized);
            Assert.IsTrue(result.Is(out FailedRecognitionError _));

            recognized = dc.TryRecognize("'", "abc", null!, out result);
            Assert.IsFalse(recognized);
            Assert.IsTrue(result.Is(out PartialRecognitionError _));

            recognized = dc2.TryRecognize("''", "abc", null!, out result);
            Assert.IsFalse(recognized);
            Assert.IsTrue(result.Is(out PartialRecognitionError _));

            recognized = dc.TryRecognize("'abc xyz", "abc", null!, out result);
            Assert.IsFalse(recognized);
            Assert.IsTrue(result.Is(out PartialRecognitionError _));

            recognized = dc.TryRecognize("'abc xyz'", "abc", null!, out result);
            Assert.IsTrue(recognized);
            Assert.IsTrue(result.Is(out ISymbolNode _));

            recognized = dc.TryRecognize("''", "abc", null!, out result);
            Assert.IsTrue(recognized);
            Assert.IsTrue(result.Is(out ISymbolNode _));
        }
    }

    [TestClass]
    public class DelimiterInfoTests
    {
        [TestMethod]
        public void Constructor_Tests()
        {
            var info = new DelimiterInfo();
            Assert.IsTrue(info.IsDefault);
            Assert.IsNull(info.Delimiter);
            Assert.IsNull(info.EscapeSequence);

            info = new DelimiterInfo("*/");
            Assert.IsFalse(info.IsDefault);
            Assert.AreEqual("*/", info.Delimiter);
            Assert.IsNull(info.EscapeSequence);

            info = new DelimiterInfo("*/", "\\*/");
            Assert.IsFalse(info.IsDefault);
            Assert.AreEqual("*/", info.Delimiter);
            Assert.AreEqual("\\*/", info.EscapeSequence);

            Assert.ThrowsException<ArgumentNullException>(() => new DelimiterInfo(null!, null));
            Assert.ThrowsException<ArgumentException>(() => new DelimiterInfo("", null));
            Assert.ThrowsException<ArgumentException>(() => new DelimiterInfo("abc", "*abx"));

            info = default;
            Assert.IsTrue(info.IsDefault);
            Assert.AreEqual(DelimiterInfo.Default, info);
        }

        [TestMethod]
        public void MatchesEndOfTokens_Tests()
        {
            var withoutEscape = new DelimiterInfo("*/");
            var withEscape = new DelimiterInfo("*/", "\\*/");

            var result = withoutEscape.MatchesEndOfTokens("abcd*/");
            Assert.IsTrue(result);

            result = withEscape.MatchesEndOfTokens("abcd*/");
            Assert.IsTrue(result);

            result = withoutEscape.MatchesEndOfTokens("abcd\\*/");
            Assert.IsTrue(result);

            result = withEscape.MatchesEndOfTokens("abcd\\*/");
            Assert.IsFalse(result);

            result = withEscape.MatchesEndOfTokens("abcd");
            Assert.IsFalse(result);

            result = withoutEscape.MatchesEndOfTokens("abcd");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TryGetIndexInTokens_Tests()
        {
            var withoutEscape = new DelimiterInfo("*/");
            var withEscape = new DelimiterInfo("*/", "\\*/");

            var result = withoutEscape.TryIndexOfDelimiterInTokens("lorem ipsum*/ lorem ipsum", out var index);
            Assert.IsTrue(result);
            Assert.AreEqual(11, index);

            result = withEscape.TryIndexOfDelimiterInTokens("lorem ipsum*/ lorem ipsum", out index);
            Assert.IsTrue(result);
            Assert.AreEqual(11, index);

            result = withoutEscape.TryIndexOfDelimiterInTokens("lorem ipsum lorem ipsum", out index);
            Assert.IsFalse(result);
            Assert.AreEqual(-1, index);

            result = withEscape.TryIndexOfDelimiterInTokens("lorem ipsum lorem ipsum", out index);
            Assert.IsFalse(result);
            Assert.AreEqual(-1, index);

            result = withEscape.TryIndexOfDelimiterInTokens("lorem ipsum \\*/lorem ipsum", out index);
            Assert.IsFalse(result);
            Assert.AreEqual(-1, index);

            result = withEscape.TryIndexOfDelimiterInTokens("lorem ipsum \\*/lorem ipsum*/", out index);
            Assert.IsTrue(result);
            Assert.AreEqual(26, index);

            result = withEscape.TryIndexOfDelimiterInTokens("lorem ipsum \\*/lorem ipsum\\*/other random stuff*/", out index);
            Assert.IsTrue(result);
            Assert.AreEqual(47, index);
        }

        [TestMethod]
        public void TryNextIndexInTokens_Tests()
        {
            var withoutEscape = new DelimiterInfo("*/");
            var withEscape = new DelimiterInfo("*/", "\\*/");

            var result = withoutEscape.TryNextIndexOfDelimiterInTokens("lorem ipsum lorem ipsum", 0, out var index);
            Assert.IsFalse(result);
            Assert.AreEqual(-1, index);

            result = withEscape.TryNextIndexOfDelimiterInTokens("lorem ipsum lorem ipsum", 0, out index);
            Assert.IsFalse(result);
            Assert.AreEqual(-1, index);

            result = withoutEscape.TryNextIndexOfDelimiterInTokens("lorem ipsum */lorem ipsum", 0, out index);
            Assert.IsTrue(result);
            Assert.AreEqual(12, index);

            result = withEscape.TryNextIndexOfDelimiterInTokens("lorem ipsum */lorem ipsum", 0, out index);
            Assert.IsTrue(result);
            Assert.AreEqual(12, index);

            result = withoutEscape.TryNextIndexOfDelimiterInTokens("lorem ipsum \\*/lorem ipsum", 0, out index);
            Assert.IsTrue(result);
            Assert.AreEqual(13, index);

            result = withEscape.TryNextIndexOfDelimiterInTokens("lorem ipsum \\*/lorem ipsum", 0, out index);
            Assert.IsFalse(result);
            Assert.AreEqual(13, index);

        }
    }

    [TestClass]
    public class LiteralPatternTests
    {
        [TestMethod]
        public void Constructor_Tests()
        {
            var pattern = new LiteralPattern("stuff");
            Assert.IsNotNull(pattern);
            Assert.AreEqual(5, pattern.Length);
            Assert.ThrowsException<ArgumentNullException>(() => new LiteralPattern(null!));
        }

        [TestMethod]
        public void Matches_Tests()
        {
            var pattern = new LiteralPattern("stuff");
            Assert.IsTrue(pattern.Matches("stuff"));
            Assert.IsFalse(pattern.Matches("Stuff"));

            pattern = new LiteralPattern("stuff", false);
            Assert.IsTrue(pattern.Matches("stuff"));
            Assert.IsTrue(pattern.Matches("StuFF"));
        }
    }

    [TestClass]
    public class WildcardPatternTests
    {
        [TestMethod]
        public void Constructor_Tests()
        {
            var pattern = new WildcardPattern("stuff");
            Assert.IsNotNull(pattern);
            Assert.AreEqual(5, pattern.Length);
            Assert.ThrowsException<ArgumentException>(() => new WildcardPattern(WildcardExpression.Default));
        }

        [TestMethod]
        public void Matches_Tests()
        {
            var pattern = new WildcardPattern("stuff");
            Assert.IsTrue(pattern.Matches("stuff"));
            Assert.IsFalse(pattern.Matches("Stuff"));
        }
    }

    [TestClass]
    public class LegalCharacterRangesTests
    {
        [TestMethod]
        public void Constructor_Tests()
        {
            var lcr = new LegalCharacterRanges(
                new DelimiterInfo("^"),
                "a-g", "x", "p-s");

            Assert.IsNotNull(lcr);
            Assert.IsNotNull(lcr.EndDelimiter);
            Assert.AreEqual(3, lcr.Ranges.Length);

            Assert.ThrowsException<ArgumentNullException>(() => new LegalCharacterRanges(default, null!));
            Assert.ThrowsException<InvalidOperationException>(() => new LegalCharacterRanges(default));
            Assert.ThrowsException<InvalidOperationException>(() => new LegalCharacterRanges(default, "a", default));
        }

        [TestMethod]
        public void ReadValidTokens_Tests()
        {
            var lcr = new LegalCharacterRanges(
                new DelimiterInfo("^"),
                "a-g", "x", "p-s");

            var lcr2 = new LegalCharacterRanges(
                new DelimiterInfo("^", "\\^"),
                "a-g", "x", "p-s", "\\", "^");

            var tokens = lcr.ReadValidTokens("abcab");
            Assert.AreEqual<Tokens>("abcab", tokens);

            tokens = lcr.ReadValidTokens("abcaqarbj");
            Assert.AreEqual<Tokens>("abcaqarb", tokens);

            tokens = lcr.ReadValidTokens("abcaq^arbj");
            Assert.AreEqual<Tokens>("abcaq", tokens);

            tokens = lcr2.ReadValidTokens("abcd\\^eqs^bleh");
            Assert.AreEqual<Tokens>("abcd\\^eqs", tokens);
        }
    }

    [TestClass]
    public class IllegalCharacterRangesTests
    {
        [TestMethod]
        public void Constructor_Tests()
        {
            var lcr = new IllegalCharacterRanges(
                new DelimiterInfo("^"),
                "a-g", "x", "p-s");

            Assert.IsNotNull(lcr);
            Assert.IsNotNull(lcr.EndDelimiter);
            Assert.AreEqual(3, lcr.Ranges.Length);

            Assert.ThrowsException<ArgumentNullException>(() => new IllegalCharacterRanges(default, null!));
            Assert.ThrowsException<InvalidOperationException>(() => new IllegalCharacterRanges(default));
            Assert.ThrowsException<InvalidOperationException>(() => new IllegalCharacterRanges(default, "a", default));
        }

        [TestMethod]
        public void ReadValidTokens_Tests()
        {
            var lcr = new IllegalCharacterRanges(
                new DelimiterInfo("^"),
                "a-g", "x", "p-s");

            var lcr2 = new IllegalCharacterRanges(
                new DelimiterInfo("^", "\\^"),
                "a-g", "x", "p-s");

            var tokens = lcr.ReadValidTokens("hijkl");
            Assert.AreEqual<Tokens>("hijkl", tokens);

            tokens = lcr.ReadValidTokens("hijkla");
            Assert.AreEqual<Tokens>("hijkl", tokens);

            tokens = lcr.ReadValidTokens("hijkl^mno");
            Assert.AreEqual<Tokens>("hijkl", tokens);

            tokens = lcr2.ReadValidTokens("hijkl\\^mno^tuvw");
            Assert.AreEqual<Tokens>("hijkl\\^mno", tokens);
        }
    }

    [TestClass]
    public class LegalDiscretePatternsTests
    {
        [TestMethod]
        public void Constructor_Tests()
        {
            var lcr = new LegalDiscretePatterns(
                new LiteralPattern("abcd"),
                new LiteralPattern("ab"),
                new WildcardPattern("abcde"));

            Assert.IsNotNull(lcr);
            Assert.AreEqual(3, lcr.Patterns.Length);

            Assert.ThrowsException<ArgumentNullException>(() => new LegalDiscretePatterns(default!));
            Assert.ThrowsException<InvalidOperationException>(() => new LegalDiscretePatterns());
            Assert.ThrowsException<InvalidOperationException>(() => new LegalDiscretePatterns(null!, null!));
        }

        [TestMethod]
        public void ReadValidTokens_Tests()
        {
            var lcr = new LegalDiscretePatterns(
                new LiteralPattern("abcd"),
                new LiteralPattern("ab"),
                new WildcardPattern("12345"));

            var tokens = lcr.ReadValidTokens("abcdabababab");
            Assert.AreEqual<Tokens>("abcdabababab", tokens);

            tokens = lcr.ReadValidTokens("12345abcdaqarbj");
            Assert.AreEqual<Tokens>("12345abcd", tokens);

            tokens = lcr.ReadValidTokens("12345aacaq^arbj");
            Assert.AreEqual<Tokens>("12345", tokens);

            tokens = lcr.ReadValidTokens("");
            Assert.IsTrue(tokens.IsEmpty);
        }
    }

    [TestClass]
    public class IllegalDiscretePatternTests
    {
        [TestMethod]
        public void Constructor_Tests()
        {
            var lcr = new IllegalDiscretePatterns(
                new DelimiterInfo("^", "\\^"),
                new LiteralPattern("abcd"),
                new LiteralPattern("ab"),
                new WildcardPattern("abcde"));

            Assert.IsNotNull(lcr);
            Assert.AreEqual(3, lcr.Patterns.Length);

            Assert.ThrowsException<ArgumentNullException>(() => new IllegalDiscretePatterns(new DelimiterInfo("^", "\\^"), default!));
            Assert.ThrowsException<InvalidOperationException>(() => new IllegalDiscretePatterns(new DelimiterInfo("^", "\\^")));
            Assert.ThrowsException<InvalidOperationException>(() => new IllegalDiscretePatterns(new DelimiterInfo("^", "\\^"), null!, null!));
        }

        [TestMethod]
        public void ReadValidTokens_Tests()
        {
            var lcr = new IllegalDiscretePatterns(
                new DelimiterInfo("^", "\\^"),
                new LiteralPattern("abcd"),
                new LiteralPattern("ab"),
                new WildcardPattern("12345"));

            var tokens = lcr.ReadValidTokens("abcdabababab");
            Assert.IsTrue(tokens.IsEmpty);

            tokens = lcr.ReadValidTokens("xyz9887^");
            Assert.AreEqual<Tokens>("xyz9887", tokens);
        }
    }
}
