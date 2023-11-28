using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Tests.CST
{
    [TestClass]
    public class NodeFilterTests
    {
        [TestMethod]
        public void Matches_Tests()
        {
            var str = "t1-tokenst2-tokens";
            var tnode1 = ICSTNode.Of("t1", Tokens.Of(str, 0, 9));
            var tnode2 = ICSTNode.Of("t2", Tokens.Of(str, 9, 9));
            var ntnode = ICSTNode.Of("nt", tnode1, tnode2);

            var nodefilter = NodeFilter.Of(
                NodeType.Unspecified,
                null,
                "t1-tokens");
            Assert.IsTrue(nodefilter.Matches(tnode1));
            Assert.IsFalse(nodefilter.Matches(tnode2));
            Assert.IsFalse(nodefilter.Matches(ntnode));


            nodefilter = NodeFilter.Of(
                NodeType.Terminal,
                null,
                null);
            Assert.IsTrue(nodefilter.Matches(tnode1));
            Assert.IsTrue(nodefilter.Matches(tnode2));
            Assert.IsFalse(nodefilter.Matches(ntnode));


            nodefilter = NodeFilter.Of(
                NodeType.Terminal,
                "t1",
                null);
            Assert.IsTrue(nodefilter.Matches(tnode1));
            Assert.IsFalse(nodefilter.Matches(tnode2));
            Assert.IsFalse(nodefilter.Matches(ntnode));


            nodefilter = NodeFilter.Of(
                NodeType.Terminal,
                "t1",
                "otro");
            Assert.IsFalse(nodefilter.Matches(tnode1));
            Assert.IsFalse(nodefilter.Matches(tnode2));
            Assert.IsFalse(nodefilter.Matches(ntnode));


            nodefilter = NodeFilter.Of(
                NodeType.Unspecified,
                "nt",
                null);
            Assert.IsFalse(nodefilter.Matches(tnode1));
            Assert.IsFalse(nodefilter.Matches(tnode2));
            Assert.IsTrue(nodefilter.Matches(ntnode));


            nodefilter = NodeFilter.Of(
                NodeType.NonTerminal,
                null,
                null);
            Assert.IsFalse(nodefilter.Matches(tnode1));
            Assert.IsFalse(nodefilter.Matches(tnode2));
            Assert.IsTrue(nodefilter.Matches(ntnode));


            nodefilter = NodeFilter.Of(
                NodeType.NonTerminal,
                "otro",
                null);
            Assert.IsFalse(nodefilter.Matches(tnode1));
            Assert.IsFalse(nodefilter.Matches(tnode2));
            Assert.IsFalse(nodefilter.Matches(ntnode));


            nodefilter = NodeFilter.Of(
                NodeType.Unspecified,
                null,
                str);
            Assert.IsFalse(nodefilter.Matches(tnode1));
            Assert.IsFalse(nodefilter.Matches(tnode2));
            Assert.IsTrue(nodefilter.Matches(ntnode));
        }
    }

    [TestClass]
    public class SegmentTests
    {
        [TestMethod]
        public void Matches_Tests()
        {
            var str = "t1-tokenst2-tokens";
            var tnode1 = ICSTNode.Of("t1", Tokens.Of(str, 0, 9));
            var tnode2 = ICSTNode.Of("t2", Tokens.Of(str, 9, 9));
            var ntnode = ICSTNode.Of("nt", tnode1, tnode2);

            var nodefilter = PathSegment.Of(
                NodeFilter.Of(
                    NodeType.Unspecified,
                    null,
                    "t1-tokens"));
            Assert.IsTrue(nodefilter.Matches(tnode1));
            Assert.IsFalse(nodefilter.Matches(tnode2));
            Assert.IsFalse(nodefilter.Matches(ntnode));

            nodefilter = PathSegment.Of(
                NodeFilter.Of(
                    NodeType.Unspecified,
                    null,
                    "t1-tokens"),
                NodeFilter.Of(
                    NodeType.Unspecified,
                    null,
                    "t2-tokens"));
            Assert.IsTrue(nodefilter.Matches(tnode1));
            Assert.IsTrue(nodefilter.Matches(tnode2));
            Assert.IsFalse(nodefilter.Matches(ntnode));

            nodefilter = PathSegment.Of(
                NodeFilter.Of(
                    NodeType.Unspecified,
                    null,
                    "t1-tokens"),
                NodeFilter.Of(
                    NodeType.NonTerminal,
                    null,
                    null),
                NodeFilter.Of(
                    NodeType.Unspecified,
                    null,
                    "t2-tokens"));
            Assert.IsTrue(nodefilter.Matches(tnode1));
            Assert.IsTrue(nodefilter.Matches(tnode2));
            Assert.IsTrue(nodefilter.Matches(ntnode));
        }
    }
}
