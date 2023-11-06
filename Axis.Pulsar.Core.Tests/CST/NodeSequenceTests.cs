using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Tests.CST
{
    [TestClass]
    public class NodeSequenceTests
    {
        [TestMethod]
        public void Prepend_Tests()
        {
            var str = "prevnext";
            var prevNode = ICSTNode.Of("t", Tokens.Of(str, 0, 4));
            var nextNode = ICSTNode.Of("t", Tokens.Of(str, 4, 4));
            var ns = NodeSequence.Of(prevNode);
            var ns2 = NodeSequence.Of(nextNode);

            var result = ns.Prepend(NodeSequence.Empty);
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);


            result = ns2.Prepend(ns);
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            result.ForAll((i, n) =>
            {
                if (i == 0)
                    Assert.AreEqual("prev", n.Tokens.ToString());

                if (i == 1)
                    Assert.AreEqual("next", n.Tokens.ToString());
            });


            result = ns.Prepend(ns2);
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            result.ForAll((i, n) =>
            {
                if (i == 1)
                    Assert.AreEqual("prev", n.Tokens.ToString());

                if (i == 0)
                    Assert.AreEqual("next", n.Tokens.ToString());
            });
        }

        [TestMethod]
        public void Append_Tests()
        {
            var str = "prevnext";
            var prevNode = ICSTNode.Of("t", Tokens.Of(str, 0, 4));
            var nextNode = ICSTNode.Of("t", Tokens.Of(str, 4, 4));
            var ns = NodeSequence.Of(prevNode);
            var ns2 = NodeSequence.Of(nextNode);

            var result = ns.Append(NodeSequence.Empty);
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);


            result = ns2.Append(ns);
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            result.ForAll((i, n) =>
            {
                if (i == 1)
                    Assert.AreEqual("prev", n.Tokens.ToString());

                if (i == 0)
                    Assert.AreEqual("next", n.Tokens.ToString());
            });


            result = ns.Append(ns2);
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            result.ForAll((i, n) =>
            {
                if (i == 0)
                    Assert.AreEqual("prev", n.Tokens.ToString());

                if (i == 1)
                    Assert.AreEqual("next", n.Tokens.ToString());
            });
        }
    }
}
