using Axis.Misc.Pulsar.Utils;
using System.Text;

namespace Axis.Pulsar.Core.Tests.Utils
{
    [TestClass]
    public class StringSegmentTests
    {
        [TestMethod]
        public void Constructor_Tests()
        {
            var @string = "the quick brown fox jumps over the lazy dog";
            var ss = new Tokens(@string);

            Assert.AreEqual(@string.Length, ss.Count);

            ss = new Tokens(@string, 5);
            Assert.AreEqual(@string.Length - 5, ss.Count);

            ss = new Tokens(@string, 5, 2);
            Assert.AreEqual(2, ss.Count);

            Assert.ThrowsException<ArgumentNullException>(() => new Tokens(null, 0, 0));
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

            Assert.AreEqual(@string.Length, ss.Count);

            ss = Tokens.Of(@string, 5);
            Assert.AreEqual(@string.Length - 5, ss.Count);

            ss = Tokens.Of(@string, 5, 2);
            Assert.AreEqual(2, ss.Count);

            ss = Tokens.Of("");
            Assert.AreEqual(0, ss.Count);
        }

        [TestMethod]
        public void Implicit_Tests()
        {
            var @string = "the quick brown fox jumps over the lazy dog";
            Tokens ss = @string;
            Assert.AreEqual(@string.Length, ss.Count);
        }

        [TestMethod]
        public void Slice_Tests()
        {
            var @string = "the quick brown fox jumps over the lazy dog";
            var ss = Tokens.Of(@string);

            var ss2 = ss.Slice(3, 2);
            Assert.AreEqual(2, ss2.Count);
            Assert.AreEqual(' ', ss2[0]);
            Assert.AreEqual('q', ss2[1]);

            ss2 = ss.Slice(17);
            Assert.AreEqual(@string.Length - 17, ss2.Count);
            Assert.AreEqual('o', ss2[0]);
            Assert.AreEqual('g', ss2[^1]);

            ss2 = ss[5..12];
            Assert.AreEqual(7, ss2.Count);
            Assert.AreEqual('u', ss2[0]);
            Assert.AreEqual('r', ss2[^1]);
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
        public void IsContiguous_tests()
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
        public void Equality_tests()
        {
            var @string = "1234512345";
            Tokens ss = Tokens.Of(@string, 0, 3);
            Tokens ss2 = Tokens.Of(@string, 5, 3);
            Assert.AreEqual(ss, ss2);
            Assert.AreEqual("123", ss);
            Assert.AreEqual("123", ss2);
            Assert.IsTrue(ss.Equals(new[] { '1', '2', '3' }));
        }

        [TestMethod]
        public void ExpandUsing_Tests()
        {
            var @string = "1234512345";
            var ss = Tokens.Of(@string, 0, 2);
            var ss2 = Tokens.Of(@string, 2, 1);
            var ss3 = ss.CombineWith(ss2);
            Assert.AreEqual(3, ss3.Count);
            Assert.IsTrue(ss3.Equals("123"));
        }
    }
}
