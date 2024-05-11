using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Composite.Group;

namespace Axis.Pulsar.Core.Tests.Grammar.Composite.Groups
{
    [TestClass]
    public class SymbolAggregationTests
    {
        #region Unit
        [TestMethod]
        public void Construction_Tests()
        {
            var node = ISymbolNode.Of("stuff", "stuff");
            var unit = new ISymbolNodeAggregation.Unit(node);

            Assert.AreEqual(node, unit.Node);
            Assert.IsFalse(unit.IsDefault);

            unit = ISymbolNodeAggregation.Unit.Default;
            Assert.IsTrue(unit.IsDefault);
        }
        #endregion
    }
}
