using Axis.Pulsar.Core.Utils;
using System.Text.RegularExpressions;

namespace Axis.Pulsar.Core.Tests.Utils
{
    [TestClass]
    public partial class TokenReaderTests
    {
        [TestMethod]
        public void Construction_Tests()
        {
            var source = "the quick brown fox jumps over the lazy dog";
            var reader = new TokenReader(source);
            Assert.AreEqual(source, reader.Source);
            Assert.AreEqual(0, reader.Position);
            Assert.IsFalse(reader.IsConsumed);

            Assert.ThrowsException<ArgumentNullException>(
                () => new TokenReader(null!));
        }

        [TestMethod]
        public void GetTokens_Tests()
        {
            var reader = new TokenReader("tokens");
            var tokens = reader.GetToken();

            Assert.AreEqual<Tokens>("t", tokens);
        }

        [TestMethod]
        public void TryGetTokens_Tests()
        {
            var reader = new TokenReader("tokens");

            Assert.IsTrue(reader.TryGetTokens(Tokens.Of("tok"), out var tokens));
            Assert.AreEqual<Tokens>("tok", tokens);

            Assert.IsFalse(reader.TryGetTokens(Tokens.Of("bleh"), out tokens));
            Assert.AreEqual<Tokens>("ens", tokens);
        }

        [TestMethod]
        public void PeekTokens_Tests()
        {
            var reader = new TokenReader("tokens");
            var tokens = reader.PeekTokens(3, false);
            Assert.AreEqual<Tokens>("tok", tokens);

            tokens = reader.PeekToken();
            Assert.AreEqual<Tokens>("t", tokens);

            Assert.ThrowsException<EndOfStreamException>(
                () => reader.PeekTokens(30, true));

            Assert.ThrowsException<ArgumentException>(
                () => reader.TryPeekTokens("", out tokens));
        }

        [TestMethod]
        public void TryPeekTokens_Tests()
        {
            var reader = new TokenReader("tokens");

            Assert.ThrowsException<ArgumentException>(
                () => reader.TryPeekTokens(Tokens.Default, out var tokens));

            Assert.IsTrue(reader.TryPeekTokens(Tokens.Of("tok"), out var tokens));
            Assert.AreEqual<Tokens>("tok", tokens);
            Assert.IsFalse(reader.TryPeekTokens(Tokens.Of("ble"), out tokens));
            Assert.IsFalse(reader.TryPeekTokens(Tokens.Of("blehritorious"), out tokens));
        }

        [TestMethod]
        public void TryPeekTokensCount_Tests()
        {
            var reader = new TokenReader("tokens");

            Assert.ThrowsException<ArgumentOutOfRangeException>(
                () => reader.TryPeekTokens(-1, true, out var tokens));
        }

        [TestMethod]
        public void TryPeekOrReadTokens_Tests()
        {
            var source = "the quick brown fox jumps over the lazy dog";
            var reader = new TokenReader(source);

            var success = reader.TryPeekToken(out var tokens);
            Assert.IsTrue(success);
            Assert.AreEqual(1, tokens.Segment.Count);
            Assert.AreEqual('t', tokens[0]);
            Assert.AreEqual(0, reader.Position);

            success = reader.TryGetToken(out var tokens2);
            Assert.IsTrue(success);
            Assert.AreEqual(1, tokens2.Segment.Count);
            Assert.AreEqual('t', tokens2[0]);
            Assert.AreEqual(tokens, tokens2);
            Assert.AreEqual(1, reader.Position);

            success = reader.TryPeekTokens(10, out tokens);
            Assert.IsTrue(success);
            Assert.AreEqual(10, tokens.Segment.Count);
            Assert.IsTrue(tokens.Equals("he quick b"));
            Assert.AreEqual(1, reader.Position);

            success = reader.TryGetTokens(10, out tokens2);
            Assert.IsTrue(success);
            Assert.AreEqual(10, tokens2.Segment.Count);
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
            Assert.AreEqual(41, tokens.Segment.Count);
            Assert.IsTrue(tokens.Equals("e quick brown fox jumps over the lazy dog"));
            Assert.AreEqual(2, reader.Position);

            success = reader.TryGetTokens(100, true, out _);
            Assert.IsFalse(success);
            success = reader.TryGetTokens(100, false, out tokens2);
            Assert.IsTrue(success);
            Assert.AreEqual(41, tokens2.Segment.Count);
            Assert.IsTrue(tokens2.Equals("e quick brown fox jumps over the lazy dog"));
            Assert.AreEqual(tokens, tokens2);
            Assert.AreEqual(43, reader.Position);

            reader.Reset(0);
            Assert.ThrowsException<EndOfStreamException>(() => reader.GetTokens(100, true));

            reader = "something";
            success = reader.TryGetTokens("some", out tokens);
            Assert.IsTrue(success);
            Assert.IsTrue(tokens.Equals("some"));

            var position = reader.Position;
            success = reader.TryGetTokens("thyne", out tokens);
            Assert.IsFalse(success);
            Assert.AreEqual(position, reader.Position);

            reader.Reset(0);
            var regex = MyRegex();
            success = reader.TryGetPattern(regex, out tokens);
            Assert.IsTrue(success);
            Assert.IsTrue(tokens.Equals("som"));
        }

        [TestMethod]
        public void Back_Tests()
        {
            var reader = new TokenReader("something");
            _ = reader.GetTokens(3, true);
            Assert.AreEqual(3, reader.Position);

            reader.Back();
            Assert.AreEqual(2, reader.Position);

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => reader.Back(-1));

            reader.Back(2);
            Assert.AreEqual(0, reader.Position);

            Assert.ThrowsException<InvalidOperationException>(() => reader.Back(1));
        }

        [TestMethod]
        public void Reset_Tests()
        {
            var reader = new TokenReader("something");
            _ = reader.GetTokens(3, true);
            Assert.AreEqual(3, reader.Position);

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => reader.Reset(20));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => reader.Reset(-1));

            reader.Reset(2);
            Assert.AreEqual(2, reader.Position);
        }

        [TestMethod]
        public void Advance_Tests()
        {
            var reader = new TokenReader("something");

            Assert.ThrowsException<ArgumentOutOfRangeException>(
                () => reader.Advance(-1));

            Assert.ThrowsException<InvalidOperationException>(
                () => reader.Advance(100));

            _ = reader.Advance(2);
            Assert.AreEqual(2, reader.Position);

            _ = reader.Advance();
            Assert.AreEqual(3, reader.Position);
        }

        [GeneratedRegex("[a-zA-Z]{1,3}")]
        private static partial Regex MyRegex();
    }
}
