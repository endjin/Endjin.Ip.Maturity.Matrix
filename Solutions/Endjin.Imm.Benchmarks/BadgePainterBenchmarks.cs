using System.Net;
using BenchmarkDotNet.Attributes;
using Endjin.Badger;
using SixLabors.Fonts;

namespace Endjin.Imm.Benchmarks;

/// <summary>
/// SVG badge rendering, plus the individual steps <see cref="BadgePainter.DrawSVG"/> performs on
/// every call (font lookup, font and text options construction, text measurement, HTML decoding).
/// </summary>
[Config(typeof(ImmBenchmarkConfig))]
[BenchmarkCategory("Badge")]
public class BadgePainterBenchmarks
{
    private const string TotalSubject = "IMM";
    private const string TotalStatus = "42 / 54";
    private const string RuleStatus = "67%";

    // The Host HTML-encodes rule names before passing them to DrawSVG, which decodes them again.
    private static readonly string RuleSubject = WebUtility.HtmlEncode("Design & Implementation Documentation");

    private FontFamily family;
    private TextOptions options = null!;
    private string decodedRuleSubject = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        // Same font selection and sizing as BadgePainter.DrawSVG.
        this.family = SystemFonts.TryGet("Verdana", out FontFamily verdana) ? verdana : SystemFonts.Families.First();
        Font font = new(this.family, 11 * 0.75f, FontStyle.Regular);
        this.options = new TextOptions(font) { Dpi = 96 };
        this.decodedRuleSubject = WebUtility.HtmlDecode(RuleSubject);

        // Warm up the SixLabors.Fonts system font collection, which is built lazily on first use.
        _ = BadgePainter.DrawSVG(TotalSubject, TotalStatus, ColorScheme.Yellow, Style.Flat);
    }

    [Benchmark(Baseline = true)]
    public string DrawSVG_Total_Flat() => BadgePainter.DrawSVG(TotalSubject, TotalStatus, ColorScheme.Yellow, Style.Flat);

    [Benchmark]
    public string DrawSVG_Rule_Flat() => BadgePainter.DrawSVG(RuleSubject, RuleStatus, ColorScheme.YellowGreen, Style.Flat);

    [Benchmark]
    public string DrawSVG_Total_FlatSquare() => BadgePainter.DrawSVG(TotalSubject, TotalStatus, ColorScheme.Yellow, Style.FlatSquare);

    [Benchmark]
    public string DrawSVG_Total_Plastic() => BadgePainter.DrawSVG(TotalSubject, TotalStatus, ColorScheme.Yellow, Style.Plastic);

    [Benchmark]
    public FontFamily SystemFonts_TryGet_Verdana() => SystemFonts.TryGet("Verdana", out FontFamily verdana) ? verdana : default;

    [Benchmark]
    public TextOptions Font_And_TextOptions_Construct()
    {
        Font font = new(this.family, 11 * 0.75f, FontStyle.Regular);
        return new TextOptions(font) { Dpi = 96 };
    }

    [Benchmark]
    public float MeasureAdvance_Short() => TextMeasurer.MeasureAdvance(TotalSubject, this.options).Width;

    [Benchmark]
    public float MeasureAdvance_Long() => TextMeasurer.MeasureAdvance(this.decodedRuleSubject, this.options).Width;

    [Benchmark]
    public string HtmlDecode_RuleSubject() => WebUtility.HtmlDecode(RuleSubject);
}
