using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Grammar.Rules;
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
        private static IProductionValidator MockValidator(Core.Grammar.Validation.Status? success)
        {
            var mockValidator = new Mock<IProductionValidator>();
            var setup = mockValidator
                .Setup(m => m.Validate(
                    It.IsAny<SymbolPath>(),
                    It.IsAny<ILanguageContext>(),
                    It.IsAny<ISymbolNode>()))
                .Returns(success switch
                {
                    Status.Valid => Status.Valid,
                    Status.Invalid => Status.Invalid,
                    Status.Fatal or _ => Status.Fatal
                });

            return mockValidator.Object;
        }

        private static ILanguageContext MockLanguageContext(
            IDictionary<string, IProductionValidator> validatorMap)
        {
            var mockContext = new Mock<ILanguageContext>();
            mockContext.Setup(m => m.ProductionValidators).Returns(validatorMap.ToImmutableDictionary());
            return mockContext.Object;
        }

        private static Production.IRule MockRule(NodeRecognition recognizer)
        {
            var mockRule = new Mock<Production.IRule>();
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
        public void Construction_Tests()
        {
            var mockRule = MockRule((TokenReader reader, SymbolPath path, ILanguageContext cxt, out NodeRecognitionResult x) =>
            {
                x = ISymbolNode
                    .Of("symbol", "tokens")
                    .ApplyTo(NodeRecognitionResult.Of);
                return true;
            });

            var prod = new Production("symbol", mockRule);
            Assert.IsNotNull(prod);
            Assert.AreEqual("symbol", prod.Symbol);
            Assert.AreEqual(mockRule, prod.Rule);

            Assert.ThrowsException<ArgumentNullException>(
                () => new Production("sym", null!));

            Assert.ThrowsException<InvalidOperationException>(
                () => new Production("+-sym", mockRule));
        }


        [TestMethod]
        public void TryRecognize_WithNoValidator()
        {
            var passingRuleMock = MockRule((TokenReader reader, SymbolPath path, ILanguageContext cxt, out NodeRecognitionResult x) =>
            {
                x = ISymbolNode
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

            // passing rule
            var production = Production.Of("symbol", passingRuleMock);
            var success = production.TryRecognize("some tokens", "path", noValidatorContext, out var result);
            Assert.IsTrue(success);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Is(out ISymbolNode _));

            // failing rule
            production = Production.Of("symbol", failingRuleMock);
            success = production.TryRecognize("some tokens", "path", noValidatorContext, out result);
            Assert.IsFalse(success);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Is(out FailedRecognitionError _));
        }

        [TestMethod]
        public void TryRecognize_WithValidValidator()
        {
            var passingRuleMock = MockRule((TokenReader reader, SymbolPath path, ILanguageContext cxt, out NodeRecognitionResult x) =>
            {
                x = ISymbolNode
                    .Of("symbol", "tokens")
                    .ApplyTo(NodeRecognitionResult.Of);
                return true;
            });

            var passingValidatorContext = MockLanguageContext(new Dictionary<string, IProductionValidator>
            {
                ["symbol"] = MockValidator(Status.Valid)
            });

            // passing rule
            var production = Production.Of("symbol", passingRuleMock);
            var success = production.TryRecognize("some tokens", "path", passingValidatorContext, out var result);
            Assert.IsTrue(success);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Is(out ISymbolNode _));
        }

        [TestMethod]
        public void TryRecognize_WithInvalidValidator()
        {
            var passingRuleMock = MockRule((TokenReader reader, SymbolPath path, ILanguageContext cxt, out NodeRecognitionResult x) =>
            {
                x = ISymbolNode
                    .Of("symbol", "tokens")
                    .ApplyTo(NodeRecognitionResult.Of);
                return true;
            });

            var invalidValidatorContext = MockLanguageContext(new Dictionary<string, IProductionValidator>
            {
                ["symbol"] = MockValidator(Status.Invalid)
            });

            // passing rule
            var production = Production.Of("symbol", passingRuleMock);
            var success = production.TryRecognize("some tokens", "path", invalidValidatorContext, out var result);
            Assert.IsFalse(success);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Is(out FailedRecognitionError _));
        }

        [TestMethod]
        public void TryRecognize_WithFatalValidator()
        {
            var passingRuleMock = MockRule((TokenReader reader, SymbolPath path, ILanguageContext cxt, out NodeRecognitionResult x) =>
            {
                x = ISymbolNode
                    .Of("symbol", "tokens")
                    .ApplyTo(NodeRecognitionResult.Of);
                return true;
            });

            var fatalValidatorContext = MockLanguageContext(new Dictionary<string, IProductionValidator>
            {
                ["symbol"] = MockValidator(Status.Fatal)
            });

            // passing rule
            var production = Production.Of("symbol", passingRuleMock);
            var success = production.TryRecognize("some tokens", "path", fatalValidatorContext, out var result);
            Assert.IsFalse(success);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Is(out PartialRecognitionError _));
        }

        [TestMethod]
        public void TryRecognize_WithUnknownValidator()
        {
            var passingRuleMock = MockRule((TokenReader reader, SymbolPath path, ILanguageContext cxt, out NodeRecognitionResult x) =>
            {
                x = ISymbolNode
                    .Of("symbol", "tokens")
                    .ApplyTo(NodeRecognitionResult.Of);
                return true;
            });

            var unknownValidatorContext = MockLanguageContext(new Dictionary<string, IProductionValidator>
            {
                ["symbol"] = MockValidator((Status)(-1))
            });

            // passing rule
            var production = Production.Of("symbol", passingRuleMock);
            var success = production.TryRecognize("some tokens", "path", unknownValidatorContext, out var result);
            Assert.IsFalse(success);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Is(out PartialRecognitionError _));
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
