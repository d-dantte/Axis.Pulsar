using Axis.Pulsar.Core.CST;

namespace Axis.Pulsar.Core.Tests.CST
{
    [TestClass]
    public class CSTNodeUtilTests
    {
        [TestMethod]
        public void FindNodesTests()
        {
            var cst = ICSTNode.Of(
                "first",
                ICSTNode.Of("second"),
                ICSTNode.Of(
                    "second",
                    ICSTNode.Of(
                        "third",
                        ICSTNode.Of(
                            "fourth",
                            "the-tokens"))));

            var nodes = Core.CST.CSTNodeUtil
                .FindNodes(cst, "first/second")
                .ToArray();
            Assert.AreEqual(0, nodes.Length);

            nodes = Core.CST.CSTNodeUtil
                .FindNodes(cst, "second")
                .ToArray();
            Assert.AreEqual(2, nodes.Length);

            nodes = CSTNodeUtil
                .FindNodes(cst, "second/third/fourth")
                .ToArray();
            Assert.AreEqual(1, nodes.Length);


            cst = ICSTNode.Of(
                "first",
                ICSTNode.Of("second"),
                ICSTNode.Of(
                    "bleh",
                    ICSTNode.Of("second"),
                    ICSTNode.Of("second"),
                    ICSTNode.Of("second"),
                    ICSTNode.Of("second"),
                    ICSTNode.Of("second")));
            nodes = CSTNodeUtil
                .FindNodes(cst, "bleh/@n:second")
                .ToArray();
            Assert.AreEqual(5, nodes.Length);
        }

        [TestMethod]
        public void FindAllNodes_Tests()
        {
            var cst = ICSTNode.Of(
                "first",
                ICSTNode.Of("second"),
                ICSTNode.Of(
                    "bleh",
                    ICSTNode.Of("second"),
                    ICSTNode.Of("second"),
                    ICSTNode.Of("second"),
                    ICSTNode.Of("second"),
                    ICSTNode.Of("second")));

            var nodes = CSTNodeUtil
                .FindAllNodes(cst, "second")
                .ToArray();

            Assert.AreEqual(6, nodes.Length);
        }
    }
}
