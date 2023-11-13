using Axis.Luna.Common.Utils;
using Axis.Pulsar.Core.Grammar.Rules;
using Axis.Pulsar.Core.Utils;
using Axis.Pulsar.Core.Utils.EscapeMatchers;

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
                Enumerable.Empty<CharRange>(),
                ArrayUtil.Of(new BSolAsciiEscapeMatcher()));

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
                Enumerable.Empty<CharRange>(),
                ArrayUtil.Of(new BSolAsciiEscapeMatcher()));

            var inputString = "\"the quick brown fox, etc...\"";

            var success = dstring.TryRecognizeEndDelimiter(
                inputString[28..],
                out var delim);
            Assert.IsTrue(success);
            Assert.AreEqual('\"', delim[0]);
        }

        [TestMethod]
        public void TryRecognizeEscapeSequence_Tests()
        {
            var dstring = DelimitedString.Of(
                true, "\"", "\"",
                Enumerable.Empty<Tokens>(),
                ArrayUtil.Of(Tokens.Of("xyz")),
                Enumerable.Empty<CharRange>(),
                Enumerable.Empty<CharRange>(),
                ArrayUtil.Of<IEscapeSequenceMatcher>(
                    new BSolAsciiEscapeMatcher(),
                    new BSolUTFEscapeMatcher(),
                    new BSolBasicEscapeMatcher()));

            var inputString = "\"the \\n \\x10 \\u0010 quick brown fox, etc...\"";

            var success = dstring.TryRecognizeEscapeSequence(
                inputString[5..],
                out var delim);
            Assert.IsTrue(success);
            Assert.IsTrue(delim.Equals("\\n"));

            success = dstring.TryRecognizeEscapeSequence(
                inputString[8..],
                out delim);
            Assert.IsTrue(success);
            Assert.IsTrue(delim.Equals("\\x10"));

            success = dstring.TryRecognizeEscapeSequence(
                inputString[13..],
                out delim);
            Assert.IsTrue(success);
            Assert.IsTrue(delim.Equals("\\u0010"));
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
                ArrayUtil.Of(new BSolAsciiEscapeMatcher()));

            var inputString = "\"the quick brown fox, etc...\"";

            var success = dstring.TryRecognizeString(
                inputString[1..],
                out var delim);
            Assert.IsTrue(success);
            Assert.IsTrue(delim.Equals("the quick brown fox, etc..."));

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
                ArrayUtil.Of(new BSolAsciiEscapeMatcher()));

            inputString = "stuff comment * bleh / finall */";
            success = dstring.TryRecognizeString(
                inputString,
                out delim);
            Assert.IsTrue(success);
            Assert.IsTrue(delim.Equals("stuff comment * bleh / finall "));

            dstring = DelimitedString.Of(
                true, "/*", "*/",
                Enumerable.Empty<Tokens>(),
                Enumerable.Empty<Tokens>(),
                Enumerable.Empty<CharRange>(),
                ArrayUtil.Of(CharRange.Of('0', '9')),
                ArrayUtil.Of(new BSolAsciiEscapeMatcher()));

            inputString = "stuff 6 comment * bleh / finall */";
            success = dstring.TryRecognizeString(
                inputString,
                out delim);
            Assert.IsFalse(success);
            Assert.IsTrue(delim.Equals("stuff "));
        }
    }

    [TestClass]
    public class SequenceMatcherTests
    {
        [TestMethod]
        public void TryNextWindow_Tests()
        {
            var sequenceMatcher = DelimitedString.SequenceMatcher.Of(
                ":",
                "So:",
                0);

            var moved = sequenceMatcher.TryNextWindow(out var isMatch);
            Assert.IsTrue(moved);
            Assert.IsFalse(isMatch);

            moved = sequenceMatcher.TryNextWindow(out isMatch);
            Assert.IsTrue(moved);
            Assert.IsFalse(isMatch);

            moved = sequenceMatcher.TryNextWindow(out isMatch);
            Assert.IsTrue(moved);
            Assert.IsTrue(isMatch);


            sequenceMatcher = DelimitedString.SequenceMatcher.Of(
                "a",
                "So, actuall, no body says yes and then says no",
                0);

            moved = sequenceMatcher.TryNextWindow(out isMatch);
            Assert.IsTrue(moved);
            Assert.IsFalse(isMatch);

            moved = sequenceMatcher.TryNextWindow(out isMatch);
            Assert.IsTrue(moved);
            Assert.IsFalse(isMatch);

            moved = sequenceMatcher.TryNextWindow(out isMatch);
            Assert.IsTrue(moved);
            Assert.IsFalse(isMatch);

            moved = sequenceMatcher.TryNextWindow(out isMatch);
            Assert.IsTrue(moved);
            Assert.IsFalse(isMatch);

            moved = sequenceMatcher.TryNextWindow(out isMatch);
            Assert.IsTrue(moved);
            Assert.IsTrue(isMatch);
        }
    }
}
