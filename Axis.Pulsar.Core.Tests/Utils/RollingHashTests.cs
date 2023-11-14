using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Tests.Utils
{
    [TestClass]
    public class RollingHashTests
    {
        private static readonly string Text = "abcdef abc dkealabcabcdke abcabcabc";

        [TestMethod]
        public void Constructor_Tests()
        {
            var rollingHash = RollingHash.Of(Text, 0, 3);
            Assert.IsInstanceOfType<RollingHash.RollingWindowHash>(rollingHash);
            Assert.AreEqual(Text, rollingHash.Source);
            Assert.AreEqual(-1, rollingHash.Offset);
            Assert.AreEqual(3, rollingHash.WindowLength);

            rollingHash = RollingHash.Of(Text, 0, 1);
            Assert.IsInstanceOfType<RollingHash.RollingValueHash>(rollingHash);
            Assert.AreEqual(Text, rollingHash.Source);
            Assert.AreEqual(-1, rollingHash.Offset);
            Assert.AreEqual(1, rollingHash.WindowLength);

            Assert.ThrowsException<ArgumentException>(() => RollingHash.Of(default, 0, 3));
            Assert.ThrowsException<ArgumentException>(() => RollingHash.Of("", 0, 3));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => RollingHash.Of(Text, -2, 3));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => RollingHash.Of(Text, 0, 300));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => RollingHash.Of(Text, 0, 300));
        }

        [TestMethod]
        public void TryNextHash_Tests()
        {
            #region multiple char window tests
            var rollingHash = RollingHash.Of(Text, 0, 3);
            var hash = RollingHash.ComputeHash(Tokens.Of(Text, 0, 3));
            var successCount = 0;
            while (rollingHash.TryNext(out var newHash))
            {
                if (newHash == hash)
                    successCount++;
            }

            Assert.AreEqual(Text.Length - 3, rollingHash.Offset);
            Assert.AreEqual(7, successCount);


            rollingHash = RollingHash.Of(Text, 0, 4);
            hash = RollingHash.ComputeHash(Tokens.Of(Text, 0, 4));
            successCount = 0;
            while (rollingHash.TryNext(out var newHash))
            {
                if (newHash == hash)
                    successCount++;
            }

            Assert.AreEqual(Text.Length - 4, rollingHash.Offset);
            Assert.AreEqual(2, successCount);


            rollingHash = RollingHash.Of(Text, 0, 3);
            hash = RollingHash.ComputeHash(Tokens.Of(Text, 0, 3));
            var moved = rollingHash.TryNext(17, out var xhash);
            Assert.IsTrue(moved);
            Assert.AreEqual(hash, xhash);
            #endregion

            #region single char window tests
            rollingHash = RollingHash.Of(Text, 0, 1);
            hash = RollingHash.ComputeHash(Tokens.Of(Text, 0, 1));
            successCount = 0;
            while (rollingHash.TryNext(out var newHash))
            {
                if (newHash == hash)
                    successCount++;
            }

            Assert.AreEqual(Text.Length - 1, rollingHash.Offset);
            Assert.AreEqual(8, successCount);


            rollingHash = RollingHash.Of(Text, 3, 1);
            hash = RollingHash.ComputeHash(Tokens.Of(Text, 3, 1));
            moved = rollingHash.TryNext(9, out xhash);
            Assert.IsTrue(moved);
            Assert.AreEqual(hash, xhash);
            #endregion
        }
    }
}
