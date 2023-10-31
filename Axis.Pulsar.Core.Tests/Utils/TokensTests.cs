using Axis.Misc.Pulsar.Utils;

namespace Axis.Pulsar.Core.Tests.Utils
{
    [TestClass]
    public class TokensTests
    {
        [TestMethod]
        public void EqualityTests()
        {
            var empty = Tokens.Empty;
            var @default = default(Tokens);

            Assert.IsFalse(@default.Equals(empty));
            Assert.IsTrue(@default.Equals(@default));
            Assert.IsTrue(empty.Equals(empty));

            var str = "some string";
            var a = Tokens.Of(str, 1, 2);
            var b = Tokens.Of(str, 1, 2);
            var c = Tokens.Of("some string", 1, 2);
            Tokens d = "om";

            Assert.IsTrue(a.Equals(a));
            Assert.IsTrue(a.Equals(b));
            Assert.IsTrue(a.Equals(c));
            Assert.IsTrue(c.Equals(b));
            Assert.IsTrue(d.Equals(c));
        }
    }
}
