using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Rules;
using Axis.Pulsar.Core.Grammar.Validation;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.XBNF.Lang;

namespace Axis.Pulsar.Core.XBNF.Tests.Lang
{
    [TestClass]
    public class XBNFLanguageContextTests
    {
        [TestMethod]
        public void Constructor_Tests()
        {
            var pcxt = LanguageMetadataBuilder
                .NewBuilder()
                .WithProductionValidator(new XBNF.Definitions.ProductionValidatorDefinition("symbol", new FauxValidator()))
                .Build()
                .ApplyTo(metadata => new XBNF.Parsers.ParserContext(metadata));

            var lcxt = new XBNFLanguageContext(new FauxGrammar(), pcxt);
            Assert.AreEqual(pcxt.Metadata, lcxt.Metadata);
            Assert.AreEqual(0, lcxt.AtomicRuleArguments.Count);
            Assert.AreEqual(1, lcxt.ProductionValidators.Count);

            Assert.ThrowsException<ArgumentNullException>(
                () => new XBNFLanguageContext(null!, pcxt));

            pcxt.AppendAtomicRuleArguments(
                string.Empty,
                IAtomicRuleFactory.Parameter.Of(
                    IAtomicRuleFactory.RegularArgument.Of("stuff"),
                    "value of stuff"));
            Assert.ThrowsException<InvalidOperationException>(
                () => new XBNFLanguageContext(new FauxGrammar(), pcxt));
        }

        #region Nested types
        internal class FauxGrammar : IGrammar
        {
            public Production this[string name] => throw new NotImplementedException();

            public string Root => throw new NotImplementedException();

            public int ProductionCount => throw new NotImplementedException();

            public IEnumerable<string> ProductionSymbols => throw new NotImplementedException();

            public bool ContainsProduction(string symbolName)
            {
                throw new NotImplementedException();
            }

            public Production GetProduction(string name)
            {
                throw new NotImplementedException();
            }

            public bool TryGetProduction(string name, out Production? production)
            {
                throw new NotImplementedException();
            }
        }

        internal class FauxValidator : IProductionValidator
        {
            public Status Validate(SymbolPath symbolPath, ILanguageContext context, ISymbolNode recogniedNode)
            {
                throw new NotImplementedException();
            }
        }
        #endregion
    }
}
