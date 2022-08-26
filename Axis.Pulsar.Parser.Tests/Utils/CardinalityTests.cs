using Axis.Pulsar.Parser.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Axis.Pulsar.Parser.Tests.Utils
{
    [TestClass]
    public class CardinalityTests
    {

        [TestMethod]
        public void Constructor_ReturnsValidObject()
        {
            var c = Cardinality.Occurs(0, 1);
            Assert.AreEqual(0, c.MinOccurence);
            Assert.AreEqual(1, c.MaxOccurence);
        }

        [TestMethod]
        public void FactoryMethods_ReturnValidObjects()
        {
            var d = Cardinality.OccursAtLeastOnce();
            var c = Cardinality.OccursAtLeast(3);

            Assert.AreEqual(3, c.MinOccurence);
            Assert.IsNull(c.MaxOccurence);

            Assert.AreEqual(1, d.MinOccurence);
            Assert.IsNull(d.MaxOccurence);
        }
    }
}
