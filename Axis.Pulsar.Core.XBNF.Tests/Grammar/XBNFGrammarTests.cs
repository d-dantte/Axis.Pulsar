using Axis.Luna.Common;
using Axis.Pulsar.Core.Grammar.Rules;
using Axis.Pulsar.Core.Grammar.Rules.Atomic;

namespace Axis.Pulsar.Core.XBNF.Tests.Grammar
{
    [TestClass]
    public class XBNFGrammarTests
    {
        [TestMethod]
        public void Constructor_Tests()
        {
            var root = "root";
            var productions = ArrayUtil.Of(
                new Production("root", new TerminalLiteral("ble", "elb")));

            var grammar = XBNFGrammar.Of(root, productions);
            Assert.AreEqual(root, grammar.Root);
            Assert.IsTrue(ArrayUtil.Of("root").SequenceEqual(productions.Select(p => p.Symbol)));

            Assert.ThrowsException<ArgumentNullException>(
                () => XBNFGrammar.Of(null!, productions));

            Assert.ThrowsException<FormatException>(
                () => XBNFGrammar.Of(" !.tire4", productions));

            Assert.ThrowsException<ArgumentNullException>(
                () => XBNFGrammar.Of("bleh", null!));

            Assert.ThrowsException<ArgumentException>(
                () => XBNFGrammar.Of("bleh", []));

            Assert.ThrowsException<ArgumentException>(
                () => XBNFGrammar.Of("bleh", [null!]));
        }

        [TestMethod]
        public void Misc_Tests()
        {
            var root = "root";
            var production = new Production("root", new TerminalLiteral("ble", "elb"));
            var productions = ArrayUtil.Of(production);
            var grammar = XBNFGrammar.Of(root, productions);

            Assert.AreEqual(1, grammar.ProductionCount);
            Assert.IsTrue(grammar.ContainsProduction("root"));
            Assert.AreEqual(production, grammar.GetProduction("root"));
            Assert.AreEqual(production, grammar["root"]);
            Assert.IsTrue(grammar.TryGetProduction("root", out var prod));
            Assert.AreEqual(production, prod);

            Assert.IsFalse(grammar.TryGetProduction("non-root", out prod));
            Assert.IsNull(prod);
        }
    }
}
