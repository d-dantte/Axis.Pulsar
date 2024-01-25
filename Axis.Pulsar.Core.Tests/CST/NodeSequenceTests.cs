using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Tests.CST
{
    [TestClass]
    public class NodeSequenceTests
    {

        [TestMethod]
        public void Append_Tests()
        {
            var str = "prevnext";
            var prevNode = ICSTNode.Of("t", Tokens.Of(str, 0, 4));
            var nextNode = ICSTNode.Of("t", Tokens.Of(str, 4, 4));
            var ns = INodeSequence.Of(prevNode);
            var ns2 = INodeSequence.Of(nextNode);

            var result = ns.ConcatSequence(INodeSequence.Empty);
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);


            result = ns2.ConcatSequence(ns);
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            result.ForAll((i, n) =>
            {
                if (i == 1)
                    Assert.AreEqual("prev", n.Tokens.ToString());

                if (i == 0)
                    Assert.AreEqual("next", n.Tokens.ToString());
            });


            result = ns.ConcatSequence(ns2);
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

        [TestMethod]
        public void RequiredNodeCount_Tests()
        {
            var str = "the string";
            var seq = INodeSequence.Of(ICSTNode.Of("x", Tokens.Of(str, 0, 1)));
            Assert.IsFalse(seq.IsOptional);
            Assert.AreEqual(1, seq.RequiredNodeCount);

            var seq2 = INodeSequence.Of(seq, false);
            Assert.IsFalse(seq2.IsOptional);
            Assert.AreEqual(1, seq2.RequiredNodeCount);

            seq2 = INodeSequence.Of(seq, true);
            Assert.IsTrue(seq2.IsOptional);
            Assert.AreEqual(0, seq2.RequiredNodeCount);
        }
    }
}
