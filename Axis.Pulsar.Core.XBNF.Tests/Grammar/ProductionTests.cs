using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Validation;
using Axis.Pulsar.Core.Utils;
using Moq;
using System.Collections.Immutable;

namespace Axis.Pulsar.Core.XBNF.Tests.Grammar
{
    [TestClass]
    public class ProductionTests
    {
        private IProductionValidator MockValidator(bool success)
        {
            var mockValidator = new Mock<IProductionValidator>();
            var setup = mockValidator.Setup(m => m.Validate(
                It.IsAny<ProductionPath>(),
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

        private IRule MockRule(TryRecognizeNode recognizer)
        {
            var mockRule = new Mock<IRule>();
            mockRule
                .Setup(r => r.TryRecognize(
                    It.IsAny<TokenReader>(),
                    It.IsAny<ProductionPath>(),
                    It.IsAny<ILanguageContext>(),
                    out It.Ref<IResult<ICSTNode>>.IsAny))
                .Returns(recognizer);
            return mockRule.Object;
        }


        [TestMethod]
        public void TryProcessRule_Tests()
        {
            var passingRuleMock = MockRule((TokenReader reader, ProductionPath? path, ILanguageContext cxt, out IResult<ICSTNode> x) =>
            {
                x = ICSTNode
                    .Of("symbol", "tokens")
                    .ApplyTo(Result.Of<ICSTNode>);
                return true;
            });

            var failingRuleMock = MockRule((TokenReader reader, ProductionPath? path, ILanguageContext cxt, out IResult<ICSTNode> x) =>
            {
                x = FailedRecognitionError
                    .Of(path!, 2)
                    .ApplyTo(Result.Of<ICSTNode>);
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
            var production = XBNFProduction.Of("symbol", passingRuleMock);
            var success = production.TryProcessRule("some tokens", null, noValidatorContext, out var result);
            Assert.IsTrue(success);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDataResult());


            production = XBNFProduction.Of("symbol", failingRuleMock);
            success = production.TryProcessRule("some tokens", null, noValidatorContext, out result);
            Assert.IsFalse(success);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsErrorResult());

            // passing validator
            production = XBNFProduction.Of("symbol", passingRuleMock);
            success = production.TryProcessRule("some tokens", null, passingValidatorContext, out result);
            Assert.IsTrue(success);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDataResult());


            production = XBNFProduction.Of("symbol", failingRuleMock);
            success = production.TryProcessRule("some tokens", null, passingValidatorContext, out result);
            Assert.IsFalse(success);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsErrorResult());

            // failing validator
            production = XBNFProduction.Of("symbol", passingRuleMock);
            success = production.TryProcessRule("some tokens", null, failingValidatorContext, out result);
            Assert.IsFalse(success);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsErrorResult(out FormatException _));


            production = XBNFProduction.Of("symbol", failingRuleMock);
            success = production.TryProcessRule("some tokens", null, failingValidatorContext, out result);
            Assert.IsFalse(success);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsErrorResult(out FailedRecognitionError _));
        }

        #region nested types
        //internal class PassingValidator : IProductionValidator
        //{
        //    public void Validate(
        //        ProductionPath productionPath,
        //        ILanguageContext context,
        //        ICSTNode recogniedNode)
        //    {
        //    }
        //}

        //internal class FailingValidator : IProductionValidator
        //{
        //    public void Validate(
        //        ProductionPath productionPath,
        //        ILanguageContext context,
        //        ICSTNode recogniedNode)
        //    {
        //        throw new NotImplementedException();
        //    }
        //}

        #endregion
    }
}
