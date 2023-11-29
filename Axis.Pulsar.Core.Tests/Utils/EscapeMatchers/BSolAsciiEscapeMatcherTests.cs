using Axis.Pulsar.Core.Utils.EscapeMatchers;

namespace Axis.Pulsar.Core.Tests.Utils.EscapeMatchers;

[TestClass]
public class BSolAsciiEscapeMatcherTests
{
    [TestMethod]
    public void Decode_Tests()
    {
            var matcher = new BSolAsciiEscapeMatcher();

            var encoded = "\\n\\p\\a";
            var raw = matcher.Decode(encoded);
            Assert.AreEqual("\\x0a\\p\\x07", raw);

            encoded = "\\n";
            raw = matcher.Decode(encoded);
            Assert.AreEqual("\\x0a", raw);

            encoded = "\\p";
            raw = matcher.Decode(encoded);
            Assert.AreEqual("\\p", raw);
    }
}
