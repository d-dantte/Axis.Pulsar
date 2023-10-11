using Axis.Luna.Common.Results;
using Axis.Pulsar.Grammar.CST;

namespace Axis.Pusar.Grammar.Tests.CST
{
    [TestClass]
    public class PathParserTests
    {
        [TestMethod]
        public void ParseTest()
        {
            var result = PathParser.Parse("abcd");
            Assert.IsNotNull(result);
            var path = result.Resolve();
            Assert.AreEqual(1, path.Segments.Length);
            Assert.AreEqual(1, path.Segments[0].NodeFilters.Length);

            result = PathParser.Parse(":abcd");
            Assert.IsNotNull(result);
            path = result.Resolve();
            Assert.AreEqual(1, path.Segments.Length);
            Assert.AreEqual(1, path.Segments[0].NodeFilters.Length);
            Assert.AreEqual(NodeType.None, path.Segments[0].NodeFilters[0].NodeType);

            result = PathParser.Parse("@r:ab-cd");
            Assert.IsNotNull(result);
            path = result.Resolve();
            Assert.AreEqual(1, path.Segments.Length);
            Assert.AreEqual(1, path.Segments[0].NodeFilters.Length);
            Assert.AreEqual(NodeType.Ref, path.Segments[0].NodeFilters[0].NodeType);

            result = PathParser.Parse("@R:ab-cd.bleh");
            Assert.IsNotNull(result);
            path = result.Resolve();
            Assert.AreEqual(2, path.Segments.Length);
            Assert.AreEqual(NodeType.None, path.Segments[1].NodeFilters[0].NodeType);

            result = PathParser.Parse("@R:ab-cd/bleh/@l<total>/@c:mello<crew>");
            Assert.IsNotNull(result);
            path = result.Resolve();

            result = PathParser.Parse("@R:ab-cd/bleh/@l<total>/@c:mello<cre\\>w>");
            Assert.IsNotNull(result);
            path = result.Resolve();
        }

        [TestMethod]
        public void Bleh()
        {
            var result = PathParser.Parse("@c:mello<cre\\>w>");
            Assert.IsNotNull(result);
            var path = result.Resolve();
            Assert.IsNotNull(path);
        }
    }
}
