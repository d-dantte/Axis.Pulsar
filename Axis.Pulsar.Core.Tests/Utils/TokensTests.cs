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
            Assert.AreEqual(@string.Length, ss.SourceSegment.Length);
            Assert.AreEqual(0, ss.SourceSegment.Offset);
            Assert.AreEqual(@string, ss.ToString());

            ss = new Tokens(@string, 5);
            Assert.AreEqual(5, ss.SourceSegment.Offset);
            Assert.AreEqual(@string.Length - 5, ss.SourceSegment.Length);
            Assert.AreEqual(@string[5..], ss.ToString());

            ss = new Tokens(@string, 8, 2);
            Assert.AreEqual(8, ss.SourceSegment.Offset);
            Assert.AreEqual(2, ss.SourceSegment.Length);

            Assert.ThrowsException<ArgumentNullException>(() => new Tokens(null!, 0, 0));
            Assert.ThrowsException<ArgumentException>(() => new Tokens(@string, -1, 0));
            Assert.ThrowsException<ArgumentException>(() => new Tokens(@string, 0, -1));
            Assert.ThrowsException<ArgumentException>(() => new Tokens(@string, 200, 0));
            Assert.ThrowsException<ArgumentException>(() => new Tokens(@string, 2, 200));
        }

        [TestMethod]
        public void Of_Tests()
        {
            var @string = "the quick brown fox jumps over the lazy dog";
            var ss = Tokens.Of(@string);

            Assert.AreEqual(@string.Length, ss.SourceSegment.Length);

            ss = Tokens.Of(@string, 5);
            Assert.AreEqual(@string.Length - 5, ss.SourceSegment.Length);

            ss = Tokens.Of(@string, 5, 2);
            Assert.AreEqual(2, ss.SourceSegment.Length);

            ss = Tokens.Of("");
            Assert.AreEqual(0, ss.SourceSegment.Length);
        }

        #endregion

        [TestMethod]
        public void Default_Tests()
        {
            var tokens = default(Tokens);
            Assert.AreEqual(0, tokens.SourceSegment.Offset);
            Assert.AreEqual(0, tokens.SourceSegment.Length);
            Assert.IsNull(tokens.Source);
            Assert.IsNotNull(tokens.GetHashCode());
            Assert.IsNull(tokens.ToString());
            Assert.IsTrue(tokens.IsDefault);
            Assert.AreEqual(tokens, Tokens.Default);
            Assert.IsFalse(tokens.IsEmpty);
        }


        [TestMethod]
        public void Empty_Tests()
        {
            var tokens = Tokens.Empty;
            Assert.AreEqual(0, tokens.SourceSegment.Offset);
            Assert.AreEqual(0, tokens.SourceSegment.Length);
            Assert.AreEqual(string.Empty, tokens.Source);
            Assert.IsNotNull(tokens.GetHashCode());
            Assert.AreEqual(string.Empty, tokens.ToString());
            Assert.IsFalse(tokens.IsDefault);
            Assert.IsTrue(tokens.IsEmpty);
            Assert.AreEqual(Tokens.Of("", 0, 0), tokens);
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
            Assert.AreEqual(2, ss2.SourceSegment.Length);
            Assert.AreEqual(' ', ss2[0]);
            Assert.AreEqual('q', ss2[1]);

            ss2 = ss.Slice(17);
            Assert.AreEqual(@string.Length - 17, ss2.SourceSegment.Length);
            Assert.AreEqual('o', ss2[0]);
            Assert.AreEqual('g', ss2[^1]);

            ss2 = ss[5..12];
            Assert.AreEqual(7, ss2.SourceSegment.Length);
            Assert.AreEqual('u', ss2[0]);
            Assert.AreEqual('r', ss2[^1]);

            ss = default;
            Assert.ThrowsException<ArgumentNullException>(() => ss.Slice(0));
            Assert.ThrowsException<ArgumentNullException>(() => ss.Slice(0, 4));

            ss = Tokens.Empty;
            ss2 = ss.Slice(0);
            Assert.AreEqual(ss, ss2);

            Assert.ThrowsException<ArgumentException>(() => ss.Slice(1));
        }

        [TestMethod]
        public void AsSpan_Tests()
        {
            var tokens = Tokens.Of("some string", 2, 3);
            var span = tokens.AsSpan();
            Assert.AreEqual(tokens.SourceSegment.Length, span.Length);
            Assert.IsTrue(Enumerable.SequenceEqual(
                span.ToArray(),
                tokens.ToArray()!));

            tokens = default;
            span = tokens.AsSpan();
            System.Diagnostics.Debug.WriteLine(span.ToString());
            System.Diagnostics.Debug.WriteLine(span.Length);
        }

        [TestMethod]
        public void IsSourceRefEqual_Tests()
        {
            var str = "the string";
            var t1 = Tokens.Of(str);
            var t2 = Tokens.Of(str);
            var t3 = Tokens.Of(new StringBuilder("the ").Append("string").ToString());

            Assert.IsTrue(t1.IsSourceRefEqual(t2));
            Assert.IsTrue(t2.IsSourceRefEqual(t1));
            Assert.IsFalse(t1.IsSourceRefEqual(t3));
            Assert.IsFalse(t2.IsSourceRefEqual(t3));

            Assert.AreEqual(t1, t2);
            Assert.AreEqual(t1, t3);
            Assert.AreEqual(t2, t3);
        }

        [TestMethod]
        public void IsSourceEqual_Tests()
        {
            var str = "the string";
            var t1 = Tokens.Of(str);
            var t3 = Tokens.Of(new StringBuilder("the ").Append("string").ToString());

            Assert.IsTrue(t1.IsSourceEqual(t3));
        }

        [TestMethod]
        public void IsRelative_Test()
        {
            var @string = "the string";
            var string2 = new StringBuilder("the string").ToString();
            var ss = new Tokens(@string);
            var ss2 = new Tokens(@string);
            var ss3 = new Tokens(string2);

            Assert.IsTrue(ss.IsSourceRefEqual(ss2));
            Assert.IsTrue(ss2.IsSourceRefEqual(ss));
            Assert.IsFalse(ss.IsSourceRefEqual(ss3));
            Assert.AreEqual(@string, string2);
        }

        [TestMethod]
        public void IsConsecutiveTo_tests()
        {
            var @string = "1234567890";
            var string2 = new StringBuilder("1234567890").ToString();
            var ss = Tokens.Of(@string, 0, 2);
            var ss2 = Tokens.Of(@string, 2, 1);
            var ss3 = Tokens.Of(@string, 3, 2);
            var ss4 = Tokens.Of(string2, 3, 2);

            Assert.IsTrue(ss.IsConsecutiveTo(ss2));
            Assert.IsTrue(ss2.IsConsecutiveTo(ss3));
            Assert.IsTrue(ss2.IsConsecutiveTo(ss4));
            Assert.IsFalse(ss2.IsConsecutiveTo(ss));
            Assert.IsFalse(ss3.IsConsecutiveTo(ss2));
            Assert.IsFalse(ss.IsConsecutiveTo(ss3));
        }

        [TestMethod]
        public void Intersects_Tests()
        {
            var @string = "1234567890";
            var string2 = new StringBuilder("1234567890").ToString();

            var token = Tokens.Of(@string, 0, 3);
            var token2 = Tokens.Of(string2, 2, 4);
            var token3 = Tokens.Of(@string, 1, 1);
            var empty = Tokens.Empty;
            var empty1 = Tokens.Of(@string, 4, 0);
            var empty2 = Tokens.Of(string2, 8, 0);
            var def = Tokens.Default;

            Assert.IsTrue(Tokens.Intersects(token, token2));
            Assert.IsTrue(Tokens.Intersects(token, token3));
            Assert.IsFalse(Tokens.Intersects(token2, token3));

            Assert.IsTrue(Tokens.Intersects(token, empty));
            Assert.IsTrue(Tokens.Intersects(token, empty1));
            Assert.IsTrue(Tokens.Intersects(token, empty2));

            Assert.IsTrue(Tokens.Intersects(token2, empty));
            Assert.IsTrue(Tokens.Intersects(token2, empty1));
            Assert.IsTrue(Tokens.Intersects(token2, empty2));

            Assert.IsTrue(Tokens.Intersects(token3, empty));
            Assert.IsTrue(Tokens.Intersects(token3, empty1));
            Assert.IsTrue(Tokens.Intersects(token3, empty2));

            Assert.IsFalse(Tokens.Intersects(def, default(Tokens)));
            Assert.IsFalse(Tokens.Intersects(token, def));
            Assert.IsFalse(Tokens.Intersects(token2, def));
            Assert.IsFalse(Tokens.Intersects(token3, def));

            Assert.IsFalse(Tokens.Intersects(empty, def));
            Assert.IsFalse(Tokens.Intersects(empty1, def));
            Assert.IsFalse(Tokens.Intersects(empty2, def));
        }

        [TestMethod]
        public void IsValueHashEquals_Tests()
        {
            Assert.IsTrue(Tokens.Default.IsValueHashEqual(Tokens.Default));

            var str = "abcdabcdabcd    abcd";
            var t1 = Tokens.Of(str, 0, 4);
            var t2 = Tokens.Of(str, 4, 4);
            var t3 = Tokens.Of(str, 9, 4);

            Assert.IsTrue(t1.IsValueHashEqual(t2));
            Assert.IsTrue(t2.IsValueHashEqual(t1));
            Assert.IsFalse(t1.IsValueHashEqual(t3));
            Assert.IsFalse(t2.IsValueHashEqual(t3));

            t1 = default;
            Assert.IsTrue(t1.IsValueHashEqual(default));

            t1 = Tokens.Empty;
            Assert.IsTrue(t1.IsValueHashEqual(Tokens.Empty));
        }

        [TestMethod]
        public void ExpandUsing_Tests()
        {
            var @string = "1234512345";
            var ss = Tokens.Of(@string, 0, 2);
            var ss2 = Tokens.Of(@string, 2, 1);
            var ss3 = ss.ConJoin(ss2);
            Assert.AreEqual(3, ss3.SourceSegment.Length);
            Assert.IsTrue(ss3.Equals("123"));
        }

        [TestMethod]
        public void Equality_Tests()
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

            Assert.IsTrue(a.Equals("om"));
            Assert.IsTrue(b.Equals(new[] { 'o', 'm' }));
            Assert.IsFalse(Tokens.Empty.Equals("om"));
            Assert.IsFalse(Tokens.Default.Equals("om"));
        }

        [TestMethod]
        public void Join_Tests()
        {
            var @string = "1234567890";
            var string2 = new StringBuilder("1234567890").ToString();
            var ss = Tokens.Of(@string, 0, 2);
            var ss2 = Tokens.Of(@string, 2, 1);
            var ss3 = Tokens.Of(@string, 3, 2);
            var ss5 = Tokens.Of(@string, 1, 1);
            var empty = Tokens.Empty;
            var empty2 = ss[0..0];

            Assert.ThrowsException<InvalidOperationException>(() =>
                Tokens.Default.ConJoin(ss));
            Assert.ThrowsException<InvalidOperationException>(() =>
                Tokens.Default.ConJoin(Tokens.Default));
            Assert.ThrowsException<InvalidOperationException>(() =>
                ss.ConJoin(Tokens.Default));
            Assert.ThrowsException<InvalidOperationException>(() =>
                ss.ConJoin(ss5));
            Assert.ThrowsException<InvalidOperationException>(() =>
                ss.ConJoin(ss));
            
            var result = ss.ConJoin(ss2);
            Assert.AreEqual(ss.SourceSegment.Length + ss2.SourceSegment.Length, result.SourceSegment.Length);
            Assert.AreEqual(ss.SourceSegment.Offset, result.SourceSegment.Offset);
        }

        [TestMethod]
        public void Merge_Tests()
        {
            var @string = "1234567890";
            var string2 = new StringBuilder("1234567890").ToString();
            var ss = Tokens.Of(@string, 0, 2);
            var ss2 = Tokens.Of(@string, 1, 2);
            var ss3 = Tokens.Of(@string, 3, 2);
            var ss4 = Tokens.Of(@string, 1, 1);
            var empty = Tokens.Empty;
            var empty2 = ss[0..0];

            Assert.ThrowsException<ArgumentException>(() =>
                Tokens.Default.Merge(ss));
            Assert.ThrowsException<ArgumentException>(() =>
                Tokens.Default.Merge(Tokens.Default));
            Assert.ThrowsException<ArgumentException>(() =>
                ss.Merge(Tokens.Default));
            Assert.ThrowsException<ArgumentException>(() =>
                ss.Merge(ss3));
            
            var result = ss.Merge(ss);
            Assert.AreEqual(ss, result);

            result = ss.Merge(empty);
            Assert.AreEqual(ss, result);

            result = ss.Merge(empty2);
            Assert.AreEqual(ss, result);

            result = ss.Merge(ss2);
            Assert.AreEqual(ss.SourceSegment.Offset, result.SourceSegment.Offset);
            Assert.AreEqual(3, result.SourceSegment.Length);

            result = ss.Merge(ss4);
            Assert.AreEqual(ss.SourceSegment.Offset, result.SourceSegment.Offset);
            Assert.AreEqual(ss.SourceSegment.Length, result.SourceSegment.Length);
        }

        [TestMethod]
        public void Split_Tests()
        {
            var source = Tokens.Of("the qick brown fox jumps over the lazy fat duck");
            var parts = source.Split(
                " ",
                "jumps",
                "ju");

            Assert.AreEqual(11, parts.Length);
            Assert.IsTrue(parts[5].Delimiter.Equals("jumps"));
        }
    }
}
