using Axis.Luna.Common;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Atomic;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Tests.Grammar.Rules
{
    [TestClass]
    public class DelimitedStringTests
    {
        [TestMethod]
        public void TryRecognizeStartDelimiter_Tests()
        {
            var dstring = DelimitedString.Of(
                "d",
                true, "\"", "\"",
                Enumerable.Empty<Tokens>(),
                ArrayUtil.Of(Tokens.Of("xyz")),
                Enumerable.Empty<CharRange>(),
                Enumerable.Empty<CharRange>());

            var inputString = "\"the quick brown fox, etc...\"";

            var success = dstring.TryRecognizeStartDelimiter(
                inputString,
                out var delim);
            Assert.IsTrue(success);
            Assert.AreEqual('\"', delim[0]);
        }

        [TestMethod]
        public void TryRecognizeEndDelimiter_Tests()
        {
            var dstring = DelimitedString.Of(
                "d",
                true, "\"", "\"",
                Enumerable.Empty<Tokens>(),
                ArrayUtil.Of(Tokens.Of("xyz")),
                Enumerable.Empty<CharRange>(),
                Enumerable.Empty<CharRange>());

            var inputString = "\"the quick brown fox, etc...\"";

            var success = dstring.TryRecognizeEndDelimiter(
                inputString[28..],
                out var delim);
            Assert.IsTrue(success);
            Assert.AreEqual('\"', delim[0]);
        }

        [TestMethod]
        public void TryRecognizeString_Tests()
        {
            var dstring = DelimitedString.Of(
                "d",
                true, "\"", "\"",
                Enumerable.Empty<Tokens>(),
                ArrayUtil.Of(Tokens.Of("xyz")),
                Enumerable.Empty<CharRange>(),
                Enumerable.Empty<CharRange>(),
                "\\\"");
            var inputString = "\"the \\\"quick\\\" brown fox, etc...\"";
            var success = dstring.TryRecognizeString(
                inputString[1..],
                out var delim);
            Assert.IsTrue(success);
            Assert.IsTrue(delim.Equals("the \\\"quick\\\" brown fox, etc..."));

            inputString = "\"the quick brown fox, xyz etc...\"";
            success = dstring.TryRecognizeString(
                inputString[1..],
                out delim);
            Assert.IsTrue(success);
            Assert.IsTrue(delim.Equals("the quick brown fox, "));


            dstring = DelimitedString.Of(
                "d",
                true, "/*", "*/",
                Enumerable.Empty<Tokens>(),
                Enumerable.Empty<Tokens>(),
                Enumerable.Empty<CharRange>(),
                Enumerable.Empty<CharRange>(),
                "\\*/");
            inputString = "stuff comment * bleh / finall \\*/ */";
            success = dstring.TryRecognizeString(
                inputString,
                out delim);
            Assert.IsTrue(success);
            Assert.IsTrue(delim.Equals("stuff comment * bleh / finall \\*/ "));


            dstring = DelimitedString.Of(
                "d",
                true, "/*", "*/",
                Enumerable.Empty<Tokens>(),
                Enumerable.Empty<Tokens>(),
                Enumerable.Empty<CharRange>(),
                ArrayUtil.Of(CharRange.Of('0', '9')));
            inputString = "stuff 6 comment * bleh / finall */";
            success = dstring.TryRecognizeString(
                inputString,
                out delim);
            Assert.IsTrue(success);
            Assert.IsTrue(delim.Equals("stuff "));


            dstring = DelimitedString.Of(
                "d",
                true, "/*", "*/",
                new Tokens[] { "abc", "ab" },
                Enumerable.Empty<Tokens>(),
                Enumerable.Empty<CharRange>(),
                ArrayUtil.Of(CharRange.Of('0', '9')));
            inputString = "abcabcabcababababc*/";
            success = dstring.TryRecognizeString(
                inputString,
                out delim);
            Assert.IsTrue(success);
            Assert.IsTrue(delim.Equals("abcabcabcababababc"));
            inputString = "abcabcabcabababab9c*/";
            success = dstring.TryRecognizeString(
                inputString,
                out delim);
            Assert.IsTrue(success);
            Assert.IsTrue(delim.Equals("abcabcabcabababab"));


            dstring = DelimitedString.Of(
                "d",
                true, "//", null,
                Enumerable.Empty<Tokens>(),
                Enumerable.Empty<Tokens>(),
                Enumerable.Empty<CharRange>(),
                ArrayUtil.Of('\n', (CharRange)'\r'));
            inputString = "the quick brown fox...";
            success = dstring.TryRecognizeString(
                inputString,
                out delim);
            Assert.IsTrue(success);
            Assert.IsTrue(delim.Equals("the quick brown fox..."));

            inputString = "the quick brown\n fox...";
            success = dstring.TryRecognizeString(
                inputString,
                out delim);
            Assert.IsTrue(success);
            Assert.IsTrue(delim.Equals("the quick brown"));
        }

        [TestMethod]
        public void TryRecognize_Tests()
        {
            var path = SymbolPath.Of("a");
            var dstring = DelimitedString.Of(
                "d",
                true, "'", "'",
                Enumerable.Empty<Tokens>(),
                ArrayUtil.Of(Tokens.Of("xyz")),
                Enumerable.Empty<CharRange>(),
                Enumerable.Empty<CharRange>(),
                "\\\"");

            var inputString = "'the \\\"quick\\\" brown fox, etc...'";

            var success = dstring.TryRecognize(
                inputString,
                path,
                null!,
                out var nodeResult);
            Assert.IsTrue(nodeResult.Is(out ICSTNode node));
            Assert.IsTrue(node.Tokens.Equals("'the \\\"quick\\\" brown fox, etc...'"));

            success = dstring.TryRecognize(
                "'something wonderful this way avoids'",
                path,
                null!,
                out nodeResult);
            Assert.IsTrue(nodeResult.Is(out node));
            Assert.IsTrue(node.Tokens.Equals("'something wonderful this way avoids'"));
        }

        [TestMethod]
        public void TryRecognize_WithNoEndDelimiter_Tests()
        {
            Assert.ThrowsException<InvalidOperationException>(() => DelimitedString.Of(
                "d",
                true, "//", null,
                Enumerable.Empty<Tokens>(),
                Enumerable.Empty<Tokens>(),
                Enumerable.Empty<CharRange>(),
                Enumerable.Empty<CharRange>()));

            var path = SymbolPath.Of("a");
            var dstring = DelimitedString.Of(
                "d",
                true, "//", null,
                Enumerable.Empty<Tokens>(),
                ArrayUtil.Of<Tokens>("\r", "\n"),
                Enumerable.Empty<CharRange>(),
                Enumerable.Empty<CharRange>());

            var success = dstring.TryRecognize(
                "//stuff that is commented out",
                path,
                null!,
                out var result);
            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out ICSTNode node));
            Assert.IsTrue(node.Tokens.Equals("//stuff that is commented out"));

            TokenReader r = "//stuff that is commented out\nstuff not commented out";
            success = dstring.TryRecognize(
                r,
                path,
                null!,
                out result);
            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out node));
            Assert.IsTrue(node.Tokens.Equals("//stuff that is commented out"));
        }
    }
}
