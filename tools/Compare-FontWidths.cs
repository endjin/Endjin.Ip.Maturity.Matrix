#:package SixLabors.Fonts@2.1.3
#:property PublishAot=false
#:project ../Solutions/Endjin.Badger/Endjin.Badger.csproj

// Compares the badge text widths Endjin.Badger computes from its Verdana table with measurements of the
// real fonts, to show how close the table is to Verdana, how much kerning matters, and how far off
// DejaVu Sans (first in the badge templates' font-family list, and what many Linux browsers will use)
// would be.
//
// Usage (Windows):
//   dotnet run tools/Compare-FontWidths.cs -- --verdana C:\Windows\Fonts\verdana.ttf --dejavu <path to DejaVuSans.ttf>
//
// Download DejaVu Sans 2.37 (https://github.com/dejavu-fonts/dejavu-fonts/releases) into an empty directory
// outside the repository. Never commit either font.
//
// Columns, all at 11px (8.25pt at 96 DPI) from private font collections:
//   V_std  Verdana with SixLabors.Fonts' default kerning (what BadgePainter measured before the table)
//   T      Endjin.Badger's table (read back from the subject segment of a rendered badge)
//   V_none Verdana with kerning turned off
//   D_std  DejaVu Sans with default kerning
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Endjin.Badger;
using SixLabors.Fonts;
using SixLabors.Fonts.WellKnownIds;

string? verdanaPath = null;
string? dejaVuPath = null;
for (int i = 0; i < args.Length - 1; i++)
{
    switch (args[i])
    {
        case "--verdana": verdanaPath = args[++i]; break;
        case "--dejavu": dejaVuPath = args[++i]; break;
    }
}

if (verdanaPath is null || dejaVuPath is null)
{
    Console.Error.WriteLine("Usage: dotnet run tools/Compare-FontWidths.cs -- --verdana <verdana.ttf> --dejavu <DejaVuSans.ttf>");
    return 1;
}

(TextOptions verdana, string verdanaVersion) = Load(verdanaPath, KerningMode.Standard);
(TextOptions verdanaNoKerning, _) = Load(verdanaPath, KerningMode.None);
(TextOptions dejaVu, string dejaVuVersion) = Load(dejaVuPath, KerningMode.Standard);

const string Emoji = "Rule \U0001F600";
string[] corpus =
[
    "Shared Engineering Standards", "Coding Standards", "Executable Specifications", "Code Coverage", "Benchmarks",
    "Reference Documentation", "Design & Implementation Documentation", "How-to Documentation", "Date of Last IP Review",
    "Framework Version", "Associated Work Items", "Source Code Availability", "License", "Production Use", "Insights",
    "Packaging", "Deployment", "OpenChain",
    "IMM", .. Enumerable.Range(0, 11).Select(i => $"{i * 10}%"), "42 / 54", "121 / 300",
    "<Ünïcødé> \"rule\"", "R&D <Tools>", "AVAWAY To Ty", "Wave Typography: \u201CQuotes\u201D \u2013 L'Avenue",
    Emoji,
];

Console.WriteLine($"Verdana: {verdanaVersion}; DejaVu: {dejaVuVersion}; table: {VerdanaMetrics()}");
Console.WriteLine();
Console.WriteLine("| Text | V_std px | T−V_std px | V_none−V_std px | D_std−V_std px | D_std−V_std % |");
Console.WriteLine("|---|---:|---:|---:|---:|---:|");

List<(double Table, double NoKerning, double DejaVu, double DejaVuPercent)> differences = [];
foreach (string text in corpus)
{
    double vStd = TextMeasurer.MeasureAdvance(text, verdana).Width;
    double vNone = TextMeasurer.MeasureAdvance(text, verdanaNoKerning).Width;
    double dStd = TextMeasurer.MeasureAdvance(text, dejaVu).Width;
    double table = TableWidth(text);
    double percent = vStd == 0 ? 0 : (dStd - vStd) / vStd * 100;
    string note = text == Emoji ? " (fallback; excluded from summary)" : string.Empty;
    Console.WriteLine(string.Create(
        CultureInfo.InvariantCulture,
        $"| `{text.Replace("|", "\\|")}`{note} | {vStd:F4} | {table - vStd:+0.00000;-0.00000;0} | {vNone - vStd:+0.000;-0.000;0} | {dStd - vStd:+0.000;-0.000;0} | {percent:+0.0;-0.0;0}% |"));
    if (text != Emoji)
    {
        differences.Add((table - vStd, vNone - vStd, dStd - vStd, percent));
    }
}

Console.WriteLine();
Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"max |T − V_std|      = {differences.Max(d => Math.Abs(d.Table)):G4} px"));
Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"max |V_none − V_std| = {differences.Max(d => Math.Abs(d.NoKerning)):F3} px (kerning contribution); mean {differences.Average(d => d.NoKerning):F3} px"));
Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"D_std − V_std        = mean {differences.Average(d => d.DejaVu):+0.000;-0.000} px ({differences.Average(d => d.DejaVuPercent):+0.0;-0.0}%), mean |…| {differences.Average(d => Math.Abs(d.DejaVu)):F3} px ({differences.Average(d => Math.Abs(d.DejaVuPercent)):F1}%), max |…| {differences.Max(d => Math.Abs(d.DejaVu)):F3} px / {differences.Max(d => Math.Abs(d.DejaVuPercent)):F1}%"));
return 0;

static (TextOptions Options, string Version) Load(string path, KerningMode kerning)
{
    FontCollection collection = new();
    FontFamily family = collection.Add(path, out FontDescription description);
    Font font = new(family, 11 * 0.75f, FontStyle.Regular);
    string version = $"{description.FontNameInvariantCulture} {description.GetNameById(CultureInfo.InvariantCulture, KnownNameIds.Version)}";
    return (new TextOptions(font) { Dpi = 96, KerningMode = kerning }, version);
}

// The table is internal to Endjin.Badger, so read its width back from a rendered badge: the subject
// segment is the measured width plus 8px of padding.
static double TableWidth(string text)
{
    string svg = BadgePainter.DrawSVG(WebUtility.HtmlEncode(text), "0%", ColorScheme.Green, Style.Flat);
    Match match = Regex.Match(svg, "<path fill=\"#555\" d=\"M0 0h(?<w>[0-9.]+)v");
    return double.Parse(match.Groups["w"].Value, CultureInfo.InvariantCulture) - 8;
}

static string VerdanaMetrics() =>
    typeof(BadgePainter).Assembly.GetType("Endjin.Badger.VerdanaMetrics")!
        .GetField("FontVersion", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
        .GetRawConstantValue()!.ToString()!;
