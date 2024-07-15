using static Axis.Pulsar.Core.XBNF.IAtomicRuleFactory;

namespace Axis.Pulsar.Core.XBNF.Tests
{
    [TestClass]
    public class AtomicRu_leFactoryTests
    {
        #region Regular Argument
        [TestMethod]
        public void RegularArgument_Constructor()
        {
            var arg = new IAtomicRuleFactory.RegularArgument("stuff");
            Assert.AreEqual("stuff", arg.Key);

            Assert.ThrowsException<ArgumentException>(
                () => new IAtomicRuleFactory.RegularArgument(".0))t654_dalfal"));
        }

        [TestMethod]
        public void RegularArgument_Default()
        {
            var arg = IAtomicRuleFactory.RegularArgument.Default;
            Assert.IsTrue(arg.IsDefault);
            Assert.AreEqual(default(IAtomicRuleFactory.RegularArgument), arg);
        }

        [TestMethod]
        public void RegularArgument_ToString()
        {
            var arg = new IAtomicRuleFactory.RegularArgument("stuff");
            Assert.AreEqual("stuff", arg.ToString());
        }

        [TestMethod]
        public void RegularArgument_GetHashCode()
        {
            var arg = new IAtomicRuleFactory.RegularArgument("stuff");
            Assert.AreEqual(HashCode.Combine("stuff"), arg.GetHashCode());
        }

        [TestMethod]
        public void RegularArgument_Equals()
        {
            var first = IAtomicRuleFactory.RegularArgument.Of("first");
            var second = IAtomicRuleFactory.RegularArgument.Of("second");

            Assert.IsTrue(first.Equals((object)first));
            Assert.IsFalse(first.Equals(new object()));
            Assert.IsFalse(first.Equals((object)second));

            Assert.IsTrue(first.Equals(first));
            Assert.IsFalse(first.Equals(second));

            Assert.IsTrue(first.Equals("first"));
            Assert.IsFalse(first.Equals("second"));

            Assert.IsFalse(first == second);
            Assert.IsTrue(first != second);
        }

        [TestMethod]
        public void RegularArgument_ImplicitOperator()
        {
            IAtomicRuleFactory.RegularArgument arg = "abc";
            var arg2 = IAtomicRuleFactory.RegularArgument.Of("abc");

            Assert.AreEqual(arg, arg2);
        }
        #endregion

        #region Content Argument
        [TestMethod]
        public void ContentArgument_Constructor()
        {
            var arg = new IAtomicRuleFactory.ContentArgument(
                IAtomicRuleFactory.ContentArgumentDelimiter.Grave);

            Assert.AreEqual(IAtomicRuleFactory.ContentArgument.KEY, arg.Key);
            Assert.AreEqual(
                IAtomicRuleFactory.ContentArgumentDelimiter.Grave,
                arg.Delimiter);

            Assert.ThrowsException<ArgumentOutOfRangeException>(
                () => new IAtomicRuleFactory.ContentArgument(
                    (IAtomicRuleFactory.ContentArgumentDelimiter)(-1)));
        }

        [TestMethod]
        public void ContentArgument_Default()
        {
            var arg = IAtomicRuleFactory.ContentArgument.Default;
            Assert.IsTrue(arg.IsDefault);
            Assert.AreEqual(default(IAtomicRuleFactory.ContentArgument), arg);
        }

        [TestMethod]
        public void ContentArgument_ToString()
        {
            var arg = new IAtomicRuleFactory.ContentArgument(
                IAtomicRuleFactory.ContentArgumentDelimiter.Grave);
            Assert.AreEqual(IAtomicRuleFactory.ContentArgument.KEY, arg.ToString());
        }

        [TestMethod]
        public void ContentArgument_GetHashCode()
        {
            var delim = IAtomicRuleFactory.ContentArgumentDelimiter.Grave;
            var arg = new IAtomicRuleFactory.ContentArgument(delim);
            Assert.AreEqual(HashCode.Combine(delim), arg.GetHashCode());
        }

        [TestMethod]
        public void ContentArgument_Equals()
        {
            var grave = IAtomicRuleFactory.ContentArgumentDelimiter.Grave;
            var sol = IAtomicRuleFactory.ContentArgumentDelimiter.Sol;
            var first = IAtomicRuleFactory.ContentArgument.Of(grave);
            var second = IAtomicRuleFactory.ContentArgument.Of(sol);

            Assert.IsTrue(first.Equals((object)first));
            Assert.IsFalse(first.Equals(new object()));
            Assert.IsFalse(first.Equals((object)second));

            Assert.IsTrue(first.Equals(first));
            Assert.IsFalse(first.Equals(second));

            Assert.IsTrue(first.Equals(grave));
            Assert.IsFalse(first.Equals(sol));

            Assert.IsTrue(first.Equals('`'));
            Assert.IsFalse(first.Equals('/'));

            Assert.IsFalse(first == second);
            Assert.IsTrue(first != second);
        }

        [TestMethod]
        public void ContentArgument_ImplicitOperator()
        {
            var grave = IAtomicRuleFactory.ContentArgumentDelimiter.Grave;
            IAtomicRuleFactory.ContentArgument arg = grave;
            var arg2 = IAtomicRuleFactory.ContentArgument.Of(grave);

            Assert.AreEqual(arg, arg2);
        }
        #endregion

        #region Argument Comparer
        [TestMethod]
        public void ArgumentComparer_Equals()
        {
            var regular = IAtomicRuleFactory.RegularArgument.Of("stuff");
            var content = IAtomicRuleFactory.ContentArgument.Of(
                IAtomicRuleFactory.ContentArgumentDelimiter.BackSol);

            var comparer = IAtomicRuleFactory.ArgumentKeyComparer.Default;

            Assert.IsTrue(comparer.Equals(regular, regular));
            Assert.IsTrue(comparer.Equals(content, content));
            Assert.IsFalse(comparer.Equals(content, regular));
            Assert.IsFalse(comparer.Equals(regular, content));
            Assert.IsTrue(comparer.Equals(null, null));
            Assert.IsFalse(comparer.Equals(null, content));
        }

        [TestMethod]
        public void ArgumentComparer_HashCode()
        {
            var regular = RegularArgument.Of("stuff");
            var content = ContentArgument.Of(ContentArgumentDelimiter.BackSol);

            var comparer = ArgumentKeyComparer.Default;

            Assert.AreEqual(regular.ToString().GetHashCode(), comparer.GetHashCode(regular));
            Assert.AreEqual(ContentArgument.KEY.GetHashCode(), comparer.GetHashCode(content));
            Assert.ThrowsException<ArgumentNullException>(
                () => comparer.GetHashCode(null!));
            Assert.ThrowsException<InvalidOperationException>(
                () => comparer.GetHashCode(new FauxArgument()));
        }

        internal class FauxArgument : IAtomicRuleFactory.IArgument
        {
            public string Key => throw new NotImplementedException();
        }

        #endregion

        #region Parameter
        [TestMethod]
        public void Parameter_Constructor()
        {
            var arg = RegularArgument.Of("abc");
            var param = Parameter.Of(arg, "value");
            var optionalParam = Parameter.Of(arg);
            var @default = Parameter.Default;

            Assert.AreEqual(arg, param.Argument);
            Assert.AreEqual("value", param.RawValue);
            Assert.IsTrue(@default.IsDefault);
            Assert.IsFalse(param.IsDefault);
            Assert.IsFalse(optionalParam.IsDefault);
            Assert.ThrowsException<ArgumentNullException>(
                () => Parameter.Of(null!));
        }

        [TestMethod]
        public void Parameter_Equals()
        {
            var arg = RegularArgument.Of("abc");
            var param = Parameter.Of(arg, "value");
            var flagParam = Parameter.Of(arg);
            var @default = Parameter.Default;

            // object
            Assert.IsTrue(param.Equals((object)param));
            Assert.IsFalse(param.Equals(new object()));

            // param
            Assert.IsTrue(param.Equals(param));
            Assert.IsFalse(param.Equals(flagParam));
            Assert.IsFalse(param.Equals(@default));

            // ==
            Assert.IsTrue(param != flagParam);
            Assert.IsFalse(param == flagParam);
        }

        [TestMethod]
        public void Parameter_GetHashCode()
        {
            var arg = RegularArgument.Of("abc");
            var param = Parameter.Of(arg, "value");

            Assert.AreEqual(
                HashCode.Combine(arg, "value"),
                param.GetHashCode());
        }

        [TestMethod]
        public void Parameter_ToString()
        {
            var arg = RegularArgument.Of("abc");
            var param = Parameter.Of(arg, "value");
            var flagParam = Parameter.Of(arg);
            var @default = Parameter.Default;

            Assert.AreEqual("{}", @default.ToString());
            Assert.AreEqual("{flag: abc}", flagParam.ToString());
            Assert.AreEqual("{key: abc, value: value}", param.ToString());
        }
        #endregion
    }
}
