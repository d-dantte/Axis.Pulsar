using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Composite.Group;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Grammar.Validation;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Utils;
using Moq;
using System.Collections.Immutable;

namespace Axis.Pulsar.Core.Tests.Grammar.Groups
{
    [TestClass]
    public class ProductionRefTests
    {
        internal static IGrammar MockGrammar(params KeyValuePair<string, Production>[] productions)
        {
            var mock = new Mock<IGrammar>();

            // setup inverses
            var symbols = productions.Select(p => p.Key).ToArray();
            mock.Setup(grammar => grammar.ContainsProduction(It.IsNotIn(symbols)))
                .Returns(false);
            mock.Setup(grammar => grammar.GetProduction(It.IsNotIn(symbols)))
                .Throws((string key) => new KeyNotFoundException(key));

            return productions
                .Aggregate(mock, (mockInstance, production) =>
                {
                    // setup grammar.Contains
                    mockInstance
                        .Setup(grammar => grammar.ContainsProduction(production.Key))
                        .Returns(true);

                    // setup grammar.GetProduction
                    mockInstance
                        .Setup(grammar => grammar.GetProduction(production.Key))
                        .Returns(production.Value);

                    return mockInstance;
                })
                .Object;
        }

        internal static Production MockProduction(
            string symbol,
            bool executionStatus,
            NodeRecognitionResult executionResult)
        {
            var mock = new Mock<IRule>();

            mock
                .Setup(m => m.TryRecognize(
                    It.IsAny<TokenReader>(),
                    It.IsAny<SymbolPath>(),
                    It.IsAny<ILanguageContext>(),
                    out It.Ref<NodeRecognitionResult>.IsAny))
                .Returns(new TryRecognizeNode((
                        TokenReader reader,
                        SymbolPath path,
                        ILanguageContext languageContext,
                        out NodeRecognitionResult result) =>
                {
                    result = executionResult;
                    return executionStatus;
                }));

            return new Production(symbol, mock.Object);
        }

        internal static ILanguageContext MockContext(
            IGrammar grammar,
            IDictionary<string, IProductionValidator> validatorMap)
        {
            var mock = new Mock<ILanguageContext>();

            mock.Setup(l => l.Grammar).Returns(grammar);
            mock.Setup(m => m.ProductionValidators).Returns(validatorMap.ToImmutableDictionary());

            return mock.Object;
        }
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

        [TestMethod]
        public void TryProcessRule_Tests()
        {
            #region Setup
            // passing production
            var passingProduction = MockProduction(
                "sp", true,
                ICSTNode
                    .Of("sp", "first passing token")
                    .ApplyTo(NodeRecognitionResult.Of));

            // unrecognized production
            var unrecognizedProduction = MockProduction(
                "up", false,
                FailedRecognitionError
                    .Of(SymbolPath.Of("up"), 2)
                    .ApplyTo(NodeRecognitionResult.Of));

            // partially recognized production
            var partialyRecognizedProduction = MockProduction(
                "pp", false,
                PartialRecognitionError
                    .Of(SymbolPath.Of("pp"), 2, 6)
                    .ApplyTo(NodeRecognitionResult.Of));

            // grammar
            var grammar = MockGrammar(
                KeyValuePair.Create("up", unrecognizedProduction),
                KeyValuePair.Create("pp", partialyRecognizedProduction),
                KeyValuePair.Create("sp", passingProduction));

            // validator
            var validators = new Dictionary<string, IProductionValidator>
            {
                ["up"] = MockValidator(true),
                ["pp"] = MockValidator(true),
                ["sp"] = MockValidator(true)
            };
            #endregion

            // lang context
            var context = MockContext(grammar, validators);


            var path = SymbolPath.Of("parent");
            var pref = ProductionRef.Of(Cardinality.OccursOnlyOnce(), "sp");

            var success = pref.TryRecognize("some tokens", path, context, out var result);
            Assert.IsTrue(success);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Is(out INodeSequence _));


            pref = ProductionRef.Of(Cardinality.OccursOnlyOnce(), "up");
            success = pref.TryRecognize("some tokens", path, context, out result);
            Assert.IsFalse(success);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Is(out GroupRecognitionError ge));
            Assert.IsTrue(ge.Cause is FailedRecognitionError);


            pref = ProductionRef.Of(Cardinality.OccursOnlyOnce(), "pp");
            success = pref.TryRecognize("some tokens", path, context, out result);
            Assert.IsFalse(success);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Is(out ge));
            Assert.IsTrue(ge.Cause is PartialRecognitionError);
        }
    }
}
