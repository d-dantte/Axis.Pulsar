using Axis.Luna.Common;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Rules.Atomic;
using Axis.Pulsar.Core.Grammar.Validation;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.XBNF.Definitions;
using Axis.Pulsar.Core.XBNF.Lang;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Axis.Pulsar.Core.XBNF.IAtomicRuleFactory;

namespace Axis.Pulsar.Core.XBNF.Tests.Lang
{
    [TestClass]
    public class LanguageMetadataTests
    {
        [TestMethod]
        public void Constructor_Tests()
        {
            var ruleDefs = ArrayUtil.Of(
                AtomicRuleDefinition.Of(new FauxRuleFactory(), "abcd"));
            var prodDefs = ArrayUtil.Of(
                ProductionValidatorDefinition.Of("abcd", new FauxValidator()));
            var lmeta = new LanguageMetadata(ruleDefs, prodDefs);

            Assert.IsNotNull(lmeta);
            Assert.AreEqual(1, lmeta.AtomicRuleDefinitionMap.Count);
            Assert.AreEqual(1, lmeta.ProductionValidatorDefinitionMap.Count);
            Assert.AreEqual(0, lmeta.AtomicContentTypeMap.Count);

            Assert.ThrowsException<ArgumentNullException>(
                () => new LanguageMetadata(null!, prodDefs));

            Assert.ThrowsException<ArgumentException>(
                () => new LanguageMetadata([null!], prodDefs));

            ruleDefs = ArrayUtil.Of(
                AtomicRuleDefinition.Of(new FauxRuleFactory(), "abcd"),
                AtomicRuleDefinition.Of(new FauxRuleFactory(), "abcd"));
            Assert.ThrowsException<InvalidOperationException>(
                () => new LanguageMetadata(ruleDefs, prodDefs));


            ruleDefs = ArrayUtil.Of(
                AtomicRuleDefinition.Of(new FauxRuleFactory(), "abcd"),
                AtomicRuleDefinition.Of(new FauxRuleFactory(), "xyz"));
            Assert.ThrowsException<ArgumentNullException>(
                () => new LanguageMetadata(ruleDefs, null!));
            Assert.ThrowsException<ArgumentException>(
                () => new LanguageMetadata(ruleDefs, [null!]));
        }

        public class FauxRuleFactory : IAtomicRuleFactory
        {
            public IAtomicRule NewRule(string ruleId, LanguageMetadata metadata, ImmutableDictionary<IAtomicRuleFactory.IArgument, string> arguments)
            {
                throw new NotImplementedException();
            }
        }

        public class FauxValidator : IProductionValidator
        {
            public Status Validate(SymbolPath symbolPath, ILanguageContext context, ISymbolNode recogniedNode)
            {
                throw new NotImplementedException();
            }
        }

    }
}
