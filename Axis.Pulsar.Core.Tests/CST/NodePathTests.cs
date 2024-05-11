using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Tests.CST
{
    [TestClass]
    public class NodeFilterTests
    {
        [TestMethod]
        public void Construction_Tests()
        {
            Assert.ThrowsException<ArgumentException>(
                () => new NodeFilter(NodeType.Unspecified, null, null));

            var filter = new NodeFilter(NodeType.Unspecified, "bleh", "bleh");
            Assert.AreEqual(NodeType.Unspecified, filter.NodeType);
            Assert.AreEqual("bleh", filter.SymbolName);
            Assert.AreEqual("bleh", filter.Tokens);
        }

        [TestMethod]
        public void Matches_Tests()
        {
            var str = "t1-tokenst2-tokens";
            var tnode1 = ISymbolNode.Of("t1", Tokens.Of(str, 0, 9));
            var tnode2 = ISymbolNode.Of("t2", Tokens.Of(str, 9, 9));
            var ntnode = ISymbolNode.Of("nt", tnode1, tnode2);
            var fakeNode = new FakeNode();

            var nodefilter = NodeFilter.Of(
                NodeType.Atomic,
                null,
                "t1-tokens");
            Assert.ThrowsException<InvalidOperationException>(() => nodefilter.Matches(fakeNode));
            Assert.ThrowsException<ArgumentNullException>(() => nodefilter.Matches(null!));

            nodefilter = NodeFilter.Of(
                NodeType.Atomic,
                "bleh",
                "t1-tokens");
            Assert.ThrowsException<InvalidOperationException>(() => nodefilter.Matches(fakeNode));

            nodefilter = NodeFilter.Of(
                NodeType.Unspecified,
                null,
                "t1-tokens");
            Assert.IsTrue(nodefilter.Matches(tnode1));
            Assert.IsFalse(nodefilter.Matches(tnode2));
            Assert.IsFalse(nodefilter.Matches(ntnode));


            nodefilter = NodeFilter.Of(
                NodeType.Atomic,
                null,
                null);
            Assert.IsTrue(nodefilter.Matches(tnode1));
            Assert.IsTrue(nodefilter.Matches(tnode2));
            Assert.IsFalse(nodefilter.Matches(ntnode));


            nodefilter = NodeFilter.Of(
                NodeType.Atomic,
                "t1",
                null);
            Assert.IsTrue(nodefilter.Matches(tnode1));
            Assert.IsFalse(nodefilter.Matches(tnode2));
            Assert.IsFalse(nodefilter.Matches(ntnode));


            nodefilter = NodeFilter.Of(
                NodeType.Atomic,
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
                NodeType.Composite,
                null,
                null);
            Assert.IsFalse(nodefilter.Matches(tnode1));
            Assert.IsFalse(nodefilter.Matches(tnode2));
            Assert.IsTrue(nodefilter.Matches(ntnode));


            nodefilter = NodeFilter.Of(
                NodeType.Composite,
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

        [TestMethod]
        public void IsNameMatch_Tests()
        {
            var nodefilter = NodeFilter.Of(
                NodeType.Atomic,
                null,
                "t1-tokens");
            Assert.IsTrue(nodefilter.IsNameMatch(ISymbolNode.Of("abcd", "abcd")));

            nodefilter = NodeFilter.Of(
                NodeType.Atomic,
                "name",
                "t1-tokens");
            Assert.IsTrue(nodefilter.IsNameMatch(ISymbolNode.Of("name", "abcd")));
            Assert.IsTrue(nodefilter.IsNameMatch(ISymbolNode.Of("name")));
            Assert.IsFalse(nodefilter.IsNameMatch(new FakeNode()));
        }
    }

    [TestClass]
    public class PathSegmentTests
    {
        [TestMethod]
        public void Construction_Tests()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new PathSegment(null!));
            Assert.ThrowsException<ArgumentException>(() => new PathSegment(new NodeFilter[] { null! }));

            var segment = new PathSegment(new NodeFilter(NodeType.Atomic, null, null));
            Assert.IsNotNull(segment);

            segment = PathSegment.Of(new NodeFilter(NodeType.Atomic, null, null));
            Assert.IsNotNull(segment);

            segment = PathSegment.Of(new[] { new NodeFilter(NodeType.Atomic, null, null) }.AsEnumerable());
            Assert.IsNotNull(segment);
        }

        [TestMethod]
        public void Equals_Tests()
        {
            var segment = new PathSegment(new NodeFilter(NodeType.Atomic, null, null));
            var emptySegment = new PathSegment();

            Assert.IsTrue(segment.Equals(segment));
            Assert.IsTrue(segment != emptySegment);
            Assert.IsFalse(segment == emptySegment);
            Assert.IsFalse(segment.Equals(emptySegment));
            Assert.IsFalse(segment.Equals(45));
        }

        [TestMethod]
        public void HashCode_Tests()
        {
            var segment = new PathSegment(new NodeFilter(NodeType.Atomic, null, null));
            var code = segment.GetHashCode();
            Assert.AreEqual(HashCode.Combine(0, segment.NodeFilters[0]), code);
        }

        [TestMethod]
        public void ToString_Tests()
        {
            var segment = new PathSegment(new NodeFilter(NodeType.Atomic, null, null));
            var emptySegment = new PathSegment();

            Assert.AreEqual("@A", segment.ToString());
            Assert.AreEqual("", emptySegment.ToString());
        }

        [TestMethod]
        public void Matches_Tests()
        {
            var str = "t1-tokenst2-tokens";
            var tnode1 = ISymbolNode.Of("t1", Tokens.Of(str, 0, 9));
            var tnode2 = ISymbolNode.Of("t2", Tokens.Of(str, 9, 9));
            var ntnode = ISymbolNode.Of("nt", tnode1, tnode2);

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
                    NodeType.Composite,
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

    [TestClass]
    public class NodePathTests
    {
        [TestMethod]
        public void Construction_Tests()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new NodePath(null!));
            Assert.ThrowsException<ArgumentException>(() => new NodePath(new PathSegment[] { null! }));

            var segment = new NodePath(new PathSegment());
            Assert.IsNotNull(segment);

            segment = NodePath.Of(new PathSegment());
            Assert.IsNotNull(segment);

            segment = NodePath.Of(new[] { new PathSegment() }.AsEnumerable());
            Assert.IsNotNull(segment);
        }

        [TestMethod]
        public void Equals_Tests()
        {
            var path = new NodePath(new PathSegment());
            var emptyPath = new NodePath();

            Assert.IsTrue(path.Equals(path));
            Assert.IsTrue(path != emptyPath);
            Assert.IsFalse(path == emptyPath);
            Assert.IsFalse(path.Equals(emptyPath));
            Assert.IsFalse(path.Equals(45));
        }

        [TestMethod]
        public void HashCode_Tests()
        {
            var path = new NodePath(new PathSegment());
            var code = path.GetHashCode();
            Assert.AreEqual(HashCode.Combine(0, path.Segments[0]), code);
        }

        [TestMethod]
        public void ToString_Tests()
        {
            var path = new NodePath(new PathSegment(new NodeFilter(NodeType.Composite, null, null)));
            var emptyPath = new NodePath();

            Assert.AreEqual("@C", path.ToString());
            Assert.AreEqual("", emptyPath.ToString());
        }
    }


    public class FakeNode : ISymbolNode
    {
        public Tokens Tokens => default;

        public string Symbol => "";
    }
}
