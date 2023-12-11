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
    #endregion
}
