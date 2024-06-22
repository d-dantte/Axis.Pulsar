using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Tests.Utils
{
    [TestClass]
    public class DeferredValueTests
    {
        [TestMethod]
        public void Constructor_Tests()
        {
            var value = new DeferredValue<int>(() => 5);

            Assert.IsFalse(value.IsGenerated);
            Assert.AreEqual(5, value.Value);
            Assert.AreEqual(5, value.Value);
            Assert.IsTrue(value.IsGenerated);
            Assert.IsTrue(value.TryValue(out var value2));
            Assert.AreEqual(5, value2);

            value = new DeferredValue<int>(() => throw new InvalidOperationException());
            Assert.ThrowsException<InvalidOperationException>(() => value.Value);
            Assert.ThrowsException<InvalidOperationException>(() => value.Value);
            Assert.IsFalse(value.TryValue(out value2));

            Func<int> generator = () => 8;
            value = generator;
            Assert.AreEqual(8, value.Value);
        }

        [TestMethod]
        public void Equals_Tests()
        {
            var value = new DeferredValue<int>(() => 1);
            var value2 = new DeferredValue<int>(() => 1);
            var value3 = new DeferredValue<int>(() => 3);
            var value4 = new DeferredValue<int>(() => throw new Exception());

            Assert.IsFalse(value.Equals(default(object?)));
            Assert.IsTrue(value!.Equals((object)value));

            Assert.IsFalse(value.Equals(null));
            Assert.IsTrue(value.Equals(value));
            Assert.IsTrue(value.Equals(value2));
            Assert.IsFalse(value.Equals(value3));
            Assert.IsTrue(value4.Equals(value4));
            Assert.IsFalse(value.Equals(value4));
        }
    }
}
