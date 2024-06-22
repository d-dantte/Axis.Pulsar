using Axis.Pulsar.Core.Grammar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Axis.Pulsar.Core.Tests.Grammar
{
    [TestClass]
    public class SymbolPathTests
    {
        [TestMethod]
        public void Constructor_Tests()
        {
            var path = SymbolPath.Of("symbol", null);
            Assert.IsNotNull(path);
            Assert.AreEqual("symbol", path.Symbol);
            Assert.IsNull(path.Parent);
            Assert.IsFalse(path.IsDefault);

            Assert.ThrowsException<ArgumentNullException>(
                () => SymbolPath.Of(null!, null));

            path = SymbolPath.Of("symbol", default);
            Assert.IsNotNull(path);
            Assert.AreEqual("symbol", path.Symbol);
            Assert.IsNull(path.Parent);

            path = SymbolPath.Of("symbol", SymbolPath.Of("parent"));
            Assert.IsNotNull(path);
            Assert.AreEqual("symbol", path.Symbol);
            Assert.AreEqual("parent", path.Parent!.Value.Symbol);

            Assert.IsTrue(SymbolPath.Default.IsDefault);
        }

        [TestMethod]
        public void Parse_Tests()
        {
            var path = SymbolPath.Parse("me/you");
            Assert.AreEqual("you", path.Symbol);
            Assert.AreEqual("me", path.Parent!.Value.Symbol);

            Assert.ThrowsException<FormatException>(
                () => SymbolPath.Parse(null!));
        }

        [TestMethod]
        public void Implicit_Tests()
        {
            SymbolPath path = "symbol";
            Assert.AreEqual("symbol", path.Symbol);

            string @string = path;
            Assert.AreEqual("symbol", @string);
        }

        [TestMethod]
        public void ToString_Tests()
        {
            SymbolPath path = "symbol";
            Assert.AreEqual("symbol", path.ToString());

            path = "me/you";
            Assert.AreEqual("me/you", path.ToString());
        }

        [TestMethod]
        public void HashCode_Tests()
        {
            var path = SymbolPath.Of("symbol");
            Assert.AreEqual(
                HashCode.Combine("symbol", (object?)null),
                path.GetHashCode());
        }

        [TestMethod]
        public void Equals_Tests()
        {
            Assert.IsFalse(SymbolPath.Of("me").Equals(new object()));
            Assert.IsTrue(SymbolPath.Of("me").Equals((object)SymbolPath.Of("me")));
            Assert.IsFalse(SymbolPath.Of("me").Equals((object)SymbolPath.Of("you")));

            var path = SymbolPath.Of("me", SymbolPath.Of("you"));
            Assert.IsTrue(path.Equals(path));
            Assert.IsFalse(path.Equals(SymbolPath.Of("me")));
        }
    }
}
