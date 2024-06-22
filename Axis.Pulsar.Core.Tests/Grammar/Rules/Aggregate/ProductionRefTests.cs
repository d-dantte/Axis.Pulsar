using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Grammar.Rules;
using Axis.Pulsar.Core.Grammar.Rules.Aggregate;
using Axis.Pulsar.Core.Grammar.Validation;
using Axis.Pulsar.Core.Lang;
using Moq;
using System.Collections.Immutable;

namespace Axis.Pulsar.Core.Tests.Grammar.Rules.Aggregate
{
    [TestClass]
    public class ProductionRefTests
    {
        [TestMethod]
        public void Constructor_Tests()
        {
            var prodRef = ProductionRef.Of("sym");
            Assert.IsNotNull(prodRef);
            Assert.AreEqual("sym", prodRef.Ref);
            Assert.AreEqual(AggregationType.Unit, prodRef.Type);
        }

        [TestMethod]
        public void Recognizer_Tests()
        {
            var prodRef = ProductionRef.Of("sym");
            var prod = MockProduction();
            var context = MockContext(("sym", prod));

            var recognizer = prodRef.Recognizer(context);
            Assert.IsNotNull(recognizer);
            Assert.AreEqual(prod, recognizer);
        }

        [TestMethod]
        public void Recognizer_WithNullContext_Tests()
        {
            var prodRef = ProductionRef.Of("sym");

            Assert.ThrowsException<ArgumentNullException>(
                () => prodRef.Recognizer(null!));
        }

        internal static ILanguageContext MockContext(params (string symbol, Production production)[] productions)
        {
            var mockGrammar = new Mock<IGrammar>();
            productions.ForEvery(prod => mockGrammar
                .Setup(m => m.GetProduction(prod.symbol))
                .Returns(prod.production));

            var mockContext = new Mock<ILanguageContext>();
            mockContext
                .Setup(m => m.Grammar)
                .Returns(mockGrammar.Object);

            return mockContext.Object;
        }

        internal static Production MockProduction()
        {
            var rule = new Mock<Production.IRule>();
            var production = new Production("sym", rule.Object);
            return production;
        }
    }
}
