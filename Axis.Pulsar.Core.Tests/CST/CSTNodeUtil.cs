using Axis.Pulsar.Core.CST;

namespace Axis.Pulsar.Core.Tests.CST
{
    [TestClass]
    public class CSTNodeUtilTests
    {
        [TestMethod]
        public void FindNodesTests()
        {
            var cst = ISymbolNode.Of(
                "first",
                ISymbolNode.Of("second"),
                ISymbolNode.Of(
                    "second",
                    ISymbolNode.Of(
                        "third",
                        ISymbolNode.Of(
                            "fourth",
                            "the-tokens"))));

            var nodes = Core.CST.SymbolNodeUtil
                .FindNodes(cst, "first/second")
                .ToArray();
            Assert.AreEqual(0, nodes.Length);

            nodes = Core.CST.SymbolNodeUtil
                .FindNodes(cst, "second")
                .ToArray();
            Assert.AreEqual(2, nodes.Length);

            nodes = SymbolNodeUtil
                .FindNodes(cst, "second/third/fourth")
                .ToArray();
            Assert.AreEqual(1, nodes.Length);


            cst = ISymbolNode.Of(
                "first",
                ISymbolNode.Of("second"),
                ISymbolNode.Of(
                    "bleh",
                    ISymbolNode.Of("second"),
                    ISymbolNode.Of("second"),
                    ISymbolNode.Of("second"),
                    ISymbolNode.Of("second"),
                    ISymbolNode.Of("second")));
            nodes = SymbolNodeUtil
                .FindNodes(cst, "bleh/@c:second")
                .ToArray();
            Assert.AreEqual(5, nodes.Length);

            cst = ISymbolNode.Of("name", "tokens");
            nodes = SymbolNodeUtil.FindNodes(cst, "bleh").ToArray();
            Assert.AreEqual(0, nodes.Length);
        }

        [TestMethod]
        public void FindAllNodes_Tests()
        {
            var cst = ISymbolNode.Of(
                "first",
                ISymbolNode.Of("second"),
                ISymbolNode.Of("second", "bleh"),
                ISymbolNode.Of("second-ish"),
                ISymbolNode.Of("second-ish", "bleh"),
                ISymbolNode.Of(
                    "bleh",
                    ISymbolNode.Of("second"),
                    ISymbolNode.Of("second"),
                    ISymbolNode.Of("second"),
                    ISymbolNode.Of("second"),
                    ISymbolNode.Of("second")));

            var nodes = SymbolNodeUtil
                .FindAllNodes(cst, "second")
                .ToArray();

            Assert.AreEqual(7, nodes.Length);

            cst = ISymbolNode.Of("name", "tokens");
            nodes = SymbolNodeUtil.FindAllNodes(cst, "bleh").ToArray();
            Assert.AreEqual(0, nodes.Length);
        }
    }
}
