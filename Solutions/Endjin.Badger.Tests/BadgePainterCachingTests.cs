using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using SixLabors.Fonts;
using Shouldly;

namespace Endjin.Badger.Tests;

/// <summary>
/// Rendering a badge is a pure function of its arguments and the installed fonts, so however the
/// painter reuses work between calls (the font, the measured text widths, the parsed templates),
/// every call must produce exactly the SVG that rendering the badge from scratch would, including
/// when called from many threads at once and when more distinct texts have been rendered than the
/// painter remembers.
/// </summary>
[TestClass]
public class BadgePainterCachingTests
{
    [TestMethod]
    [DataRow("IMM", "42 / 54", Style.Flat)]
    [DataRow("IMM", "42 / 54", Style.FlatSquare)]
    [DataRow("IMM", "42 / 54", Style.Plastic)]
    [DataRow("Design &amp; Implementation Documentation", "67%", Style.Flat)]
    [DataRow("Shared Engineering Standards", "100%", Style.Plastic)]
    [DataRow("&lt;Ünïcødé&gt; &quot;rule&quot;", "0%", Style.FlatSquare)]
    public void DrawSVG_AnyBadge_IsByteForByteTheSvgRenderedFromScratch(string subject, string status, Style style)
    {
        // Act
        string svg = BadgePainter.DrawSVG(subject, status, ColorScheme.YellowGreen, style);

        // Assert
        svg.ShouldBe(RenderFromScratch(subject, status, ColorScheme.YellowGreen, style));
    }

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
        string[] expected = [.. badges.Select(b => RenderFromScratch(b.Subject, b.Status, ColorScheme.Yellow, b.Style))];
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
    public void DrawSVG_MoreDistinctTextsThanTheMeasurementCacheHolds_StillRendersEarlierBadgesIdentically()
    {
        // Arrange
        string earlier = BadgePainter.DrawSVG("Rule rendered before the cache fills", "50%", ColorScheme.Yellow, Style.Flat);

        // Act
        for (int i = 0; i < (BadgePainter.MaxCachedMeasurements * 2) + 1; i++)
        {
            _ = BadgePainter.DrawSVG($"Overflow subject {i}", $"{i} / {i + 1}", ColorScheme.Red, Style.Flat);
        }

        string again = BadgePainter.DrawSVG("Rule rendered before the cache fills", "50%", ColorScheme.Yellow, Style.Flat);

        // Assert
        again.ShouldBe(earlier);
        again.ShouldBe(RenderFromScratch("Rule rendered before the cache fills", "50%", ColorScheme.Yellow, Style.Flat));
    }

    // Renders a badge the way BadgePainter did before it reused any work between calls: the font is
    // looked up, both texts measured and the template formatted on every call.
    private static string RenderFromScratch(string subject, string status, string statusColor, Style style)
    {
        string template = style switch
        {
            Style.Flat => Resources.flat,
            Style.FlatSquare => Resources.flatSquare,
            Style.Plastic => Resources.plastic,
            _ => Resources.flat,
        };

        FontFamily family = SystemFonts.TryGet("Verdana", out FontFamily verdana) ? verdana : SystemFonts.Families.First();
        Font font = new(family, 11 * 0.75f, FontStyle.Regular);
        TextOptions options = new(font) { Dpi = 96 };

        float subjectWidth = TextMeasurer.MeasureAdvance(WebUtility.HtmlDecode(subject), options).Width + 8;
        float statusWidth = TextMeasurer.MeasureAdvance(WebUtility.HtmlDecode(status), options).Width + 8;

        return string.Format(
            CultureInfo.InvariantCulture,
            template,
            subjectWidth + statusWidth,
            subjectWidth,
            statusWidth,
            subjectWidth / 2,
            subjectWidth + (statusWidth / 2),
            subject,
            status,
            statusColor);
    }
}
