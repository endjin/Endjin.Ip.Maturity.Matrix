using SixLabors.Fonts;
using SixLabors.Fonts.WellKnownIds;
using Shouldly;

namespace Endjin.Badger.Tests;

/// <summary>
/// <see cref="VerdanaMetrics"/> must give the widths that measuring with Verdana itself gives, the
/// way BadgePainter measured text before the table existed (SixLabors.Fonts, 8.25pt at 96 DPI, with
/// the default kerning). Verdana may not be redistributed, so these tests run only on Windows, where
/// it is installed, and load it from there into a private font collection.
/// </summary>
[TestClass]
[OSCondition(OperatingSystems.Windows)]
public class VerdanaMetricsOracleTests
{
    // The table is exact; SixLabors.Fonts accumulates float rounding error of around 0.00005px.
    private const float Tolerance = 0.02f;

    private static readonly string VerdanaPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "verdana.ttf");

    private static readonly Lazy<(TextOptions Options, string Version)> Verdana = new(LoadVerdana);

    /// <summary>
    /// Gets texts badges show: every rule name in the benchmark rule set
    /// (Solutions/Endjin.Imm.Benchmarks/Data/RuleSet.yaml), totals and percentages, and texts that
    /// exercise kerning, accented and HTML-significant characters, and whitespace.
    /// </summary>
    public static IEnumerable<object[]> Corpus =>
    [
        .. RuleNames.Select(name => new object[] { name }),
        .. Enumerable.Range(0, 101).Select(percent => new object[] { $"{percent}%" }),
        ["IMM"],
        ["42 / 54"],
        ["121 / 300"],
        ["<Ünïcødé> \"rule\""],
        ["R&D <Tools>"],
        ["AVAWAY To Ty"],
        ["Wave Typography: “Quotes” – L'Avenue"],
        ["Ångström Œuvre Łódź"],
        ["Rule with trailing space "],
        [" Rule with leading space"],
        ["No break "],
        ["été"],
        ["   "],
        [string.Empty],
    ];

    private static string[] RuleNames =>
    [
        "Shared Engineering Standards",
        "Coding Standards",
        "Executable Specifications",
        "Code Coverage",
        "Benchmarks",
        "Reference Documentation",
        "Design & Implementation Documentation",
        "How-to Documentation",
        "Date of Last IP Review",
        "Framework Version",
        "Associated Work Items",
        "Source Code Availability",
        "License",
        "Production Use",
        "Insights",
        "Packaging",
        "Deployment",
        "OpenChain",
    ];

    [TestMethod]
    [DynamicData(nameof(Corpus))]
    public void MeasureWidth_AnyBadgeText_MatchesMeasuringWithVerdana(string text)
    {
        // Arrange
        (TextOptions options, string version) = Verdana.Value;
        float expected = TextMeasurer.MeasureAdvance(text, options).Width;

        // Act
        float width = VerdanaMetrics.MeasureWidth(text);

        // Assert
        width.ShouldBe(
            expected,
            Tolerance,
            $"Measured with {version} from {VerdanaPath}; the table was generated from {VerdanaMetrics.FontVersion}.");
    }

    private static (TextOptions Options, string Version) LoadVerdana()
    {
        if (!File.Exists(VerdanaPath))
        {
            Assert.Inconclusive($"Verdana is not installed at {VerdanaPath}.");
        }

        FontCollection collection = new();
        FontFamily family = collection.Add(VerdanaPath, out FontDescription description);
        string version = $"{description.FontNameInvariantCulture} {description.GetNameById(System.Globalization.CultureInfo.InvariantCulture, KnownNameIds.Version)}";
        Font font = new(family, 11 * 0.75f, FontStyle.Regular);
        return (new TextOptions(font) { Dpi = 96 }, version);
    }
}
