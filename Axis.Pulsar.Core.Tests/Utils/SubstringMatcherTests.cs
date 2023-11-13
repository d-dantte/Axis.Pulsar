using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Tests;


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
