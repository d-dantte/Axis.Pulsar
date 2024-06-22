using Axis.Pulsar.Core.Grammar.Rules.Aggregate;

namespace Axis.Pulsar.Core.Tests.Grammar.Rules.Aggregate
{
    [TestClass]
    public class CardinalityTests
    {
        [TestMethod]
        public void Constructor_Tests()
        {
            var cardinality = Cardinality.OccursOnlyOnce();
            Assert.AreEqual(1, cardinality.MinOccurence);
            Assert.AreEqual(1, cardinality.MaxOccurence);
            Assert.IsTrue(cardinality.IsClosed);
            Assert.IsFalse(cardinality.IsOpen);
            Assert.IsFalse(cardinality.IsZeroMinOccurence);
            Assert.IsFalse(cardinality.IsDefault);

            cardinality = Cardinality.OccursAtLeastOnce();
            Assert.IsTrue(cardinality.IsOpen);

            cardinality = Cardinality.OccursAtMost(5);
            Assert.IsTrue(cardinality.IsAny);

            cardinality = Cardinality.OccursNeverOrMore();
            Assert.IsTrue(cardinality.IsProbable);

            cardinality = Cardinality.OccursNeverOrAtMost(1);
            Assert.IsFalse(cardinality.IsProbable);
            Assert.IsTrue(cardinality.IsOptional);

            cardinality = Cardinality.Occurs(1, null);
            Assert.IsFalse(cardinality.IsDefault);

            cardinality = Cardinality.Default;
            Assert.IsTrue(cardinality.IsDefault);

            Assert.ThrowsException<InvalidOperationException>(
                () => Cardinality.Occurs(3, 1));

            Assert.ThrowsException<InvalidOperationException>(
                () => Cardinality.Occurs(0, 0));
        }

        [TestMethod]
        public void IsOptional_Tests()
        {
            var cardinality = Cardinality.OccursOptionally();
            Assert.IsTrue(cardinality.IsOptional);

            cardinality = Cardinality.Occurs(1, 1);
            Assert.IsFalse(cardinality.IsOptional);

            cardinality = Cardinality.Occurs(0, 2);
            Assert.IsFalse(cardinality.IsOptional);
        }

        [TestMethod]
        public void IsProbable_Tests()
        {
            var cardinality = Cardinality.Occurs(0, null);
            Assert.IsTrue(cardinality.IsProbable);

            cardinality = Cardinality.Occurs(0, 1);
            Assert.IsFalse(cardinality.IsProbable);

            cardinality = Cardinality.Occurs(1, null);
            Assert.IsFalse(cardinality.IsProbable);
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
            Assert.IsTrue(zeroOrMore.IsValidRepetition(0));
            Assert.IsTrue(zeroOrMore.IsValidRepetition(1));
            Assert.IsTrue(zeroOrMore.IsValidRepetition(int.MaxValue));
            Assert.IsFalse(zeroOrMore.IsValidRepetition(-1));

            // zero or one
            Assert.IsTrue(zeroOrOne.IsValidRepetition(0));
            Assert.IsTrue(zeroOrOne.IsValidRepetition(1));
            Assert.IsFalse(zeroOrOne.IsValidRepetition(2));
            Assert.IsFalse(zeroOrOne.IsValidRepetition(-1));

            // one or more
            Assert.IsFalse(oneOrMore.IsValidRepetition(0));
            Assert.IsTrue(oneOrMore.IsValidRepetition(1));
            Assert.IsTrue(oneOrMore.IsValidRepetition(int.MaxValue));
            Assert.IsFalse(oneOrMore.IsValidRepetition(-1));

            // one or three
            Assert.IsFalse(oneOrThree.IsValidRepetition(0));
            Assert.IsFalse(oneOrThree.IsValidRepetition(4));
            Assert.IsTrue(oneOrThree.IsValidRepetition(1));
            Assert.IsTrue(oneOrThree.IsValidRepetition(3));

            // three
            Assert.IsFalse(three.IsValidRepetition(0));
            Assert.IsFalse(three.IsValidRepetition(1));
            Assert.IsFalse(three.IsValidRepetition(2));
            Assert.IsFalse(three.IsValidRepetition(4));
            Assert.IsTrue(three.IsValidRepetition(3));
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

        [TestMethod]
        public void Equals_WithObject_Tests()
        {
            var cardinality = Cardinality.OccursOnlyOnce();
            Assert.IsFalse(cardinality.Equals(new object()));
            Assert.IsTrue(cardinality.Equals((object)cardinality));
        }

        [TestMethod]
        public void Equals_Tests()
        {
            var cardinality = Cardinality.OccursOnlyOnce();
            Assert.IsTrue(cardinality.Equals(cardinality));
            Assert.IsFalse(cardinality.Equals(Cardinality.Occurs(1, 2)));
            Assert.IsFalse(cardinality.Equals(Cardinality.Occurs(2, 2)));

            Assert.IsFalse(cardinality == Cardinality.Occurs(1, 2));
            Assert.IsTrue(cardinality != Cardinality.Occurs(1, 2));
        }

        [TestMethod]
        public void HashCode_Tests()
        {
            var cardinality = Cardinality.OccursOnlyOnce();

            var expected = HashCode.Combine(cardinality.MinOccurence, cardinality.MaxOccurence);
            var hash = cardinality.GetHashCode();
            Assert.AreEqual(expected, hash);
        }

        [TestMethod]
        public void ToString_Tests()
        {
            Assert.AreEqual("", Cardinality.Occurs(1, 1).ToString());
            Assert.AreEqual(".2", Cardinality.Occurs(2, 2).ToString());
            Assert.AreEqual(".?", Cardinality.Occurs(0, 1).ToString());
            Assert.AreEqual(".*", Cardinality.Occurs(0, null).ToString());
            Assert.AreEqual(".+", Cardinality.Occurs(1, null).ToString());
            Assert.AreEqual(".2+", Cardinality.Occurs(2, null).ToString());
            Assert.AreEqual(".2,5", Cardinality.Occurs(2, 5).ToString());
        }

        [TestMethod]
        public void CanRepeat_Tests()
        {
            var cardinality = Cardinality.OccursOnly(2);
            Assert.IsTrue(Cardinality.OccursNeverOrMore().CanRepeat(1));
            Assert.IsTrue(cardinality.CanRepeat(1));
            Assert.IsFalse(cardinality.CanRepeat(2));
        }

        [TestMethod]
        public void IsValidRange_Tests()
        {
            var cardinality = Cardinality.OccursOnly(2);
            Assert.IsTrue(cardinality.IsValidRepetition(2));
            Assert.IsFalse(cardinality.IsValidRepetition(3));

            cardinality = Cardinality.OccursAtLeast(3);
            Assert.IsTrue(cardinality.IsValidRepetition(21));
        }
    }
}
