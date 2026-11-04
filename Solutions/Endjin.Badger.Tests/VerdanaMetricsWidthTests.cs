using Shouldly;

namespace Endjin.Badger.Tests;

/// <summary>
/// <see cref="VerdanaMetrics"/> must give the widths that measuring with Verdana itself gives, the
/// way BadgePainter measured text before the table existed. The expected widths were measured once
/// with Verdana 5.33 (Windows 11), using SixLabors.Fonts at 8.25pt and 96 DPI with the default
/// kerning, as <c>tools/GenerateFontMetrics.cs</c> does; Verdana may not be redistributed, so it
/// cannot be measured here on every platform. To check a regenerated table against the font itself,
/// run <c>tools/Compare-FontWidths.cs</c>.
/// </summary>
[TestClass]
public class VerdanaMetricsWidthTests
{
    // The table is exact; SixLabors.Fonts accumulates float rounding error of around 0.00005px.
    private const float Tolerance = 0.02f;

    [TestMethod]

    // Rule names in the benchmark rule set.
    [DataRow("Shared Engineering Standards", 168.46436f)]
    [DataRow("Coding Standards", 98.07081f)]
    [DataRow("Executable Specifications", 139.4336f)]
    [DataRow("Code Coverage", 83.38623f)]
    [DataRow("Benchmarks", 67.987305f)]
    [DataRow("Reference Documentation", 142.6992f)]
    [DataRow("Design & Implementation Documentation", 229.07181f)]
    [DataRow("How-to Documentation", 127.697754f)]
    [DataRow("Date of Last IP Review", 125.055176f)]
    [DataRow("Framework Version", 106.04688f)]
    [DataRow("Associated Work Items", 126.65577f)]
    [DataRow("Source Code Availability", 133.12791f)]
    [DataRow("License", 40.669926f)]
    [DataRow("Production Use", 82.74169f)]
    [DataRow("Insights", 44.220215f)]
    [DataRow("Packaging", 55.504883f)]
    [DataRow("Deployment", 66.55322f)]
    [DataRow("OpenChain", 60.25293f)]

    // Percentages and totals.
    [DataRow("0%", 18.831055f)]
    [DataRow("9%", 18.831055f)]
    [DataRow("10%", 25.824219f)]
    [DataRow("67%", 25.824219f)]
    [DataRow("100%", 32.817383f)]
    [DataRow("0 / 0", 26.71582f)]
    [DataRow("1 / 3", 26.71582f)]
    [DataRow("3 / 46", 33.708984f)]
    [DataRow("8 / 18", 33.708984f)]
    [DataRow("22 / 46", 40.70215f)]
    [DataRow("42 / 54", 40.70215f)]
    [DataRow("46 / 46", 40.70215f)]
    [DataRow("99 / 100", 47.695312f)]
    [DataRow("121 / 300", 54.688477f)]
    [DataRow("100 / 100", 54.688477f)]

    // Subjects and the texts of error badges.
    [DataRow("IMM", 23.1709f)]
    [DataRow("IMM rule", 48.264645f)]
    [DataRow("imm.yaml not found", 112.0625f)]
    [DataRow("rule set not found", 98.73682f)]
    [DataRow("unavailable", 62.41211f)]
    [DataRow("invalid imm.yaml", 94.568855f)]
    [DataRow("unknown rule", 75.12549f)]
    [DataRow("invalid rule id", 74.49708f)]
    [DataRow("rule not found", 78.25147f)]
    [DataRow("opted out", 53.109383f)]

    // Kerning, accented and HTML-significant characters, and whitespace.
    [DataRow("<Ünïcødé> \"rule\"", 97.03956f)]
    [DataRow("R&D <Tools>", 73.686035f)]
    [DataRow("AVAWAY To Ty", 78.33204f)]
    [DataRow("Wave Typography: “Quotes” – L'Avenue", 218.80225f)]
    [DataRow("Ångström Œuvre Łódź", 123.17529f)]
    [DataRow("Rule with trailing space ", 128.77737f)]
    [DataRow(" Rule with leading space", 134.19678f)]
    [DataRow("No break ", 53.855957f)]
    [DataRow("été", 17.439941f)]
    [DataRow("   ", 3.8671875f)]
    [DataRow("", 0f)]
    public void MeasureWidth_BadgeText_MatchesTheWidthMeasuredWithVerdana(string text, float widthMeasuredWithVerdana)
    {
        // Act
        float width = VerdanaMetrics.MeasureWidth(text);

        // Assert
        width.ShouldBe(widthMeasuredWithVerdana, Tolerance);
    }
}
