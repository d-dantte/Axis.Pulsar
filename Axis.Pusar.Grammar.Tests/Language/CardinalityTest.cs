using Axis.Pulsar.Grammar.Language;

namespace Axis.Pusar.Grammar.Tests.Language
{
    [TestClass]
    public class CardinalityTest
    {
        [TestMethod]
        public void Constructor_WithInvalidArgs_ShouldThrowException()
        {
            Assert.ThrowsException<ArgumentException>(() => Cardinality.Occurs(-1, 1));
            Assert.ThrowsException<ArgumentException>(() => Cardinality.Occurs(1, -1));
            Assert.ThrowsException<InvalidOperationException>(() => Cardinality.Occurs(2, 1));
            Assert.ThrowsException<InvalidOperationException>(() => Cardinality.Occurs(0, 0));
        }

        [TestMethod]
        public void Property_AssignmentTests()
        {
            var cardinality = Cardinality.Occurs(1, 2);
            Assert.AreEqual(1, cardinality.MinOccurence);
            Assert.AreEqual(2, cardinality.MaxOccurence);

            cardinality = Cardinality.Occurs(3, null);
            Assert.AreEqual(3, cardinality.MinOccurence);
            Assert.IsNull(cardinality.MaxOccurence);
        }

        [TestMethod]
        public void IsValidRangeTests()
        {
            var zeroOrMore = Cardinality.Occurs(0, null);
            var zeroOrOne = Cardinality.Occurs(0, 1);
            var oneOrMore = Cardinality.Occurs(1, null);
            var oneOrThree = Cardinality.Occurs(1, 3);
            var three = Cardinality.Occurs(3, 3);

            // zero or more
            Assert.IsTrue(zeroOrMore.IsValidRange(0));
            Assert.IsTrue(zeroOrMore.IsValidRange(1));
            Assert.IsTrue(zeroOrMore.IsValidRange(int.MaxValue));
            Assert.IsFalse(zeroOrMore.IsValidRange(-1));

            // zero or one
            Assert.IsTrue(zeroOrOne.IsValidRange(0));
            Assert.IsTrue(zeroOrOne.IsValidRange(1));
            Assert.IsFalse(zeroOrOne.IsValidRange(2));
            Assert.IsFalse(zeroOrOne.IsValidRange(-1));

            // one or more
            Assert.IsFalse(oneOrMore.IsValidRange(0));
            Assert.IsTrue(oneOrMore.IsValidRange(1));
            Assert.IsTrue(oneOrMore.IsValidRange(int.MaxValue));
            Assert.IsFalse(oneOrMore.IsValidRange(-1));

            // one or three
            Assert.IsFalse(oneOrThree.IsValidRange(0));
            Assert.IsFalse(oneOrThree.IsValidRange(4));
            Assert.IsTrue(oneOrThree.IsValidRange(1));
            Assert.IsTrue(oneOrThree.IsValidRange(3));

            // three
            Assert.IsFalse(three.IsValidRange(0));
            Assert.IsFalse(three.IsValidRange(1));
            Assert.IsFalse(three.IsValidRange(2));
            Assert.IsFalse(three.IsValidRange(4));
            Assert.IsTrue(three.IsValidRange(3));
        }

        [TestMethod]
        public void CanRepeatTests()
        {
            var zeroOrMore = Cardinality.Occurs(0, null);
            Assert.IsTrue(zeroOrMore.CanRepeat(0));
            Assert.IsTrue(zeroOrMore.CanRepeat(1));
            Assert.IsTrue(zeroOrMore.CanRepeat(int.MaxValue));
            Assert.IsTrue(zeroOrMore.CanRepeat(-1));

            var zeroOrOne = Cardinality.Occurs(0, 1);
            Assert.IsTrue(zeroOrOne.CanRepeat(-1));
            Assert.IsTrue(zeroOrOne.CanRepeat(0));
            Assert.IsFalse(zeroOrOne.CanRepeat(1));

            var oneOrMore = Cardinality.Occurs(1, null);
            Assert.IsTrue(oneOrMore.CanRepeat(-1));
            Assert.IsTrue(oneOrMore.CanRepeat(0));
            Assert.IsTrue(oneOrMore.CanRepeat(int.MaxValue));

            var oneOrThree = Cardinality.Occurs(1, 3);
            Assert.IsTrue(oneOrThree.CanRepeat(-1));
            Assert.IsTrue(oneOrThree.CanRepeat(0));
            Assert.IsTrue(oneOrThree.CanRepeat(2));
            Assert.IsFalse(oneOrThree.CanRepeat(3));

            var four = Cardinality.Occurs(4, 4);
            Assert.IsTrue(four.CanRepeat(-1));
            Assert.IsTrue(four.CanRepeat(0));
            Assert.IsTrue(four.CanRepeat(2));
            Assert.IsFalse(four.CanRepeat(4));
        }
    }
}
