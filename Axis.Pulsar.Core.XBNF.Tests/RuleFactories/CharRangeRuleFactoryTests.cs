using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Axis.Pulsar.Core.XBNF.Tests.RuleFactories
{
    public class CharRangeRuleFactoryTests
    {

    }

    [TestClass]
    public class RangesEscapeTransformerTests
    {
        [TestMethod]
        public void DecodeTests()
        {
            var transformer = new CharRangeRuleFactory.RangesEscapeTransformer();

            var decoded = transformer.Decode("a-z");
            Assert.AreEqual("a-z", decoded);
        }
    }
}
