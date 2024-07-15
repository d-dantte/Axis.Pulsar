using Axis.Pulsar.Core.Grammar.Rules.Atomic;
using System.Collections.Immutable;

namespace Axis.Pulsar.Core.XBNF.Tests.RuleFactories
{
    [TestClass]
    public class PatternRuleFactoryTests
    {
        [TestMethod]
        public void ParseRanges_Tests()
        {
            var ranges = CharRangeRuleFactory.ParseRanges("^\\n, ^\\x0d, \\s");
            var excludes = ranges.Excludes.ToArray();
            var includes = ranges.Includes.ToArray();
            Assert.AreEqual(2, excludes.Count());
            Assert.AreEqual('\n', excludes[0]);
            Assert.AreEqual('\r', excludes[1]);
            Assert.AreEqual(1, includes.Length);
            Assert.AreEqual(' ', includes[0]);
        }

        [TestMethod]
        public void NewRule_Tests()
        {
            var factory = new CharRangeRuleFactory();
            var cxt = new XBNF.Lang.LanguageMetadata([], []);
            var map = new Dictionary<IAtomicRuleFactory.IArgument, string>();
            Assert.ThrowsException<ArgumentException>(
                () => new CharRangeRuleFactory().NewRule("abc", cxt, map.ToImmutableDictionary()));

            map[CharRangeRuleFactory.RangesArgument] = "a-z";
            var rule = factory.NewRule("bleh", cxt, map.ToImmutableDictionary());
            Assert.IsInstanceOfType<CharacterRanges>(rule);
        }

        [TestMethod]
        public void Unescape_Tests()
        {
            var result = CharRangeRuleFactory.Unescape(null!);
            Assert.IsNull(result);

            result = CharRangeRuleFactory.Unescape("\\'\\^\\ ");
            Assert.AreEqual("'^ ", result);
        }

        [TestMethod]
        public void Escape_Tests()
        {
            var result = CharRangeRuleFactory.Escape(null!);
            Assert.IsNull(result);

            result = CharRangeRuleFactory.Escape("'^ ");
            Assert.AreEqual("\\'\\^\\ ", result);
        }
    }
}
