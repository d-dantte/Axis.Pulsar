using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Tests.Utils
{
    [TestClass]
    public class TokenReaderTests
    {
        [TestMethod]
        public void Construction_Tests()
        {
            var source = "the quick brown fox jumps over the lazy dog";
            var reader = new TokenReader(source);
            Assert.AreEqual(source, reader.Source);
            Assert.AreEqual(0, reader.Position);
            Assert.IsFalse(reader.IsConsumed);
        }

        [TestMethod]
        public void TryPeekOrReadTokens_Tests()
        {
            var source = "the quick brown fox jumps over the lazy dog";
            var reader = new TokenReader(source);

            var success = reader.TryPeekToken(out var tokens);
            Assert.IsTrue(success);
            Assert.AreEqual(1, tokens.Count);
            Assert.AreEqual('t', tokens[0]);
            Assert.AreEqual(0, reader.Position);

            success = reader.TryGetToken(out var tokens2);
            Assert.IsTrue(success);
            Assert.AreEqual(1, tokens2.Count);
            Assert.AreEqual('t', tokens2[0]);
            Assert.AreEqual(tokens, tokens2);
            Assert.AreEqual(1, reader.Position);

            success = reader.TryPeekTokens(10, out tokens);
            Assert.IsTrue(success);
            Assert.AreEqual(10, tokens.Count);
            Assert.IsTrue(tokens.Equals("he quick b"));
            Assert.AreEqual(1, reader.Position);

            success = reader.TryGetTokens(10, out tokens2);
            Assert.IsTrue(success);
            Assert.AreEqual(10, tokens2.Count);
            Assert.IsTrue(tokens2.Equals("he quick b"));
            Assert.AreEqual(tokens, tokens2);
            Assert.AreEqual(11, reader.Position);

            _ = reader.Back(2);
            Assert.AreEqual(9, reader.Position);

            _ = reader.Reset(2);
            Assert.AreEqual(2, reader.Position);


            success = reader.TryPeekTokens(100, true, out _);
            Assert.IsFalse(success);
            success = reader.TryPeekTokens(100, false, out tokens);
            Assert.IsTrue(success);
            Assert.AreEqual(41, tokens.Count);
            Assert.IsTrue(tokens.Equals("e quick brown fox jumps over the lazy dog"));
            Assert.AreEqual(2, reader.Position);

            success = reader.TryGetTokens(100, true, out _);
            Assert.IsFalse(success);
            success = reader.TryGetTokens(100, false, out tokens2);
            Assert.IsTrue(success);
            Assert.AreEqual(41, tokens2.Count);
            Assert.IsTrue(tokens2.Equals("e quick brown fox jumps over the lazy dog"));
            Assert.AreEqual(tokens, tokens2);
            Assert.AreEqual(43, reader.Position);

            reader.Reset(0);
            Assert.ThrowsException<InvalidOperationException>(() => reader.GetTokens(100, true));
        }
    }
}
