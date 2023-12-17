using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Nodes;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Grammar.Validation;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Utils;
using Moq;
using System.Collections.Immutable;

namespace Axis.Pulsar.Core.Tests.Grammar.Rules
{
    [TestClass]
    public class ProductionTests
    {
        private IProductionValidator MockValidator(bool success)
        {
            var mockValidator = new Mock<IProductionValidator>();
            var setup = mockValidator.Setup(m => m.Validate(
                It.IsAny<SymbolPath>(),
                It.IsAny<ILanguageContext>(),
                It.IsAny<ICSTNode>()));

            if (!success)
                setup.Throws<FormatException>();

            return mockValidator.Object;
        }

        private ILanguageContext MockLanguageContext(
            IDictionary<string, IProductionValidator> validatorMap)
        {
            var mockContext = new Mock<ILanguageContext>();
            mockContext.Setup(m => m.ProductionValidators).Returns(validatorMap.ToImmutableDictionary());
            return mockContext.Object;
        }

        private INodeRule MockRule(TryRecognizeNode recognizer)
        {
            var mockRule = new Mock<INodeRule>();
            mockRule
                .Setup(r => r.TryRecognize(
                    It.IsAny<TokenReader>(),
                    It.IsAny<SymbolPath>(),
                    It.IsAny<ILanguageContext>(),
                    out It.Ref<NodeRecognitionResult>.IsAny))
                .Returns(recognizer);
            return mockRule.Object;
        }


        [TestMethod]
        public void TryRecognize_Tests()
        {
            var passingRuleMock = MockRule((TokenReader reader, SymbolPath path, ILanguageContext cxt, out NodeRecognitionResult x) =>
            {
                x = ICSTNode
                    .Of("symbol", "tokens")
                    .ApplyTo(NodeRecognitionResult.Of);
                return true;
            });

            var failingRuleMock = MockRule((TokenReader reader, SymbolPath path, ILanguageContext cxt, out NodeRecognitionResult x) =>
            {
                x = FailedRecognitionError
                    .Of(path!, 2)
                    .ApplyTo(NodeRecognitionResult.Of);
                return false;
            });

            var noValidatorContext = MockLanguageContext(new Dictionary<string, IProductionValidator>());
            var passingValidatorContext = MockLanguageContext(new Dictionary<string, IProductionValidator>
            {
                ["symbol"] = MockValidator(true)
            });
            var failingValidatorContext = MockLanguageContext(new Dictionary<string, IProductionValidator>
            {
                ["symbol"] = MockValidator(false)
            });

            // no validator
            var production = Production.Of("symbol", passingRuleMock);
            var success = production.TryRecognize("some tokens", null!, noValidatorContext, out var result);
            Assert.IsTrue(success);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Is(out ICSTNode _));


            production = Production.Of("symbol", failingRuleMock);
            success = production.TryRecognize("some tokens", null!, noValidatorContext, out result);
            Assert.IsFalse(success);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Is(out FailedRecognitionError _));

            // passing validator
            production = Production.Of("symbol", passingRuleMock);
            success = production.TryRecognize("some tokens", null!, passingValidatorContext, out result);
            Assert.IsTrue(success);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Is(out ICSTNode _));


            production = Production.Of("symbol", failingRuleMock);
            success = production.TryRecognize("some tokens", null!, passingValidatorContext, out result);
            Assert.IsFalse(success);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Is(out FailedRecognitionError _));

            // failing validator
            production = Production.Of("symbol", passingRuleMock);
            Assert.ThrowsException<FormatException>(() => production.TryRecognize(
                "some tokens",
                null!,
                failingValidatorContext,
                out result));


            production = Production.Of("symbol", failingRuleMock);
            success = production.TryRecognize("some tokens", null!, failingValidatorContext, out result);
            Assert.IsFalse(success);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Is(out FailedRecognitionError _));
        }

        #region nested types
        //internal class PassingValidator : IProductionValidator
        //{
        //    public void Validate(
        //        SymbolPath productionPath,
        //        ILanguageContext context,
        //        ICSTNode recogniedNode)
        //    {
        //    }
        //}

        //internal class FailingValidator : IProductionValidator
        //{
        //    public void Validate(
        //        SymbolPath productionPath,
        //        ILanguageContext context,
        //        ICSTNode recogniedNode)
        //    {
        //        throw new NotImplementedException();
        //    }
        //}

        #endregion
    }
}
