using System.Net;
using BenchmarkDotNet.Attributes;
using Endjin.Badger;

namespace Endjin.Imm.Benchmarks;

/// <summary>
/// SVG badge rendering, plus the individual steps <see cref="BadgePainter.DrawSVG"/> performs (text
/// measurement with the precomputed Verdana metrics, HTML decoding).
/// </summary>
/// <remarks>
/// <see cref="BadgePainter.DrawSVG"/> loads no fonts and caches nothing but its parsed templates, so
/// every call costs the same as a first call after <see cref="BadgePainter.WarmUp"/>.
/// </remarks>
[Config(typeof(ImmBenchmarkConfig))]
[BenchmarkCategory("Badge")]
public class BadgePainterBenchmarks
{
    private const string TotalSubject = "IMM";
    private const string TotalStatus = "42 / 54";
    private const string RuleStatus = "67%";

    // The Host HTML-encodes rule names before passing them to DrawSVG, which decodes them again.
    private static readonly string RuleSubject = WebUtility.HtmlEncode("Design & Implementation Documentation");

    private string decodedRuleSubject = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        this.decodedRuleSubject = WebUtility.HtmlDecode(RuleSubject);
        BadgePainter.WarmUp();
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
    public float VerdanaMetrics_Measure_Short() => VerdanaMetrics.MeasureWidth(TotalSubject);

    [Benchmark]
    public float VerdanaMetrics_Measure_Long() => VerdanaMetrics.MeasureWidth(this.decodedRuleSubject);

    [Benchmark]
    public string HtmlDecode_RuleSubject() => WebUtility.HtmlDecode(RuleSubject);
}
