using Axis.Pulsar.Core.Utils.EscapeMatchers;
using System.Text.RegularExpressions;

namespace Axis.Pulsar.Core.Tests.Utils.EscapeMatchers
{
    [TestClass]
    public class BSolBasicEscapeMatcherTests
    {
        [TestMethod]
        public void Decode_Tests()
        {
            var matcher = new BSolBasicEscapeMatcher();

            var encoded = "\\n\\p\\a";
            var raw = matcher.Decode(encoded);
            Assert.AreEqual("\n\\p\a", raw);
        }

        [TestMethod]
        public void EscapePattern_Tests()
        {
            var Pattern = new Regex(
                "^\\\\['\"\\\\nrfbtv0a]\\z",
                RegexOptions.Compiled);

            var encoded = "\\'";
            var match = Pattern.Match(encoded);
            Assert.IsTrue(match.Success);

            encoded = "\\\"";
            match = Pattern.Match(encoded);
            Assert.IsTrue(match.Success);

            encoded = "\\\\";
            match = Pattern.Match(encoded);
            Assert.IsTrue(match.Success);

            encoded = "\\n";
            match = Pattern.Match(encoded);
            Assert.IsTrue(match.Success);

            encoded = "\\r";
            match = Pattern.Match(encoded);
            Assert.IsTrue(match.Success);

            encoded = "\\f";
            match = Pattern.Match(encoded);
            Assert.IsTrue(match.Success);

            encoded = "\\b";
            match = Pattern.Match(encoded);
            Assert.IsTrue(match.Success);

            encoded = "\\t";
            match = Pattern.Match(encoded);
            Assert.IsTrue(match.Success);

            encoded = "\\v";
            match = Pattern.Match(encoded);
            Assert.IsTrue(match.Success);

            encoded = "\\0";
            match = Pattern.Match(encoded);
            Assert.IsTrue(match.Success);

            encoded = "\\a";
            match = Pattern.Match(encoded);
            Assert.IsTrue(match.Success);
        }

        [TestMethod]
        public void RawPattern_Tests()
        {
            var Pattern = new Regex(
                "^['\"\\\\\n\r\f\b\t\v\0\a]\\z",
                RegexOptions.Compiled);

            var raw = "'";
            var match = Pattern.Match(raw);
            Assert.IsTrue(match.Success);

            raw = "\"";
            match = Pattern.Match(raw);
            Assert.IsTrue(match.Success);

            raw = "\\";
            match = Pattern.Match(raw);
            Assert.IsTrue(match.Success);

            raw = "\n";
            match = Pattern.Match(raw);
            Assert.IsTrue(match.Success);

            raw = "\r";
            match = Pattern.Match(raw);
            Assert.IsTrue(match.Success);

            raw = "\f";
            match = Pattern.Match(raw);
            Assert.IsTrue(match.Success);

            raw = "\b";
            match = Pattern.Match(raw);
            Assert.IsTrue(match.Success);

            raw = "\t";
            match = Pattern.Match(raw);
            Assert.IsTrue(match.Success);

            raw = "\v";
            match = Pattern.Match(raw);
            Assert.IsTrue(match.Success);

            raw = "\0";
            match = Pattern.Match(raw);
            Assert.IsTrue(match.Success);

            raw = "\a";
            match = Pattern.Match(raw);
            Assert.IsTrue(match.Success);

        }
    }
}
