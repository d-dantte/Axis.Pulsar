using Axis.Luna.Result;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Tests.Utils
{
    [TestClass]
    public class WildcardExpressionTests
    {
        [TestMethod]
        public void Default_Tests()
        {
            // default
            Assert.IsTrue(WildcardExpression.Default.IsDefault);
            Assert.AreEqual(0, default(WildcardExpression).Length);
            Assert.IsTrue(WildcardExpression.Default.Equals(WildcardExpression.Default));
        }

        [TestMethod]
        public void Parse_Tests()
        {
            var result = WildcardExpression.Parse("abcd", true);
            Assert.IsTrue(result.IsDataResult());

            var exp = result.Resolve();
            Assert.IsNotNull(exp);
            Assert.AreEqual(4, exp.Length);
            Assert.IsTrue(exp.IsCaseSensitive);
            Assert.IsFalse(exp.IsDefault);

            result = WildcardExpression.Parse("abc.d", false);
            Assert.IsTrue(result.IsDataResult());

            exp = result.Resolve();
            Assert.IsNotNull(exp);
            Assert.AreEqual(5, exp.Length);
            Assert.IsFalse(exp.IsCaseSensitive);

            result = WildcardExpression.Parse("abc\\.d\\\\ef");
            Assert.IsTrue(result.IsDataResult());

            exp = result.Resolve();
            Assert.IsNotNull(exp);
            Assert.AreEqual(8, exp.Length);

            result = WildcardExpression.Parse("abc\\.d\\#ef");
            Assert.IsTrue(result.IsErrorResult());
            Assert.ThrowsException<FormatException>(() => result.Resolve());
        }

        [TestMethod]
        public void Implicit_Tests()
        {
            WildcardExpression exp = "abc\\.d\\\\ef";
            Assert.AreEqual(8, exp.Length);
        }

        [TestMethod]
        public void Equality_Tests()
        {
            WildcardExpression exp = "abc\\.d\\\\ef";
            WildcardExpression exp2 = "abc\\.d\\\\ef";
            WildcardExpression exp3 = "xyz123";
            object exp4 = new object();
            var exp5 = WildcardExpression.Parse("xyz123", false).Resolve();

            Assert.AreEqual(exp, exp);
            Assert.IsTrue(exp.Equals(exp));
            Assert.IsTrue(exp.Equals((object)exp));

            Assert.AreEqual(exp, exp2);
            Assert.IsTrue(exp.Equals(exp2));
            Assert.IsTrue(exp.Equals((object)exp2));
            Assert.IsTrue(exp == exp2);

            Assert.AreNotEqual(exp, exp3);
            Assert.IsFalse(exp.Equals(exp4));
            Assert.IsFalse(exp.Equals((object)exp3));
            Assert.IsTrue(exp != exp3);

            Assert.IsFalse(exp.Equals(exp4));
            Assert.IsFalse(exp.Equals((object?)null));

            Assert.AreNotEqual(exp, exp5);
            Assert.IsFalse(exp.Equals(exp5));
        }

        [TestMethod]
        public void GetHashCode_Tests()
        {
            WildcardExpression exp = "xyz123";
            WildcardExpression exp2 = "xyz123";

            Assert.AreEqual(exp.GetHashCode(), exp2.GetHashCode());
        }

        [TestMethod]
        public void IsMatch_Tests()
        {
            Assert.IsTrue(WildcardExpression.Default.IsMatch(default(string)!));
            Assert.IsFalse(WildcardExpression.Default.IsMatch(""));
            Assert.IsTrue(WildcardExpression.Parse("").Resolve().IsMatch(""));
            Assert.IsFalse(WildcardExpression.Parse(" ").Resolve().IsMatch(""));
            Assert.IsFalse(WildcardExpression.Default.IsMatch(" "));

            WildcardExpression exp = "abcd";
            Assert.IsFalse(exp.IsMatch("abc"));

            exp = "abc..f";
            Assert.IsTrue(exp.IsMatch("abcdef"));
            Assert.IsFalse(exp.IsMatch("a2cdef"));

            exp = WildcardExpression.Parse("abc..f", false).Resolve();
            Assert.IsTrue(exp.IsMatch("abcdef"));
            Assert.IsTrue(exp.IsMatch("abCdef"));
            Assert.IsFalse(exp.IsMatch("ab1def"));

        }
    }
}
