using System.Text;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Tests.Utils
{
    [TestClass]
    public class TokensTests
    {
        #region Construction

        [TestMethod]
        public void Constructor_Tests()
        {
            var @string = "the quick brown fox jumps over the lazy dog";

            var ss = new Tokens(@string);
            Assert.AreEqual(@string, ss.Source);
            Assert.AreEqual(@string.Length, ss.Segment.Count);
            Assert.AreEqual(0, ss.Segment.Offset);
            Assert.AreEqual(@string, ss.ToString());

            ss = new Tokens(@string, (8, 2));
            Assert.AreEqual(8, ss.Segment.Offset);
            Assert.AreEqual(2, ss.Segment.Count);

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Tokens(@string, (-1, 0)));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Tokens(@string, (0, -1)));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Tokens(@string, (200, 0)));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Tokens(@string, (2, 200)));
        }

        [TestMethod]
        public void Of_Tests()
        {
            var @string = "the quick brown fox jumps over the lazy dog";
            var ss = Tokens.Of(@string);

            Assert.AreEqual(@string.Length, ss.Segment.Count);

            ss = Tokens.Of(@string, 5);
            Assert.AreEqual(@string.Length - 5, ss.Segment.Count);

            ss = Tokens.Of(@string, 5, 2);
            Assert.AreEqual(2, ss.Segment.Count);

            ss = Tokens.Of("");
            Assert.AreEqual(0, ss.Segment.Count);
        }

        #endregion

        [TestMethod]
        public void Default_Tests()
        {
            var tokens = default(Tokens);
            Assert.AreEqual(0, tokens.Segment.Offset);
            Assert.AreEqual(0, tokens.Segment.Count);
            Assert.IsNull(tokens.Source);
            Assert.IsNotNull(tokens.GetHashCode());
            Assert.IsNull(tokens.ToString());
            Assert.IsTrue(tokens.IsDefault);
            Assert.AreEqual(tokens, Tokens.Default);
            Assert.IsTrue(tokens.IsEmpty);
            Assert.AreEqual(Tokens.Of(null!, (0, 0)), tokens);
        }

        [TestMethod]
        public void Expand_Tests2()
        {
            var str = "the quick brown fox jumps over something";
            var tokens = Tokens.Default;
            Assert.ThrowsException<InvalidOperationException>(() => tokens += 2);

            tokens = Tokens.EmptyAt(str, 0);
            tokens += 6;
            Assert.AreEqual(tokens, Tokens.Of(str, 0, 6));
        }

        [TestMethod]
        public void Implicit_Tests()
        {
            var @string = "some string";
            Tokens t = @string;
            Assert.AreEqual(@string, t.ToString());
            Assert.AreEqual(@string, t[..].ToString());
        }

        [TestMethod]
        public void Slice_Tests()
        {
            var @string = "the quick brown fox jumps over the lazy dog";
            var ss = Tokens.Of(@string);

            var ss2 = ss.Slice(3, 2);
            Assert.AreEqual(2, ss2.Segment.Count);
            Assert.AreEqual(' ', ss2[0]);
            Assert.AreEqual('q', ss2[1]);

            ss2 = ss.Slice(17);
            Assert.AreEqual(@string.Length - 17, ss2.Segment.Count);
            Assert.AreEqual('o', ss2[0]);
            Assert.AreEqual('g', ss2[^1]);

            ss2 = ss[5..12];
            Assert.AreEqual(7, ss2.Segment.Count);
            Assert.AreEqual('u', ss2[0]);
            Assert.AreEqual('r', ss2[^1]);

            ss = default;
            Assert.AreEqual(default(Tokens), ss.Slice(0));
            Assert.ThrowsException<InvalidOperationException>(() => ss.Slice(0, 4));
        }

        [TestMethod]
        public void AsSpan_Tests()
        {
            var tokens = Tokens.Of("some string", 2, 3);
            var span = tokens.AsSpan();
            Assert.AreEqual(tokens.Segment.Count, span.Length);
            Assert.IsTrue(Enumerable.SequenceEqual(
                span.ToArray(),
                tokens.ToArray()!));

            Assert.ThrowsException<InvalidOperationException>(() => default(Tokens).AsSpan());
        }

        [TestMethod]
        public void Intersects_Tests()
        {
            var @string = "1234567890";
            var string2 = new StringBuilder("1234567890").ToString();

            var token = Tokens.Of(@string, 0, 3);
            var token2 = Tokens.Of(string2, 2, 4);
            var token3 = Tokens.Of(@string, 1, 1);
            var def = Tokens.Default;

            Assert.IsTrue((bool)Tokens.Intersects(token, token2));
            Assert.IsTrue((bool)Tokens.Intersects(token, token3));
            Assert.IsFalse((bool)Tokens.Intersects(token2, token3));

            Assert.IsTrue((bool)Tokens.Intersects(def, default(Tokens)));
            Assert.IsFalse((bool)Tokens.Intersects(token, def));
            Assert.IsFalse((bool)Tokens.Intersects(token2, def));
            Assert.IsFalse((bool)Tokens.Intersects(token3, def));
        }

        [TestMethod]
        public void Merge_Tests()
        {
            var @string = "1234512345";
            var ss = Tokens.Of(@string, 0, 2);
            var ss2 = Tokens.Of(@string, 2, 1);
            var ss3 = ss.MergeWith(ss2);
            Assert.AreEqual(3, ss3.Segment.Count);
            Assert.IsTrue(ss3.Equals("123"));
        }

        [TestMethod]
        public void Equality_Tests()
        {
            var @default = default(Tokens);

            Assert.IsTrue(@default.Equals(@default));

            var str = "some string";
            var a = Tokens.Of(str, 1, 2);
            var b = Tokens.Of(str, 1, 2);
            var c = Tokens.Of("some string", 1, 2);
            var e = Tokens.Of(str, 4, 2);
            Tokens d = "om";

            Assert.IsTrue(a.Equals(a));
            Assert.IsTrue(a.Equals(b));
            Assert.IsTrue(a.Equals(c));
            Assert.IsTrue(c.Equals(b));
            Assert.IsTrue(d.Equals(c));

            Assert.IsTrue(a.Equals("om"));
            Assert.IsTrue(b.Equals(new[] { 'o', 'm' }));
            Assert.IsFalse(Tokens.Default.Equals("om"));
            Assert.IsFalse(e.Equals(a));
        }

        [TestMethod]
        public void Merge2_Tests()
        {
            var @string = "1234567890";
            var string2 = new StringBuilder("1234567890").ToString();
            var ss = Tokens.Of(@string, 0, 2);
            var ss2 = Tokens.Of(@string, 1, 2);
            var ss3 = Tokens.Of(@string, 3, 2);
            var ss4 = Tokens.Of(@string, 1, 1);
            var empty2 = ss[0..0];
            var def = default(Tokens);
            
            var result = ss.MergeWith(ss);
            Assert.AreEqual(ss, result);

            result = ss.MergeWith(empty2);
            Assert.AreEqual(ss, result);

            result = ss.MergeWith(ss2);
            Assert.AreEqual(ss.Segment.Offset, result.Segment.Offset);
            Assert.AreEqual(3, result.Segment.Count);

            result = ss.MergeWith(ss4);
            Assert.AreEqual(ss.Segment.Offset, result.Segment.Offset);
            Assert.AreEqual(ss.Segment.Count, result.Segment.Count);

            Assert.AreEqual(ss, ss.MergeWith(def));
            Assert.AreEqual(ss2, ss2.MergeWith(def));
            Assert.AreEqual(ss3, ss3.MergeWith(def));
            Assert.AreEqual(ss4, ss4.MergeWith(def));
            Assert.AreEqual(empty2, empty2.MergeWith(def));
        }

        [TestMethod]
        public void Split_Tests()
        {
            var source = Tokens.Of("the qick brown fox jumps over the lazy fat duck judgment");
            var parts = source.Split(
                " ",
                "jumps",
                "ju");

            Assert.AreEqual(13, parts.Length);
            Assert.IsTrue(parts[5].Delimiter.Equals("jumps"));
            Assert.IsTrue(parts[5].Tokens.Equals(string.Empty));
            Assert.IsTrue(parts[8].Delimiter.Equals(" "));
            Assert.IsTrue(parts[8].Tokens.Equals("lazy"));
        }
    }
}
