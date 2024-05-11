using Axis.Luna.Common;
using Axis.Pulsar.Core.Grammar.Atomic;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Tests.Grammar.Atomic
{
    [TestClass]
    public class DelimitedStringTests
    {
        [TestMethod]
        public void Constructor_Tests()
        {
            var delimitedString = DelimitedString.Of(
                "xyz", true, "/*", "*/",
                ArrayUtil.Of<Tokens>(),
                ArrayUtil.Of<Tokens>(),
                ArrayUtil.Of(CharRange.Parse("1-4")),
                ArrayUtil.Of<CharRange>());
            Assert.IsNotNull(delimitedString);
            Assert.IsTrue(delimitedString.AcceptsEmptyString);

            Assert.ThrowsException<ArgumentNullException>(() => DelimitedString.Of(
                "xyz", true, "/*", "*/",
                null!,
                ArrayUtil.Of<Tokens>(),
                ArrayUtil.Of(CharRange.Parse("1-4")),
                ArrayUtil.Of<CharRange>()));

            Assert.ThrowsException<ArgumentException>(() => DelimitedString.Of(
                "xyz", true, "/*", "*/",
                ArrayUtil.Of(default(Tokens)),
                ArrayUtil.Of<Tokens>(),
                ArrayUtil.Of(CharRange.Parse("1-4")),
                ArrayUtil.Of<CharRange>()));

            Assert.ThrowsException<ArgumentException>(() => DelimitedString.Of(
                "xyz", true, "/*", "*/",
                ArrayUtil.Of(Tokens.Of("")),
                ArrayUtil.Of<Tokens>(),
                ArrayUtil.Of(CharRange.Parse("1-4")),
                ArrayUtil.Of<CharRange>()));

            Assert.ThrowsException<ArgumentNullException>(() => DelimitedString.Of(
                "xyz", true, "/*", "*/",
                ArrayUtil.Of<Tokens>(),
                null!,
                ArrayUtil.Of(CharRange.Parse("1-4")),
                ArrayUtil.Of<CharRange>()));

            Assert.ThrowsException<ArgumentException>(() => DelimitedString.Of(
                "xyz", true, "/*", "*/",
                ArrayUtil.Of<Tokens>(),
                ArrayUtil.Of(default(Tokens)),
                ArrayUtil.Of(CharRange.Parse("1-4")),
                ArrayUtil.Of<CharRange>()));

            Assert.ThrowsException<ArgumentException>(() => DelimitedString.Of(
                "xyz", true, "/*", "*/",
                ArrayUtil.Of<Tokens>(),
                ArrayUtil.Of(Tokens.Of("")),
                ArrayUtil.Of(CharRange.Parse("1-4")),
                ArrayUtil.Of<CharRange>()));

            Assert.ThrowsException<ArgumentNullException>(() => DelimitedString.Of(
                "xyz", true, "/*", "*/",
                ArrayUtil.Of<Tokens>(),
                ArrayUtil.Of<Tokens>(),
                null!,
                ArrayUtil.Of<CharRange>()));

            Assert.ThrowsException<ArgumentNullException>(() => DelimitedString.Of(
                "xyz", true, "/*", "*/",
                ArrayUtil.Of<Tokens>(),
                ArrayUtil.Of<Tokens>(),
                ArrayUtil.Of<CharRange>(),
                null!));
        }

        [TestMethod]
        public void TryParseStartDelimiter_Tests()
        {
            var delimitedString = DelimitedString.Of(
                "xyz", true, "/*", "*/",
                ArrayUtil.Of<Tokens>(),
                ArrayUtil.Of<Tokens>(),
                ArrayUtil.Of(CharRange.Parse("1-4")),
                ArrayUtil.Of<CharRange>());

            var recognized = delimitedString.TryRecognizeStartDelimiter("/*", out var tokens);
            Assert.IsTrue(recognized);
            Assert.IsTrue(tokens.Equals("/*"));

            recognized = delimitedString.TryRecognizeStartDelimiter("", out tokens);
            Assert.IsFalse(recognized);

            recognized = delimitedString.TryRecognizeStartDelimiter("bleh", out tokens);
            Assert.IsFalse(recognized);
        }

        [TestMethod]
        public void TryParseEndDelimiter_Tests()
        {
            var delimitedString = DelimitedString.Of(
                "xyz", true, "/*", "*/",
                ArrayUtil.Of<Tokens>(),
                ArrayUtil.Of<Tokens>(),
                ArrayUtil.Of(CharRange.Parse("1-4")),
                ArrayUtil.Of<CharRange>());

            var recognized = delimitedString.TryRecognizeEndDelimiter("*/", out var tokens);
            Assert.IsTrue(recognized);
            Assert.IsTrue(tokens.Equals("*/"));

            recognized = delimitedString.TryRecognizeStartDelimiter("", out tokens);
            Assert.IsFalse(recognized);

            recognized = delimitedString.TryRecognizeStartDelimiter("bleh", out tokens);
            Assert.IsFalse(recognized);
        }

        [TestMethod]
        public void TryRecognizeStringWithoutLegalSequence_Tests()
        {
            var delimitedString = DelimitedString.Of(
                "xyz", true, "/*", "*/",
                ArrayUtil.Of<Tokens>(),
                ArrayUtil.Of<Tokens>(),
                ArrayUtil.Of(CharRange.Parse("1-4")),
                ArrayUtil.Of<CharRange>());

            var recognized = delimitedString.TryRecognizeStringWithoutLegalSequence(
                "1241213144322321234433424123",
                out var tokens);
            Assert.IsTrue(recognized);
            Assert.IsTrue(tokens.Equals("1241213144322321234433424123"));


            recognized = delimitedString.TryRecognizeStringWithoutLegalSequence(
                "1241213144322321234433424123555",
                out tokens);
            Assert.IsTrue(recognized);
            Assert.IsTrue(tokens.Equals("1241213144322321234433424123"));
        }

        [TestMethod]
        public void TryRecognizeStringWithoutLegalSequence_WithNullEndDelimiter_Tests()
        {
            //var delimitedString = DelimitedString.Of(
            //    "xyz", true, "/*", null,
            //    ArrayUtil.Of<Tokens>(),
            //    ArrayUtil.Of<Tokens>(),
            //    ArrayUtil.Of(CharRange.Parse("1-4")),
            //    ArrayUtil.Of<CharRange>());

            //var recognized = delimitedString.TryRecognizeStringWithoutLegalSequence(
            //    "1241213144322321234433424123",
            //    out var tokens);
            //Assert.IsTrue(recognized);
            //Assert.IsTrue(tokens.Equals("1241213144322321234433424123"));
        }
    }
}
