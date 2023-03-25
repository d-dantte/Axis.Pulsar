using Axis.Pulsar.Grammar.CST;

namespace Axis.Pusar.Grammar.Tests.CST
{
    [TestClass]
    public class CSTExtensionsTests
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
                            "fourth"))));

            var nodes = cst.FindNodes("first.second").ToArray();
            Assert.AreEqual(0, nodes.Length);

            nodes = cst.FindNodes("second").ToArray();
            Assert.AreEqual(2, nodes.Length);

            nodes = cst.FindNodes("second.third.fourth").ToArray();
            Assert.AreEqual(1, nodes.Length);
        }

        [TestMethod]
        public void FindAllNodes_ShouldFindNodesMatchingName()
        {
            var cst = CSTNode.Of(
                "first",
                CSTNode.Of("second", "tokens"),
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

            var nodes = cst.FindAllNodes("second").ToArray();
            Assert.AreEqual(4, nodes.Length);
        }
    }
}
