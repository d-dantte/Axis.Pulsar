using Axis.Pulsar.Core.Utils;
using System.Collections.Immutable;

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

        [TestMethod]
        public void IndexOf_Tests()
        {
            Assert.ThrowsException<ArgumentOutOfRangeException>(
                () => "abc".AsSpan().IndexOf("abc", -1, StringComparison.Ordinal));
            Assert.ThrowsException<ArgumentOutOfRangeException>(
                () => "abc".AsSpan().IndexOf("abc", 10, StringComparison.Ordinal));

            var index = "abc".AsSpan().IndexOf("ab", 0, StringComparison.InvariantCulture);
            Assert.AreEqual(0, index);
        }

        [TestMethod]
        public void TryIndexOf_Tests()
        {
            var success = "abcabc".AsSpan().TryNextIndexOf("b", 2, StringComparison.InvariantCulture, out var index);
            Assert.IsTrue(success);
            Assert.AreEqual(2, index);

            success = "abc".AsSpan().TryNextIndexOf("z", 2, StringComparison.InvariantCulture, out index);
            Assert.IsFalse(success);
            Assert.AreEqual(-1, index);
        }

        [TestMethod]
        public void InsertItem_Tests()
        {
            var list = new List<int> { 0 };
            var l = list.InsertItem(0, 5);
            Assert.AreEqual(list, l);
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(5, list[0]);
        }

        [TestMethod]
        public void DefaultOrSequenceEqual_Tests()
        {
            var arr = new[] { 1, 2, 3 }.ToImmutableArray();
            var arr2 = new[] { 1, 2 }.ToImmutableArray();
            var defaultArr = default(ImmutableArray<int>);

            Assert.IsTrue(arr.DefaultOrSequenceEqual(arr));
            Assert.IsFalse(arr.DefaultOrSequenceEqual(arr2));
            Assert.IsFalse(arr.DefaultOrSequenceEqual(defaultArr));
            Assert.IsTrue(defaultArr.DefaultOrSequenceEqual(defaultArr));
        }
    }
}
