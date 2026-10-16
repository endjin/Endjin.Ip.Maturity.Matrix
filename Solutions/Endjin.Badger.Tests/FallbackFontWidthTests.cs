using System.Globalization;
using Endjin.Badger.Tests.TestData;
using Shouldly;

namespace Endjin.Badger.Tests;

/// <summary>
/// Badges are sized for Verdana, the first font in the templates' font-family list, and each text is
/// given that width as its <c>textLength</c>, so a browser that draws the text in another font spaces it
/// out (or squeezes it) to fill the same width. The further the other font's natural width is from
/// Verdana's, the more visibly the spacing changes; beyond about 8% it starts to look wrong. These tests
/// measure every text badges show with the fonts browsers fall back to when Verdana is not installed:
/// DejaVu Sans (named in the font-family list; most Linux desktops) and Liberation Sans (metric-compatible
/// with Arial and Helvetica, which the generic sans-serif usually resolves to). Their tables were
/// generated from the fonts by tools/GenerateFontMetrics.cs, as test evidence only; they are not shipped.
/// </summary>
[TestClass]
public class FallbackFontWidthTests
{
    private const double MaximumDifference = 0.08;

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void MeasureWidth_EveryBadgeTextInDejaVuSans_IsWithinEightPercentOfVerdana()
    {
        // Act
        (string Text, double Ratio)[] ratios = this.RatiosToVerdana(
            DejaVuSansMetrics.FontVersion,
            text => FontMetrics<DejaVuSansMetrics>.MeasureWidth(text, VerdanaMetrics.PixelSize));

        // Assert
        ratios.Where(r => Math.Abs(r.Ratio - 1) > MaximumDifference).Select(Describe)
            .ShouldBeEmpty($"DejaVu Sans widths more than {MaximumDifference:P0} from Verdana's");
    }

    /// <summary>
    /// Liberation Sans, and so Arial and Helvetica, is much narrower than Verdana: every badge text is
    /// between about 7% and 20% narrower (digits, spaces and slashes most of all), so a browser with none
    /// of Verdana, Geneva or DejaVu Sans spaces the text out noticeably to fill the badge. This is outside
    /// the 8% the badges aim for. The text still fits its segment, because it is never wider than Verdana;
    /// this test records how far off it is, so that any change is noticed.
    /// </summary>
    [TestMethod]
    public void MeasureWidth_EveryBadgeTextInLiberationSans_IsNarrowerThanVerdanaByUpToTwentyPercent()
    {
        // Act
        (string Text, double Ratio)[] ratios = this.RatiosToVerdana(
            LiberationSansMetrics.FontVersion,
            text => FontMetrics<LiberationSansMetrics>.MeasureWidth(text, VerdanaMetrics.PixelSize));

        // Assert
        ratios.Where(r => r.Ratio is < 0.80 or > 1).Select(Describe)
            .ShouldBeEmpty("Liberation Sans widths more than 20% narrower than, or wider than, Verdana's");
    }

    private static string Describe((string Text, double Ratio) ratio) =>
        string.Create(CultureInfo.InvariantCulture, $"'{ratio.Text}' {ratio.Ratio:F4}");

    private (string Text, double Ratio)[] RatiosToVerdana(string fontVersion, Func<string, float> measure)
    {
        (string Text, double Ratio)[] ratios =
        [
            .. BadgeTexts.All.Distinct().Select(text => (text, (double)measure(text) / VerdanaMetrics.MeasureWidth(text))),
        ];
        (string Text, double Ratio) narrowest = ratios.MinBy(r => r.Ratio);
        (string Text, double Ratio) widest = ratios.MaxBy(r => r.Ratio);
        int outside = ratios.Count(r => Math.Abs(r.Ratio - 1) > MaximumDifference);
        this.TestContext.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"{fontVersion}: {ratios.Length} texts; width / Verdana's from {narrowest.Ratio:F4} ('{narrowest.Text}') to {widest.Ratio:F4} ('{widest.Text}'), mean {ratios.Average(r => r.Ratio):F4}; {outside} more than {MaximumDifference:P0} from Verdana"));
        foreach ((string text, double ratio) in ratios.OrderBy(r => r.Ratio))
        {
            this.TestContext.WriteLine(Describe((text, ratio)));
        }

        return ratios;
    }
}
