using Axis.Luna.Common;

namespace Axis.Pulsar.Core.XBNF.Tests
{
    [TestClass]
    public class ExtensionTests
    {
        [TestMethod]
        public void ThrowIfDuplicate_Tests()
        {
            var array = ArrayUtil.Of(1, 2, 3, 3, 4, 5);
            var ex = Assert.ThrowsException<Exception>(
                () => array
                    .ThrowIfDuplicate(v => new Exception(v.ToString()))
                    .ToList()); // force enumeration
            Assert.AreEqual("3", ex.Message);
        }
    }
}
