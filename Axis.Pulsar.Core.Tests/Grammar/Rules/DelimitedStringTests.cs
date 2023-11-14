using Axis.Luna.Common.Results;
using Axis.Luna.Common.Utils;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Rules;
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
            Assert.IsFalse(success);
            Assert.IsTrue(delim.Equals("the quick brown fox, "));

            dstring = DelimitedString.Of(
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
                true, "/*", "*/",
                Enumerable.Empty<Tokens>(),
                Enumerable.Empty<Tokens>(),
                Enumerable.Empty<CharRange>(),
                ArrayUtil.Of(CharRange.Of('0', '9')));

            inputString = "stuff 6 comment * bleh / finall */";
            success = dstring.TryRecognizeString(
                inputString,
                out delim);
            Assert.IsFalse(success);
            Assert.IsTrue(delim.Equals("stuff "));

            dstring = DelimitedString.Of(
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
        }

        [TestMethod]
        public void TryRecognize_Tests()
        {
            var path = ProductionPath.Of("a");
            var dstring = DelimitedString.Of(
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
            Assert.IsTrue(success);
            nodeResult.Consume(n => n.Tokens.Equals("'the \\\"quick\\\" brown fox, etc...'"));

            success = dstring.TryRecognize(
                "'something wonderful this way avoids'",
                path,
                null!,
                out nodeResult);
            Assert.IsTrue(success);
            nodeResult.Consume(n => n.Tokens.Equals("'something wonderful this way avoids'"));
        }
    }
}
