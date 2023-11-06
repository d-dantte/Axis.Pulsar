using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Tests.Utils
{
    [TestClass]
    public class ExtensionsTests
    {
        [TestMethod]
        public void Intersects_Tests()
        {
            var t1 = (0, 4);
            var t2 = (0, 5);
            var t3 = (1, 2);
            var t4 = (4, 4);

            Assert.IsTrue(t1.Intersects(t2));
            Assert.IsTrue(t1.Intersects(t3));
            Assert.IsTrue(t1.Intersects(t4));
            Assert.IsTrue(t4.Intersects(t4));
            Assert.IsFalse(t3.Intersects(t4));
        }
    }
}
