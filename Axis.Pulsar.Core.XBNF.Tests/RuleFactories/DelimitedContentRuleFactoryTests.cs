using Axis.Pulsar.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Axis.Pulsar.Core.XBNF.RuleFactories.DelimitedContentRuleFactory;

namespace Axis.Pulsar.Core.XBNF.Tests.RuleFactories
{
    [TestClass]
    public class DelimitedContentRuleFactoryTests
    {
    }

    [TestClass]
    public class CharacterRangeParserTests
    {
        [TestMethod]
        public void TryParseRange_Tests()
        {
            var result = CharacterRangesParser.TryParseRange("a- b", out var range);
            Assert.AreEqual<CharRange>("a-b", range);

            result = CharacterRangesParser.TryParseRange("t ", out range);
            Assert.AreEqual<CharRange>("t", range);
        }

        [TestMethod]
        public void TryParseRanges_Tests()
        {
            var result = CharacterRangesParser.TryParseRanges("a- b, t , d", out var ranges);
            Assert.AreEqual(3, ranges.Length);
            Assert.AreEqual("a-b", ranges[0]);
            Assert.AreEqual("t", ranges[1]);
            Assert.AreEqual("d", ranges[2]);
        }
    }
}
