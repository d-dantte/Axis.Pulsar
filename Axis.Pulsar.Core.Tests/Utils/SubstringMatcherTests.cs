using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Tests.Utils;


[TestClass]
public class SubstringMatcherTests
{
    #region LookAhead Matcher
    [TestMethod]
    public void TryNextWindow_LookAhead_Tests()
    {
        var matcher = SubstringMatcher.LookAheadMatcher.Of(
            "abc",
            "bc abc c",
            0);

        var moved = matcher.TryNextWindow(out var matched);
        Assert.IsTrue(moved);
        Assert.IsFalse(matched);

        moved = matcher.TryNextWindow(out matched);
        Assert.IsTrue(moved);
        Assert.IsFalse(matched);

        moved = matcher.TryNextWindow(out matched);
        Assert.IsTrue(moved);
        Assert.IsFalse(matched);

        moved = matcher.TryNextWindow(out matched);
        Assert.IsTrue(moved);
        Assert.IsTrue(matched);

        moved = matcher.TryNextWindow(out matched);
        Assert.IsTrue(moved);
        Assert.IsFalse(matched);

        moved = matcher.TryNextWindow(out matched);
        Assert.IsTrue(moved);
        Assert.IsFalse(matched);

        moved = matcher.TryNextWindow(out matched);
        Assert.IsFalse(moved);
        Assert.IsFalse(matched);
    }

    [TestMethod]
    public void TrySkip_LookAhead_Tests()
    {
        var matcher = SubstringMatcher.LookAheadMatcher.Of(
            "abc",
            "bc abc c",
            0);

        var skipped = matcher.Advance(3);
        Assert.AreEqual(3, matcher.Index);
        Assert.AreEqual(3, skipped);

        var moved = matcher.TryNextWindow(out bool ismatch);
        Assert.IsTrue(moved);
        Assert.IsTrue(ismatch);

        skipped = matcher.Advance(40);
        Assert.AreNotEqual(40, skipped);
        Assert.AreEqual(2, skipped);

        moved = matcher.TryNextWindow(out ismatch);
        Assert.IsFalse(moved);
        Assert.IsFalse(ismatch);
    }

    [TestMethod]
    public void LookaheadAdvance_Tests()
    {
        var matcher = SubstringMatcher.LookAheadMatcher.Of(
            "abc",
            "bc abc c",
            0);

        var advanced = matcher.Advance(1);
        Assert.AreEqual(1, matcher.Index);

        Assert.ThrowsException<ArgumentOutOfRangeException>(
            () => matcher.Advance(-1));
    }

    [TestMethod]
    public void Looahead_Constructor_Tests()
    {
        var matcher = SubstringMatcher.LookAheadMatcher.Of(
            "abc",
            "bc abc c",
            0);

        Assert.IsNotNull(matcher);
        Assert.AreEqual<Tokens>("abc", matcher.Pattern);

        Assert.ThrowsException<ArgumentOutOfRangeException>(
            () => SubstringMatcher.LookAheadMatcher.Of("abc", "abcd", -1));

        Assert.ThrowsException<ArgumentOutOfRangeException>(
            () => SubstringMatcher.LookAheadMatcher.Of("abc", "abcd", 10));
    }
    #endregion

    #region LookBehind Matcher
    [TestMethod]
    public void TryNextWindow_LookBehind_Tests()
    {
        var matcher = SubstringMatcher.LookBehindMatcher.Of(
            "abc",
            "bc abc c",
            0);

        var moved = matcher.TryNextWindow(out var matched);
        Assert.IsTrue(moved);
        Assert.IsFalse(matched);

        moved = matcher.TryNextWindow(out matched);
        Assert.IsTrue(moved);
        Assert.IsFalse(matched);

        moved = matcher.TryNextWindow(out matched);
        Assert.IsTrue(moved);
        Assert.IsFalse(matched);

        moved = matcher.TryNextWindow(out matched);
        Assert.IsTrue(moved);
        Assert.IsFalse(matched);

        moved = matcher.TryNextWindow(out matched);
        Assert.IsTrue(moved);
        Assert.IsFalse(matched);

        moved = matcher.TryNextWindow(out matched);
        Assert.IsTrue(moved);
        Assert.IsTrue(matched);

        moved = matcher.TryNextWindow(out matched);
        Assert.IsTrue(moved);
        Assert.IsFalse(matched);

        moved = matcher.TryNextWindow(out matched);
        Assert.IsTrue(moved);
        Assert.IsFalse(matched);

        moved = matcher.TryNextWindow(out matched);
        Assert.IsFalse(moved);
        Assert.IsFalse(matched);
    }

    [TestMethod]
    public void TrySkip_LookBehind_Tests()
    {
        var matcher = SubstringMatcher.LookBehindMatcher.Of(
            "abc",
            "bc abc c",
            0);

        var skipped = matcher.Advance(5);
        Assert.AreEqual(5, matcher.Index);
        Assert.AreEqual(5, skipped);

        var moved = matcher.TryNextWindow(out bool ismatch);
        Assert.IsTrue(moved);
        Assert.IsTrue(ismatch);

        skipped = matcher.Advance(40);
        Assert.AreNotEqual(40, skipped);
        Assert.AreEqual(2, skipped);

        moved = matcher.TryNextWindow(out ismatch);
        Assert.IsFalse(moved);
        Assert.IsFalse(ismatch);
    }

    [TestMethod]
    public void LookbehindAdvance_Tests()
    {
        var matcher = SubstringMatcher.LookBehindMatcher.Of(
            "abc",
            "bc abc c",
            0);

        var advanced = matcher.Advance(1);
        Assert.AreEqual(1, matcher.Index);

        Assert.ThrowsException<ArgumentOutOfRangeException>(
            () => matcher.Advance(-1));
    }

    [TestMethod]
    public void IsValidWindow_Tests()
    {
        var matcher = SubstringMatcher.LookBehindMatcher.Of(
            "abc",
            "bc abc c",
            0);

        Assert.IsFalse(matcher.IsValidWindow);

        matcher.Advance(2);
        Assert.IsTrue(matcher.IsValidWindow);

        matcher.Advance(20);
        Assert.IsFalse(matcher.IsValidWindow);
    }

    [TestMethod]
    public void Lookbehind_Constructor_Tests()
    {
        var matcher = SubstringMatcher.LookBehindMatcher.Of(
            "abc",
            "bc abc c",
            0);

        Assert.IsNotNull(matcher);
        Assert.AreEqual<Tokens>("abc", matcher.Pattern);

        Assert.ThrowsException<ArgumentOutOfRangeException>(
            () => SubstringMatcher.LookBehindMatcher.Of("abc", "abcd", -1));

        Assert.ThrowsException<ArgumentOutOfRangeException>(
            () => SubstringMatcher.LookBehindMatcher.Of("abc", "abcd", 10));
    }

    [TestMethod]
    public void NextPattern_Tests()
    {
        var matcher = SubstringMatcher.LookBehindMatcher.Of(
            "abc",
            "bc abc cabc  abc bb ac abc",
            0);

        var tokens = matcher.NextPattern;
        Assert.AreEqual<Tokens>("b", tokens);

        matcher.Advance(3);
        tokens = matcher.NextPattern;
        Assert.AreEqual<Tokens>("c a", tokens);

        matcher.Advance(40);
        tokens = matcher.NextPattern;
        Assert.AreEqual<Tokens>("bc", tokens);
    }
    #endregion
}
