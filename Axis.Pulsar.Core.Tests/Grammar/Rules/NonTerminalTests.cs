using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Grammar.Groups;
using Axis.Pulsar.Core.Grammar.Rules;
using Axis.Pulsar.Core.Utils;
using Moq;

namespace Axis.Pulsar.Core.Tests.Grammar.Rules
{
    [TestClass]
    public class NonTerminalTests
    {
        internal static IGroupElement MockElement(
            Cardinality cardinality,
            bool recognitionStatus,
            IResult<NodeSequence> recognitionResult)
        {
            var mock = new Mock<IGroupElement>();

            mock.Setup(m => m.Cardinality).Returns(cardinality);
            mock.Setup(m => m.TryRecognize(
                    It.IsAny<TokenReader>(),
                    It.IsAny<ProductionPath>(),
                    It.IsAny<ILanguageContext>(),
                    out It.Ref<IResult<NodeSequence>>.IsAny))
                .Returns(
                    new TryRecognizeNodeSequence((
                        TokenReader reader,
                        ProductionPath? path,
                        ILanguageContext cxt,
                        out IResult<NodeSequence> result) =>
                {
                    result = recognitionResult;
                    return recognitionStatus;
                }));

            return mock.Object;
        }

        [TestMethod]
        public void TryRecognize_Tests()
        {
            var path = ProductionPath.Of("parent");

            // passing element
            var element = MockElement(
                Cardinality.OccursOnlyOnce(),
                true,
                Result.Of(NodeSequence.Empty));
            var nt = NonTerminal.Of(element);
            var success = nt.TryRecognize("stuff", path, null!, out var result);
            Assert.IsTrue(success);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDataResult());

            // unrecognized element
            element = MockElement(
                Cardinality.OccursOnlyOnce(),
                false,
                FailedRecognitionError
                    .Of(path, 0)
                    .ApplyTo(GroupRecognitionError.Of)
                    .ApplyTo(Result.Of<NodeSequence>));
            nt = NonTerminal.Of(element);
            success = nt.TryRecognize("stuff", path, null!, out result);
            Assert.IsFalse(success);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsErrorResult(out FailedRecognitionError ge));

            // partially recognized element
            element = MockElement(
                Cardinality.OccursOnlyOnce(),
                false,
                PartialRecognitionError
                    .Of(path, 0, 11)
                    .ApplyTo(p => GroupRecognitionError.Of(p, 2))
                    .ApplyTo(Result.Of<NodeSequence>));
            nt = NonTerminal.Of(element);
            success = nt.TryRecognize("stuff", path, null!, out result);
            Assert.IsFalse(success);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsErrorResult(out PartialRecognitionError pe));

            // recognition threshold element
            element = MockElement(
                Cardinality.OccursOnlyOnce(),
                false,
                FailedRecognitionError
                    .Of(path, 3)
                    .ApplyTo(e => GroupRecognitionError.Of(e, 2))
                    .ApplyTo(Result.Of<NodeSequence>));
            nt = NonTerminal.Of(element);
            success = nt.TryRecognize("stuff", path, null!, out result);
            Assert.IsFalse(success);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsErrorResult(out pe));

            // runtime recognized element
            element = MockElement(
                Cardinality.OccursOnlyOnce(),
                false,
                Result.Of<NodeSequence>(new Exception()));
            nt = NonTerminal.Of(element);
            success = nt.TryRecognize("stuff", path, null!, out result);
            Assert.IsFalse(success);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsErrorResult(out Exception _));

        }
    }
}
