using System.Collections;
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

            ss = "stuff";
            Assert.AreEqual(5, ss.Segment.Count);
            Assert.AreEqual(0, ss.Segment.Offset);

            ss = default(string)!;
            Assert.IsTrue(ss.IsDefault);

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

            ss = Tokens.Of(null!, 0);
            Assert.IsTrue(ss.IsDefault);
        }

        #endregion

        [TestMethod]
        public void IntIndexer_Tests()
        {
            var tokens = Tokens.Of("bleh bleh");
            var empty = Tokens.Empty;

            Assert.ThrowsException<InvalidOperationException>(
                () => empty[0]);

            Assert.ThrowsException<IndexOutOfRangeException>(
                () => tokens[-1]);

            Assert.ThrowsException<IndexOutOfRangeException>(
                () => tokens[40]);

            var c = tokens[3];
            Assert.AreEqual('h', c);
        }

        [TestMethod]
        public void IndexIndexer_Tests()
        {
            var tokens = Tokens.Of("bleh bleh");

            var c = tokens[new Index(2)];
            Assert.AreEqual('e', c);

            c = tokens[^3];
            Assert.AreEqual('l', c);
        }

        [TestMethod]
        public void RangeIndexer_Tests()
        {
            var tokens = Tokens.Of("bleh bleh");
            var sub = tokens[..3];

            Assert.AreEqual<Tokens>("ble", sub);
        }

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
            Tokens ts = "some tokens";
            Assert.AreEqual<Tokens>("some tokens", ts);

            string sts = ts;
            Assert.AreEqual("some tokens", sts);
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
            var string2 = "other string source";
            var ss = Tokens.Of(@string, 0, 2);
            var ss2 = Tokens.Of(@string, 2, 1);
            var ss3 = Tokens.Merge(ss, ss2);
            var @default = Tokens.Default;
            var ss4 = @default.MergeWith(@default);
            var ss5 = @default.MergeWith(ss);
            Assert.AreEqual(ss, ss5);
            Assert.AreEqual(@default, ss4);
            Assert.AreEqual(3, ss3.Segment.Count);
            Assert.IsTrue(ss3.Equals("123"));

            Assert.ThrowsException<InvalidOperationException>(
                () => Tokens.Of(@string).MergeWith(Tokens.Of(string2)));
        }

        [TestMethod]
        public void Equality_Tests()
        {
            var @default = default(Tokens);
            var empty = Tokens.Empty;
            var tokens = Tokens.Of("the quick");
            var tokens2 = Tokens.Of("not the quick");
            var tokens3 = Tokens.Of(new StringBuilder().Append("the quick").ToString());

            Assert.IsTrue(tokens.Equals((object)tokens));
            Assert.IsFalse(tokens.Equals(544m));
            Assert.IsTrue(tokens.Equals(tokens));
            Assert.IsTrue(tokens != tokens2);
            Assert.IsFalse(tokens == tokens2);

            // (string))
            Assert.IsTrue(tokens.Equals("the quick"));
            Assert.IsTrue(@default.Equals(default(string)!));
            Assert.IsFalse(@default.Equals("the quick"));
            Assert.IsTrue(empty.Equals(string.Empty));
            Assert.IsFalse(empty.Equals("the quick"));

            // (char[])
            Assert.IsTrue(@default.Equals(default(char[])!));
            Assert.IsFalse(@default.Equals("the quick".ToCharArray()));
            Assert.IsFalse(tokens.Equals(default(char[])!));
            Assert.IsTrue(empty.Equals(Array.Empty<char>()));
            Assert.IsFalse(empty.Equals(default(char[])!));
            Assert.IsFalse(empty.Equals("stuff".ToCharArray()));
            Assert.IsTrue(tokens.Equals("the quick".ToCharArray()));
            Assert.IsFalse(tokens.Equals("not the quick".ToCharArray()));
            Assert.IsFalse(tokens.Equals(default(char[])!));

            // (Tokens, bool)
            Assert.IsFalse(@default.Equals("stuff", true));
            Assert.IsTrue(@default.Equals(@default, true));
            Assert.IsTrue(@default.Equals(@default, false));
            Assert.IsTrue(tokens.Equals(tokens3, true));
            Assert.IsTrue(tokens.Equals(tokens2[4..], true));
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

            Assert.AreEqual<Tokens>((ss + ss4), result);
        }

        [TestMethod]
        public void Enumerable_Tests()
        {
            var tokens = Tokens.Of("the quick");
            Assert.IsTrue(((IEnumerable<char>)tokens).SequenceEqual("the quick"));

            int index = 0;
            foreach(var c in ((IEnumerable)tokens))
            {
                Assert.AreEqual("the quick"[index++], c);
            }
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

            Assert.ThrowsException<ArgumentOutOfRangeException>(
                () => source.Split(-1, 2, " "));

            Assert.ThrowsException<ArgumentOutOfRangeException>(
                () => source.Split(200, 2, " "));

            Assert.ThrowsException<ArgumentOutOfRangeException>(
                () => source.Split(20, 60, " "));

            Assert.ThrowsException<ArgumentNullException>(
                () => source.Split(0, 4, default(Tokens[])!));

            Assert.ThrowsException<ArgumentException>(
                () => source.Split(0, 4, Array.Empty<Tokens>()));

            var ex = Assert.ThrowsException<ArgumentException>(
                () => source.Split(0, 4, "", Tokens.Default));
            Assert.AreEqual("Invalid delimiter: default/empty", ex.Message);

            parts = source.Split(
                4,
                " ",
                "jumps",
                "ju");

            Assert.AreEqual(12, parts.Length);
        }

        [TestMethod]
        public void ExpandBy_Tests()
        {
            var tokens = Tokens.Of("the quick brown fox", 2, 3);

            var expanded = tokens.ExpandBy(2);
            var expanded2 = tokens + 2;
            Assert.AreEqual(5, expanded.Segment.Count);
            Assert.AreEqual<Tokens>("e qui", expanded);
            Assert.AreEqual(expanded, expanded2);

            expanded = tokens.ExpandBy(-1);
            expanded2 = tokens - 1;
            Assert.AreEqual(2, expanded.Segment.Count);
            Assert.AreEqual<Tokens>("e ", expanded);
            Assert.AreEqual(expanded, expanded2);
        }

        [TestMethod]
        public void Contains_Tests()
        {
            var tokens = Tokens.Of("the quick brown fox");

            Assert.IsFalse(tokens.Contains(default(string)!));
            Assert.IsFalse(Tokens.Empty.Contains("stuff"));
            Assert.IsTrue(tokens.Contains("quick"));
            Assert.IsFalse(tokens.Contains("quickens"));

            Assert.IsFalse(tokens.Contains(Tokens.Default));
            Assert.IsFalse(Tokens.Empty.Contains(Tokens.Of("stuff")));
            Assert.IsTrue(tokens.Contains(Tokens.Of("quick")));
            Assert.IsFalse(tokens.Contains(Tokens.Of("quickens")));
            Assert.IsFalse(tokens.Contains(Tokens.Of("quickens the heart beat of its prey")));
        }

        [TestMethod]
        public void Contains_Character()
        {
            var tokens = Tokens.Of("the quick brown fox");

            Assert.IsTrue(tokens.Contains('q'));
            Assert.IsTrue(tokens.ContainsAny('z', 'f'));
            Assert.IsFalse(tokens.ContainsAny('z'));
            Assert.IsFalse(Tokens.Default.ContainsAny('5'));
        }

        [TestMethod]
        public void Precceeds_tests()
        {
            var original = Tokens.Of("the quick brown fox");
            var @default = Tokens.Default;
            var tokens = original[..3];
            var tokens2 = original[3..];
            var tokens3 = original[2..];
            var tokens4 = original[5..];

            Assert.IsFalse(original.Preceeds(@default));
            Assert.IsFalse(@default.Preceeds(@default));
            Assert.IsTrue(tokens.Preceeds(tokens2));
            Assert.IsTrue(tokens2.Succeeds(tokens));
            Assert.IsFalse(tokens.Preceeds(tokens3));
            Assert.IsFalse(tokens.Preceeds(tokens4));
            Assert.IsFalse(tokens.Preceeds("bleh"));
        }

        [TestMethod]
        public void StartsWith_Tests()
        {
            Assert.IsTrue(default(Tokens).StartsWith(default));
            Assert.IsFalse(Tokens.Empty.StartsWith(Tokens.Default));
            Assert.IsTrue(Tokens.Empty.StartsWith(Tokens.Empty));
            Assert.IsFalse(Tokens.Of("abc").StartsWith(Tokens.Of("abcde")));
            Assert.IsTrue(Tokens.Of("abcd").StartsWith(Tokens.Of("ab")));
        }

        [TestMethod]
        public void EndsWith_Tests()
        {
            Assert.IsTrue(default(Tokens).EndsWith(default));
            Assert.IsFalse(Tokens.Empty.EndsWith(Tokens.Default));
            Assert.IsTrue(Tokens.Empty.EndsWith(Tokens.Empty));
            Assert.IsFalse(Tokens.Of("abc").EndsWith(Tokens.Of("abcde")));
            Assert.IsTrue(Tokens.Of("abcd").EndsWith(Tokens.Of("cd")));
        }
    }

    [TestClass]
    public class TokenEnumeratorTest
    {
        [TestMethod]
        public void MoveNext_Test()
        {
            var token = Tokens.Empty;
            var enumerator = token.GetEnumerator();
            Assert.IsFalse(enumerator.MoveNext());
            Assert.IsFalse(enumerator.MoveNext());
        }

        [TestMethod]
        public void Reset_Tests()
        {
            var token = Tokens.Of(" ");
            var enumerator = token.GetEnumerator();

            enumerator.MoveNext();
            Assert.IsFalse(enumerator.MoveNext());

            enumerator.Reset();
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsFalse(enumerator.MoveNext());
        }

        [TestMethod]
        public void Dispose_Tests()
        {
            var token = Tokens.Of(" ");
            var enumerator = token.GetEnumerator();
            enumerator.Dispose();

            Assert.ThrowsException<InvalidOperationException>(
                () => enumerator.MoveNext());
        }
    }
}
