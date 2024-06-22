using Axis.Luna.Common;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Tests.Utils
{
    [TestClass]
    public class CharRangeTests
    {
        [TestMethod]
        public void Constructor_Tests()
        {
            var range = new CharRange('a', 'd');
            Assert.AreEqual('a', range.LowerBound);
            Assert.AreEqual('d', range.UpperBound);
            Assert.IsTrue(range.IsRange);

            range = new CharRange('@', '@');
            Assert.AreEqual('@', range.LowerBound);
            Assert.AreEqual('@', range.UpperBound);
            Assert.IsFalse(range.IsRange);

            range = default;
            Assert.AreEqual('\0', range.LowerBound);
            Assert.AreEqual('\0', range.UpperBound);
            Assert.IsFalse(range.IsRange);

            range = "a-z";
            Assert.AreEqual('a', range.LowerBound);
            Assert.AreEqual('z', range.UpperBound);

            range = '^';
            Assert.IsFalse(range.IsRange);
            Assert.AreEqual(range.LowerBound, range.UpperBound);
            Assert.AreEqual('^', range.UpperBound);

            Assert.ThrowsException<ArgumentException>(() => CharRange.Of('b', 'a'));
        }

        [TestMethod]
        public void Intersects_Tests()
        {
            var range = CharRange.Of('0', '3');
            var range2 = CharRange.Of('2', '4');
            var range3 = CharRange.Of('1', '1');
            var def = CharRange.Default;

            Assert.IsTrue(CharRange.Intersects(range, range2));
            Assert.IsTrue(CharRange.Intersects(range, range3));
            Assert.IsFalse(CharRange.Intersects(range2, range3));

            Assert.IsTrue(CharRange.Intersects(def, default));
            Assert.IsFalse(CharRange.Intersects(range, def));
            Assert.IsFalse(CharRange.Intersects(range2, def));
            Assert.IsFalse(CharRange.Intersects(range3, def));
        }

        [TestMethod]
        public void Equality_Tests()
        {
            var range = CharRange.Of('0', '9');
            var range2 = CharRange.Of('0', '9');
            var range3 = CharRange.Of('0');
            CharRange range4 = "2-5";

            Assert.AreEqual(range, range);
            Assert.IsTrue(range.Equals(range));
            Assert.IsTrue(range.Equals((object)range));
            Assert.IsFalse(range.Equals(new object()));
            Assert.IsFalse(range.Equals(range4));
#pragma warning disable CS1718 // Comparison made to same variable
            Assert.IsTrue(range == range);
#pragma warning restore CS1718 // Comparison made to same variable

            Assert.AreEqual(range, range2);
            Assert.IsTrue(range.Equals(range2));
            Assert.IsTrue(range.Equals((object)range2));
            Assert.IsTrue(range == range2);

            Assert.AreNotEqual(range, range3);
            Assert.IsFalse(range.Equals(range3));
            Assert.IsFalse(range.Equals((object)range3));
            Assert.IsTrue(range != range3);

            Assert.AreEqual(range3, range3);
            Assert.IsTrue(range3.Equals(range3));
            Assert.IsTrue(range3.Equals((object)range3));
#pragma warning disable CS1718 // Comparison made to same variable
            Assert.IsTrue(range3 == range3);
#pragma warning restore CS1718 // Comparison made to same variable
        }

        [TestMethod]
        public void Contains_Tests()
        {
            var range = CharRange.Of('0', '9');
            Assert.IsTrue(range.Contains('0'));
            Assert.IsTrue(range.Contains('9'));
            Assert.IsTrue(range.Contains('6'));
            Assert.IsFalse(range.Contains('a'));

            range = CharRange.Of('0');
            Assert.IsTrue(range.Contains('0'));
            Assert.IsFalse(range.Contains('\n'));
        }

        [TestMethod]
        public void Merge_Tests()
        {
            var range = CharRange.Of('0', '2');
            var range2 = CharRange.Of('2', '4');
            var range3 = CharRange.Of('3', '7');
            var range4 = CharRange.Of('9');

            var merged = range.TryMergeWith(range2, out var result);
            Assert.IsTrue(merged);
            Assert.AreEqual(range.LowerBound, result.LowerBound);
            Assert.AreEqual(range2.UpperBound, result.UpperBound);

            _ = range2.TryMergeWith(range, out var result_x);
            Assert.AreEqual(result, result_x);

            merged = result.TryMergeWith(range3, out result);
            Assert.IsTrue(merged);
            Assert.AreEqual(range.LowerBound, result.LowerBound);
            Assert.AreEqual(range3.UpperBound, result.UpperBound);

            merged = result.TryMergeWith(range4, false, out _);
            Assert.IsFalse(merged);

            merged = result.TryMergeWith(range4, true, out result);
            Assert.IsTrue(merged);
            Assert.AreEqual(range.LowerBound, result.LowerBound);
            Assert.AreEqual(range4.UpperBound, result.UpperBound);
        }

        [TestMethod]
        public void Parse_Tests()
        {
            var range = CharRange.Parse("1 - 3");
            Assert.AreEqual('1', range.LowerBound);
            Assert.AreEqual('3', range.UpperBound);

            range = CharRange.Parse("2");
            Assert.AreEqual('2', range.LowerBound);
            Assert.AreEqual('2', range.UpperBound);

            range = CharRange.Parse("1 -3");
            Assert.AreEqual('1', range.LowerBound);
            Assert.AreEqual('3', range.UpperBound);

            range = CharRange.Parse("1-3");
            Assert.AreEqual('1', range.LowerBound);
            Assert.AreEqual('3', range.UpperBound);

            range = CharRange.Parse("1     -3");
            Assert.AreEqual('1', range.LowerBound);
            Assert.AreEqual('3', range.UpperBound);

            range = CharRange.Parse("1 - \\");
            Assert.AreEqual('1', range.LowerBound);
            Assert.AreEqual('\\', range.UpperBound);

            range = CharRange.Parse("1 - \u12ab");
            Assert.AreEqual('1', range.LowerBound);
            Assert.AreEqual('\u12ab', range.UpperBound);

            range = CharRange.Parse("\\x2c - \\u12ab");
            Assert.AreEqual(',', range.LowerBound);
            Assert.AreEqual('\u12ab', range.UpperBound);

            Assert.ThrowsException<ArgumentException>(
                () => CharRange.Parse(null!));

            Assert.ThrowsException<FormatException>(
                () => CharRange.Parse("a-g-y"));
        }

        [TestMethod]
        public void ParseChar_Tests()
        {
            var @char = CharRange.ParseChar("a");
            Assert.AreEqual('a', @char);

            @char = CharRange.ParseChar("\\\\");
            Assert.AreEqual('\\', @char);

            @char = CharRange.ParseChar("\\x24");
            Assert.AreEqual('\x24', @char);

            @char = CharRange.ParseChar("\\uae3c");
            Assert.AreEqual('\uae3c', @char);

            Assert.ThrowsException<FormatException>(
                () => CharRange.Parse("\\xaz"));

            Assert.ThrowsException<FormatException>(
                () => CharRange.Parse("1234"));

            Assert.ThrowsException<FormatException>(
                () => CharRange.Parse("123456"));
        }

        [TestMethod]
        public void NormalizeRanges_Tests()
        {
            var ranges = ArrayUtil.Of(
                CharRange.Of('0', '2'),
                CharRange.Of('2', '4'),
                CharRange.Of('3', '7'),
                CharRange.Of('9'));

            var normalized = CharRange
                .NormalizeRanges(ranges)
                .ToArray();

            Assert.AreEqual(2, normalized.Length);
            Assert.AreEqual('0', normalized[0].LowerBound);
            Assert.AreEqual('7', normalized[0].UpperBound);
            Assert.AreEqual('9', normalized[1].LowerBound);
            Assert.AreEqual('9', normalized[1].UpperBound);

            Assert.ThrowsException<ArgumentNullException>(
                () => default(IEnumerable<CharRange>)!.NormalizeRanges());
        }

        [TestMethod]
        public void ToString_Tests()
        {
            CharRange range = "1-4";
            Assert.AreEqual("1-4", range.ToString());

            range = "£";
            Assert.AreEqual("£", range.ToString());
        }
    }
}
