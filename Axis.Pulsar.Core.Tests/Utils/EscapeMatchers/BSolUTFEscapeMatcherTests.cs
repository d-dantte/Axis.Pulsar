using Axis.Pulsar.Core.Utils.EscapeMatchers;

namespace Axis.Pulsar.Core.Tests.Utils.EscapeMatchers;

[TestClass]
public class BSolUTFEscapeMatcherTests
{
    [TestMethod]
    public void Decode_Tests()
    {
        var matcher = new BSolUTFEscapeMatcher();

        var encoded = "\\u000a\\u000ip\\u0007";
        var raw = matcher.Decode(encoded);
        Assert.AreEqual("\n\\u000ip\a", raw);

        encoded = "\\u000a";
        raw = matcher.Decode(encoded);
        Assert.AreEqual("\n", raw);

        encoded = "\\p";
        raw = matcher.Decode(encoded);
        Assert.AreEqual("\\p", raw);
    }

    [TestMethod]
    public void Encode_Tests()
    {
        var matcher = new BSolUTFEscapeMatcher();

        var encoded = "\n\\u000ip\a" ;
        var raw = matcher.Encode(encoded);
        Assert.AreEqual("\\u000a\\u000ip\\u0007", raw);

        encoded = "\n";
        raw = matcher.Encode(encoded);
        Assert.AreEqual("\\u000a", raw);

        encoded = "the quck brown fox jumps over the lazy duckling";
        raw = matcher.Encode(encoded);
        Assert.AreEqual(encoded, raw);

        encoded = "the quck brown fox\n jumps over the lazy duckling";
        raw = matcher.Encode(encoded);
        Assert.AreEqual(
            "the quck brown fox\\u000a jumps over the lazy duckling",
            raw);
    }
}
