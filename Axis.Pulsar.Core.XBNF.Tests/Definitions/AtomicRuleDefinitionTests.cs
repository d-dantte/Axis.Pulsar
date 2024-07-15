using Axis.Pulsar.Core.Grammar.Rules.Atomic;
using Axis.Pulsar.Core.XBNF.Definitions;
using Axis.Pulsar.Core.XBNF.Lang;
using System.Collections.Immutable;

namespace Axis.Pulsar.Core.XBNF.Tests.Definitions
{
    [TestClass]
    public class AtomicRuleDefinitionTests
    {
        [TestMethod]
        public void Construction_Tests()
        {
            var delimiter = IAtomicRuleFactory.ContentArgumentDelimiter.BackSol;
            var factory = new FauxFactory();
            string[] symbols = ["abc", "xyz"];

            var definition = AtomicRuleDefinition.Of(delimiter, factory, symbols);
            Assert.IsNotNull(definition);
            Assert.AreEqual(delimiter, definition.ContentDelimiterType);
            Assert.AreEqual(factory, definition.Factory);
            Assert.IsTrue(symbols.SequenceEqual(definition.Symbols.Order()));

            definition = AtomicRuleDefinition.Of(factory, symbols);
            Assert.IsNotNull(definition);
            Assert.AreEqual(
                IAtomicRuleFactory.ContentArgumentDelimiter.None,
                definition.ContentDelimiterType);
            Assert.AreEqual(factory, definition.Factory);
            Assert.IsTrue(symbols.SequenceEqual(definition.Symbols.Order()));

            definition = AtomicRuleDefinition.Of<FauxFactory>(symbols);
            Assert.IsNotNull(definition);
            Assert.AreEqual(
                IAtomicRuleFactory.ContentArgumentDelimiter.None,
                definition.ContentDelimiterType);
            Assert.IsInstanceOfType<FauxFactory>(definition.Factory);
            Assert.IsTrue(symbols.SequenceEqual(definition.Symbols.Order()));

            definition = AtomicRuleDefinition.Of<FauxFactory>(delimiter, symbols);
            Assert.IsNotNull(definition);
            Assert.AreEqual(delimiter, definition.ContentDelimiterType);
            Assert.IsInstanceOfType<FauxFactory>(definition.Factory);
            Assert.IsTrue(symbols.SequenceEqual(definition.Symbols.Order()));

            Assert.ThrowsException<ArgumentNullException>(
                () => AtomicRuleDefinition.Of(null!, symbols));

            var invalidDelm = (IAtomicRuleFactory.ContentArgumentDelimiter)(-2);
            Assert.ThrowsException<ArgumentException>(
                () => AtomicRuleDefinition.Of(invalidDelm, factory, symbols));

            Assert.ThrowsException<ArgumentNullException>(
                () => AtomicRuleDefinition.Of(factory, (string[])null!));

            Assert.ThrowsException<ArgumentException>(
                () => AtomicRuleDefinition.Of(factory, []));

            Assert.ThrowsException<FormatException>(
                () => AtomicRuleDefinition.Of(factory, ["12x-.zy"]));

            Assert.ThrowsException<InvalidOperationException>(
                () => AtomicRuleDefinition.Of(factory, ["abc", "abc"]));
        }


        public class FauxFactory : IAtomicRuleFactory
        {
            public IAtomicRule NewRule(string ruleId, LanguageMetadata metadata, ImmutableDictionary<IAtomicRuleFactory.IArgument, string> arguments)
            {
                throw new NotImplementedException();
            }
        }
    }
}
