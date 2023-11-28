using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Tests.Utils;


[TestClass]
public class SubstringMatcherTests
{
    #region LookAhead Matcher
    [TestMethod]
    public void TryNextWindow_LookAhead_Tests()
    {
        var matcher = SubstringMatcher.OfLookAhead(
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
        var matcher = SubstringMatcher.OfLookAhead(
            "abc",
            "bc abc c",
            0);

        var moved = matcher.TrySkip(3, out var skipped);
        Assert.IsTrue(moved);
        Assert.AreEqual(3, skipped);

        moved = matcher.TryNextWindow(out bool ismatch);
        Assert.IsTrue(moved);
        Assert.IsTrue(ismatch);

        moved = matcher.TrySkip(40, out skipped);
        Assert.IsTrue(moved);
        Assert.AreNotEqual(40, skipped);
        Assert.AreEqual(2, skipped);

        moved = matcher.TryNextWindow(out ismatch);
        Assert.IsFalse(moved);
        Assert.IsFalse(ismatch);

        moved = matcher.TrySkip(1, out skipped);
        Assert.IsFalse(moved);
        Assert.AreEqual(0, skipped);
    }
    #endregion

    #region LookBehind Matcher
    [TestMethod]
    public void TryNextWindow_LookBehind_Tests()
    {
        var matcher = SubstringMatcher.OfLookBehind(
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
    #endregion
}
