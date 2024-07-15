using Axis.Pulsar.Core.Grammar.Rules.Atomic;
using System.Collections.Immutable;

namespace Axis.Pulsar.Core.XBNF.Tests.RuleFactories
{
    [TestClass]
    public class LiteralRuleFactoryTests
    {
        [TestMethod]
        public void NewRule_Tests()
        {
            var factory = new LiteralRuleFactory();
            var cxt = new XBNF.Lang.LanguageMetadata([], []);
            var map = new Dictionary<IAtomicRuleFactory.IArgument, string>();
            Assert.ThrowsException<ArgumentNullException>(
                () => factory.NewRule("abc", cxt, null!));
            Assert.ThrowsException<ArgumentException>(
                () => factory.NewRule("abc", cxt, map.ToImmutableDictionary()));

            map[LiteralRuleFactory.LiteralArgument] = "bleh";
            var rule = factory.NewRule("bleh", cxt, map.ToImmutableDictionary());
            Assert.IsInstanceOfType<TerminalLiteral>(rule);
        }
    }
}
