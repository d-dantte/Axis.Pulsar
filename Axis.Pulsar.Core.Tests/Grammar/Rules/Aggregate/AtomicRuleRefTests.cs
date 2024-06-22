using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Grammar.Rules.Aggregate;
using Axis.Pulsar.Core.Grammar.Rules.Atomic;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Utils;
using Moq;

namespace Axis.Pulsar.Core.Tests.Grammar.Rules.Composite.Aggregation
{
    [TestClass]
    public class AtomicRuleRefTests
    {
        internal static IAtomicRule MockAtom(string id)
        {
            var mock = new Mock<IAtomicRule>();
            mock.Setup(m => m.TryRecognize(
                    It.IsAny<TokenReader>(),
                    It.IsAny<SymbolPath>(),
                    It.IsAny<ILanguageContext>(),
                    out It.Ref<NodeRecognitionResult>.IsAny))
                .Returns(
                    new NodeRecognition((
                        TokenReader reader,
                        SymbolPath path,
                        ILanguageContext cxt,
                        out NodeRecognitionResult result) =>
                    {
                        result = NodeRecognitionResult.Of(ISymbolNode.Of(id, "tokens"));
                        return result.Is(out ISymbolNode _);
                    }));

            return mock.Object;
        }

        [TestMethod]
        public void Constructor_Tests()
        {
            var atom = MockAtom("id");
            var arr = new AtomicRuleRef(atom);
            Assert.IsNotNull(arr);
            Assert.AreEqual(atom, arr.Ref);
            Assert.AreEqual(AggregationType.Unit, arr.Type);

            Assert.ThrowsException<ArgumentNullException>(
                () => AtomicRuleRef.Of(null!));
        }

        [TestMethod]
        public void Recognizer_Tests()
        {
            var atom = MockAtom("atom");
            var atomicRef = AtomicRuleRef.Of(atom);

            var recognizer = atomicRef.Recognizer(null!);
            Assert.IsNotNull(recognizer);
            Assert.AreEqual(atom, recognizer);
        }

    }
}
