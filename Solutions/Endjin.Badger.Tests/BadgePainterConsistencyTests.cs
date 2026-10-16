using System.Collections.Concurrent;
using Shouldly;

namespace Endjin.Badger.Tests;

/// <summary>
/// Rendering a badge is a pure function of its arguments, so however the painter reuses work
/// between calls (the parsed templates), every call must produce exactly the SVG that the first
/// call did, including when called from many threads at once.
/// </summary>
[TestClass]
public class BadgePainterConsistencyTests
{
    [TestMethod]
    public void DrawSVG_SameBadgeRenderedRepeatedly_ReturnsIdenticalSvg()
    {
        // Arrange
        string first = BadgePainter.DrawSVG("Design &amp; Implementation Documentation", "67%", ColorScheme.YellowGreen, Style.Flat);

        // Act
        List<string> repeats = [.. Enumerable.Range(0, 100).Select(_ =>
            BadgePainter.DrawSVG("Design &amp; Implementation Documentation", "67%", ColorScheme.YellowGreen, Style.Flat))];

        // Assert
        repeats.ShouldAllBe(svg => svg == first);
    }

    [TestMethod]
    public void DrawSVG_CalledConcurrently_EveryResultEqualsTheSequentialRender()
    {
        // Arrange
        (string Subject, string Status, Style Style)[] badges =
        [
            .. Enumerable.Range(0, 50).Select(i => ($"Concurrent rule {i}", $"{i}%", (Style)(i % 3))),
        ];
        string[] expected = [.. badges.Select(b => BadgePainter.DrawSVG(b.Subject, b.Status, ColorScheme.Yellow, b.Style))];
        ConcurrentBag<int> mismatches = [];

        // Act
        Parallel.For(0, 2000, i =>
        {
            int badge = i % badges.Length;
            string svg = BadgePainter.DrawSVG(badges[badge].Subject, badges[badge].Status, ColorScheme.Yellow, badges[badge].Style);
            if (svg != expected[badge])
            {
                mismatches.Add(i);
            }
        });

        // Assert
        mismatches.ShouldBeEmpty();
    }

    [TestMethod]
    public void WarmUp_CalledBeforeAndAfterRendering_DoesNotChangeTheBadges()
    {
        // Arrange
        string before = BadgePainter.DrawSVG("IMM", "42 / 54", ColorScheme.Green, Style.Plastic);

        // Act
        BadgePainter.WarmUp();
        string after = BadgePainter.DrawSVG("IMM", "42 / 54", ColorScheme.Green, Style.Plastic);

        // Assert
        after.ShouldBe(before);
    }
}
