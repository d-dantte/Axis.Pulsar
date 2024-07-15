using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Rules;
using Axis.Pulsar.Core.Grammar.Validation;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Lang;

namespace Axis.Pulsar.Core.XBNF.Tests.Parsers
{
    [TestClass]
    public class ParserContextTests
    {
        [TestMethod]
        public void Constructor_Tests()
        {
            var metadata = LanguageMetadataBuilder
                .NewBuilder()
                .WithProductionValidator(new XBNF.Definitions.ProductionValidatorDefinition("symbol", new FauxValidator()))
                .Build();

            var cxt = new XBNF.Parsers.ParserContext(metadata);
            Assert.AreEqual(metadata, cxt.Metadata);
            Assert.AreEqual(0, cxt.AtomicRuleArguments.Count);
        }

        [TestMethod]
        public void AppendAtomicRuleArguments_Tests()
        {
            var metadata = LanguageMetadataBuilder
                .NewBuilder()
                .Build();
            var cxt = new XBNF.Parsers.ParserContext(metadata);
            var param = IAtomicRuleFactory.Parameter.Of(
                    IAtomicRuleFactory.RegularArgument.Of("stuff"),
                    "bleh");

            cxt.AppendAtomicRuleArguments("sym", param);
            Assert.AreEqual(1, cxt.AtomicRuleArguments.Count);
            Assert.IsTrue(cxt.AtomicRuleArguments.TryGetValue("sym", out var @params));

            Assert.ThrowsException<InvalidOperationException>(
                () => cxt.AppendAtomicRuleArguments("sym", param));
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
