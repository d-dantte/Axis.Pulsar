using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Atomic;
using Axis.Pulsar.Core.Grammar.Composite.Group;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Utils;
using Moq;

namespace Axis.Pulsar.Core.Tests.Grammar.Composite.Groups
{
    [TestClass]
    public class AtomicRuleRefTests
    {
        internal static IAtomicRule MockAtom(
            string id,
            NodeRecognitionResult recognitionResult)
        {
            var mock = new Mock<IAtomicRule>();
            mock.Setup(m => m.Id).Returns(id);
            mock.Setup(m => m.TryRecognize(
                    It.IsAny<TokenReader>(),
                    It.IsAny<SymbolPath>(),
                    It.IsAny<ILanguageContext>(),
                    out It.Ref<NodeRecognitionResult>.IsAny))
                .Returns(
                    new TryRecognizeNode((
                        TokenReader reader,
                        SymbolPath path,
                        ILanguageContext cxt,
                        out NodeRecognitionResult result) =>
                    {
                        result = recognitionResult;
                        return result.Is(out ISymbolNode _);
                    }));

            return mock.Object;
        }

        [TestMethod]
        public void Constructor_Tests()
        {
            var atom = MockAtom("id", NodeRecognitionResult.Of(ISymbolNode.Of("id", Tokens.Of("bleh"))));
            var cardinality = Cardinality.OccursOnly(1);
            var arr = new AtomicRuleRef(cardinality, atom);
            Assert.IsNotNull(arr);
            Assert.AreEqual(atom, arr.Ref);
            Assert.AreEqual(cardinality, arr.Cardinality);

            arr = AtomicRuleRef.Of(cardinality, atom);
            Assert.IsNotNull(arr);

            Assert.ThrowsException<ArgumentException>(
                () => new AtomicRuleRef(Cardinality.Default, atom));

            Assert.ThrowsException<ArgumentNullException>(
                () => new AtomicRuleRef(cardinality, null!));
        }

        [TestMethod]
        public void TryRecognizeWithPassingRef_Tests()
        {
            var atom = MockAtom("id", NodeRecognitionResult.Of(ISymbolNode.Of("id", Tokens.Of("bleh"))));
            var cardinality = Cardinality.OccursOnly(1);
            var arr = new AtomicRuleRef(cardinality, atom);
            var recognized = arr.TryRecognize("bleh", "sym", null!, out var result);

            Assert.IsTrue(recognized);
            Assert.IsTrue(result.Is(out INodeSequence _));
        }

        [TestMethod]
        public void TryRecognizeWithFailedRef_Tests()
        {
            var atom = MockAtom("id", NodeRecognitionResult.Of(FailedRecognitionError.Of("sym", 0)));
            var cardinality = Cardinality.OccursOnly(1);
            var arr = new AtomicRuleRef(cardinality, atom);
            var recognized = arr.TryRecognize("bleh", "sym", null!, out var result);

            Assert.IsFalse(recognized);
            Assert.IsTrue(result.Is(out GroupRecognitionError gre));
            Assert.IsTrue(gre.Cause is FailedRecognitionError);
        }

        [TestMethod]
        public void TryRecognizeWithPartialRef_Tests()
        {
            var atom = MockAtom("id", NodeRecognitionResult.Of(PartialRecognitionError.Of("sym", 0, 1)));
            var cardinality = Cardinality.OccursOnly(1);
            var arr = new AtomicRuleRef(cardinality, atom);
            var recognized = arr.TryRecognize("bleh", "sym", null!, out var result);

            Assert.IsFalse(recognized);
            Assert.IsTrue(result.Is(out GroupRecognitionError gre));
            Assert.IsTrue(gre.Cause is PartialRecognitionError);
        }

    }
}
