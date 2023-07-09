using Axis.Pulsar.Grammar.CST;

namespace Axis.Pusar.Grammar.Tests.CST
{
    [TestClass]
    public class CSTNodeUtilsTests
    {
        [TestMethod]
        public void FindNodes_ShouldFindNodesMatchingPath()
        {
            var cst = CSTNode.Of(
                "first",
                CSTNode.Of("second"),
                CSTNode.Of(
                    "second",
                    CSTNode.Of(
                        "third",
                        CSTNode.Of(
                            CSTNode.TerminalType.Literal,
                            "fourth",
                            "the-tokens"))));

            var nodes = CSTNodeUtils.FindNodes(cst, "first.second");
            Assert.AreEqual(0, nodes.Length);

            nodes = CSTNodeUtils.FindNodes(cst, "second");
            Assert.AreEqual(2, nodes.Length);

            nodes = CSTNodeUtils.FindNodes(cst, "second.third.fourth");
            Assert.AreEqual(1, nodes.Length);


            cst = CSTNode.Of(
                "first",
                CSTNode.Of(
                    "bleh",
                    CSTNode.Of("second"),
                    CSTNode.Of("second"),
                    CSTNode.Of("second"),
                    CSTNode.Of("second"),
                    CSTNode.Of("second")));
            nodes = CSTNodeUtils.FindNodes(cst, "bleh.second");
            Assert.AreEqual(5, nodes.Length);

            cst = cst.FirstNode();
            nodes = CSTNodeUtils.FindNodes(cst, "second");
            Assert.AreEqual(5, nodes.Length);
        }

        [TestMethod]
        public void FindAllNodes_ShouldFindNodesMatchingName()
        {
            var cst = CSTNode.Of(
                "first",
                CSTNode.Of(CSTNode.TerminalType.Literal, "second", "tokens"),
                CSTNode.Of(
                    "second",
                    CSTNode.Of(
                        "third",
                        CSTNode.Of(
                            "fourth"))),
                CSTNode.Of("something"),
                CSTNode.Of("second"),
                CSTNode.Of("third"),
                CSTNode.Of("bleh"),
                CSTNode.Of("second"));

            var nodes = CSTNodeUtils.FindAllNodes(cst, "second");
            Assert.AreEqual(4, nodes.Length);
        }
    }
}
