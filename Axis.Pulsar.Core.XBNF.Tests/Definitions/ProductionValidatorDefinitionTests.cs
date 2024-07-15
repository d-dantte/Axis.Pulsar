using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Validation;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.XBNF.Definitions;

namespace Axis.Pulsar.Core.XBNF.Tests.Definitions
{
    [TestClass]
    public class ProductionValidatorDefinitionTests
    {
        [TestMethod]
        public void Constructor_Tests()
        {
            var symbol = "sym";
            var validator = new FauxValiator();
            var definition = ProductionValidatorDefinition.Of(symbol, validator);
            Assert.AreEqual(symbol, definition.Symbol);
            Assert.AreEqual(validator, definition.Validator);

            Assert.ThrowsException<ArgumentNullException>(
                () => ProductionValidatorDefinition.Of(symbol, null!));

            Assert.ThrowsException<ArgumentNullException>(
                () => ProductionValidatorDefinition.Of(null!, validator));

            Assert.ThrowsException<FormatException>(
                () => ProductionValidatorDefinition.Of("!6545DFrtygf", validator));
        }

        public class FauxValiator : IProductionValidator
        {
            public Status Validate(SymbolPath symbolPath, ILanguageContext context, ISymbolNode recogniedNode)
            {
                throw new NotImplementedException();
            }
        }
    }
}
