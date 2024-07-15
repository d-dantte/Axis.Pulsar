using Axis.Pulsar.Core.Grammar.Rules.Atomic;
using Axis.Pulsar.Core.XBNF.RuleFactories;

namespace Axis.Pulsar.Core.XBNF.Tests.RuleFactories
{
    [TestClass]
    public class EOFRuleFactoryTests
    {
        [TestMethod]
        public void NewRule_Tests()
        {
            var factory = new EOFRuleFactory();
            var rule = factory.NewRule("abcd", null!, null!);
            Assert.IsInstanceOfType<EOF>(rule);
        }
    }
}
