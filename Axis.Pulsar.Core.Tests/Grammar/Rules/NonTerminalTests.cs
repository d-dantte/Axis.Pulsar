using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Composite;
using Axis.Pulsar.Core.Grammar.Composite.Group;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Utils;
using Moq;

namespace Axis.Pulsar.Core.Tests.Grammar.Rules
{
    [TestClass]
    public class NonTerminalTests
    {
        internal static IGroupRule MockElement(
            Cardinality cardinality,
            bool recognitionStatus,
            GroupRecognitionResult recognitionResult)
        {
            var mock = new Mock<IGroupRule>();

            mock.Setup(m => m.Cardinality).Returns(cardinality);
            mock.Setup(m => m.TryRecognize(
                    It.IsAny<TokenReader>(),
                    It.IsAny<SymbolPath>(),
                    It.IsAny<ILanguageContext>(),
                    out It.Ref<GroupRecognitionResult>.IsAny))
                .Returns(
                    new TryRecognizeNodeSequence((
                        TokenReader reader,
                        SymbolPath path,
                        ILanguageContext cxt,
                        out GroupRecognitionResult result) =>
                {
                    result = recognitionResult;
                    return recognitionStatus;
                }));

            return mock.Object;
        }

        [TestMethod]
        public void TryRecognize_Tests()
        {
            var path = SymbolPath.Of("parent");

            // passing element
            var element = MockElement(
                Cardinality.OccursOnlyOnce(),
                true,
                GroupRecognitionResult.Of(INodeSequence.Empty));
            var nt = NonTerminal.Of(element);
            var success = nt.TryRecognize("stuff", path, null!, out var result);
            Assert.IsTrue(success);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Is(out ICSTNode nseq));

            // unrecognized element
            element = MockElement(
                Cardinality.OccursOnlyOnce(),
                false,
                FailedRecognitionError
                    .Of(path, 0)
                    .ApplyTo(err => GroupRecognitionError.Of(err))
                    .ApplyTo(GroupRecognitionResult.Of));
            nt = NonTerminal.Of(element);
            success = nt.TryRecognize("stuff", path, null!, out result);
            Assert.IsFalse(success);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Is(out FailedRecognitionError ge));

            // unrecognized element
            element = MockElement(
                Cardinality.OccursOnlyOnce(),
                false,
                FailedRecognitionError
                    .Of(path, 0)
                    .ApplyTo(err => GroupRecognitionError.Of(err, 5))
                    .ApplyTo(GroupRecognitionResult.Of));
            nt = NonTerminal.Of(null, element);
            success = nt.TryRecognize("stuff", path, null!, out result);
            Assert.IsFalse(success);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Is(out ge));

            // partially recognized element
            element = MockElement(
                Cardinality.OccursOnlyOnce(),
                false,
                PartialRecognitionError
                    .Of(path, 0, 11)
                    .ApplyTo(p => GroupRecognitionError.Of(p, 2))
                    .ApplyTo(GroupRecognitionResult.Of));
            nt = NonTerminal.Of(element);
            success = nt.TryRecognize("stuff", path, null!, out result);
            Assert.IsFalse(success);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Is(out PartialRecognitionError pe));

            // recognition threshold element
            element = MockElement(
                Cardinality.OccursOnlyOnce(),
                false,
                FailedRecognitionError
                    .Of(path, 3)
                    .ApplyTo(e => GroupRecognitionError.Of(e, 2))
                    .ApplyTo(GroupRecognitionResult.Of));
            nt = NonTerminal.Of(1, element);
            success = nt.TryRecognize("stuff", path, null!, out result);
            Assert.IsFalse(success);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Is(out pe));
        }
    }
}
