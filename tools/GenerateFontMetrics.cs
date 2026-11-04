#:package SixLabors.Fonts@2.1.3
#:property PublishAot=false

// Generates a table of a font's advance widths and kerning at 11px (an IFontMetricsTable, which
// Endjin.Badger's FontMetrics<TTable> measures text with), so that text can be sized without the font
// installed.
//
// Usage:
//   dotnet run tools/GenerateFontMetrics.cs -- --font <font.ttf> --out <File.g.cs> --class <TypeName> --namespace <Namespace>
//       [--licence <licence name>] [--source <where the font came from>]
//
// The shipped table, which BadgePainter sizes badges with (Windows, where Verdana is installed):
//   dotnet run tools/GenerateFontMetrics.cs -- --font C:\Windows\Fonts\verdana.ttf --out Solutions/Endjin.Badger/VerdanaMetrics.g.cs --class VerdanaMetrics --namespace Endjin.Badger --licence "Microsoft (not redistributable)" --source "C:\Windows\Fonts\verdana.ttf (Windows 11)"
//
// Test-only tables for the fonts a browser falls back to when Verdana is not installed, which
// Endjin.Badger.Tests uses to check how far their widths are from Verdana's (download each release into
// a new, empty directory outside the repository):
//   dotnet run tools/GenerateFontMetrics.cs -- --font <path to DejaVuSans.ttf> --out Solutions/Endjin.Badger.Tests/DejaVuSansMetrics.g.cs --class DejaVuSansMetrics --namespace Endjin.Badger.Tests --licence "Bitstream Vera Fonts licence (DejaVu changes public domain)" --source https://github.com/dejavu-fonts/dejavu-fonts/releases/download/version_2_37/dejavu-sans-ttf-2.37.zip
//   dotnet run tools/GenerateFontMetrics.cs -- --font <path to LiberationSans-Regular.ttf> --out Solutions/Endjin.Badger.Tests/LiberationSansMetrics.g.cs --class LiberationSansMetrics --namespace Endjin.Badger.Tests --licence "SIL Open Font License 1.1" --source https://github.com/liberationfonts/liberation-fonts/files/7261482/liberation-fonts-ttf-2.1.5.tar.gz
//
// Only numbers derived from the font are written out. Never commit a font file: the Verdana licence
// does not allow the font to be redistributed, and the others have no reason to be in the repository.
//
// Every number is measured with SixLabors.Fonts exactly as BadgePainter measured text before the tables
// existed (8.25pt at 96 DPI, i.e. 11px, with the default kerning), from a private FontCollection so that
// the result never depends on which fonts happen to be installed.
using System.Globalization;
using System.Numerics;
using System.Text;
using SixLabors.Fonts;
using SixLabors.Fonts.Unicode;
using SixLabors.Fonts.WellKnownIds;

string? fontPath = null;
string? outPath = null;
string? className = null;
string? namespaceName = null;
string? licence = null;
string? source = null;
for (int i = 0; i < args.Length - 1; i++)
{
    switch (args[i])
    {
        case "--font": fontPath = args[++i]; break;
        case "--out": outPath = args[++i]; break;
        case "--class": className = args[++i]; break;
        case "--namespace": namespaceName = args[++i]; break;
        case "--licence": licence = args[++i]; break;
        case "--source": source = args[++i]; break;
    }
}

if (fontPath is null || outPath is null || className is null || namespaceName is null)
{
    Console.Error.WriteLine(
        "Usage: dotnet run tools/GenerateFontMetrics.cs -- --font <font.ttf> --out <File.g.cs> --class <TypeName> --namespace <Namespace> [--licence <licence name>] [--source <where the font came from>]");
    return 1;
}

const float PixelSize = 11f;

// Must match FontMetrics.LatinFirst and FontMetrics.LatinLast in Endjin.Badger.
const int LatinFirst = 0x20;
const int LatinLast = 0x24F;

FontCollection collection = new();
FontFamily family = collection.Add(fontPath, out FontDescription description);
Font font = new(family, PixelSize * 0.75f, FontStyle.Regular);
TextOptions options = new(font) { Dpi = 96 };

int unitsPerEm = font.FontMetrics.UnitsPerEm;
if (!BitOperations.IsPow2(unitsPerEm))
{
    // Widths at 11px are exact multiples of 11/unitsPerEm px only when unitsPerEm is a power of two;
    // otherwise rounding to the nearest unit below may be off by one unit.
    Console.Error.WriteLine($"Warning: {unitsPerEm} units per em is not a power of two, so advances may be off by a unit.");
}

string version = description.GetNameById(CultureInfo.InvariantCulture, KnownNameIds.Version);
string fontName = description.FontNameInvariantCulture;
HashSet<int> available = [.. font.FontMetrics.GetAvailableCodePoints().Select(cp => cp.Value)];

// The advance of one character, in font units. Measured widths at 11px are exact multiples of
// 11/unitsPerEm px, so rounding only removes float noise.
int MeasureUnits(string text) =>
    (int)Math.Round(TextMeasurer.MeasureAdvance(text, options).Width * unitsPerEm / PixelSize, MidpointRounding.AwayFromZero);

static bool IsCombiningMark(int cp) => cp is >= 0x300 and <= 0x36F;

// Advances for U+0020..U+024F (0 = not in the font).
int[] latin = [.. Enumerable.Range(LatinFirst, LatinLast - LatinFirst + 1)];
ushort[] advances = new ushort[latin.Length];
List<int> zeroWidth = [];
foreach (int cp in latin)
{
    if (!available.Contains(cp) || char.IsControl((char)cp))
    {
        continue;
    }

    int units = MeasureUnits(char.ConvertFromUtf32(cp));
    if (units <= 0)
    {
        zeroWidth.Add(cp);
        continue;
    }

    advances[cp - LatinFirst] = checked((ushort)units);
}

// Punctuation and symbols outside the Latin range that rule names plausibly contain.
int[] extraCandidates = [.. Enumerable.Range(0x2010, 0x203A - 0x2010 + 1), 0x20AC, 0x2122];
List<(int CodePoint, int Units)> extras = [];
foreach (int cp in extraCandidates)
{
    if (available.Contains(cp))
    {
        int units = MeasureUnits(char.ConvertFromUtf32(cp));
        if (units > 0)
        {
            extras.Add((cp, units));
        }
    }
}

int[] kernable = [.. latin.Where(cp => advances[cp - LatinFirst] != 0 && !IsCombiningMark(cp))];

// SixLabors.Fonts leaves breaking whitespace at the end of a text out of its advance (it would hang
// past the end of the line), so "a " measures the same as "a". FontMetrics mirrors that with
// FontMetrics.IsTrailingSpaceIgnored; check that rule agrees with every character the table covers.
int aUnits = advances['a' - LatinFirst];
foreach (int cp in kernable.Concat(extras.Select(e => e.CodePoint)))
{
    bool ignored = MeasureUnits("a" + char.ConvertFromUtf32(cp)) == aUnits;
    if (ignored != IsTrailingSpaceIgnored(cp))
    {
        Console.Error.WriteLine($"U+{cp:X4}: SixLabors.Fonts {(ignored ? "ignores" : "counts")} it at the end of a text, which IsTrailingSpaceIgnored does not predict.");
        return 1;
    }
}

// Kerning, derived empirically: the width of each pair of Latin characters less the widths of the two
// characters on their own. This captures whatever kerning SixLabors.Fonts applies (the font's kern or
// GPOS table), as it applied it to badges before. A pair ending in ignored trailing whitespace is
// measured with '|' after it, and the (already known) advance of '|' and kerning of the whitespace
// against '|' taken off again.
const char Sentinel = '|';
int sentinelUnits = advances[Sentinel - LatinFirst];
var pairs = new System.Collections.Concurrent.ConcurrentBag<(int Key, short Units)>();
Parallel.ForEach(kernable, left =>
{
    string leftText = char.ConvertFromUtf32(left);
    int leftUnits = advances[left - LatinFirst];
    foreach (int right in kernable)
    {
        string rightText = char.ConvertFromUtf32(right);
        int rightUnits = advances[right - LatinFirst];
        int kern = IsTrailingSpaceIgnored(right)
            ? MeasureUnits(leftText + rightText + Sentinel) - leftUnits - rightUnits - sentinelUnits
                - (MeasureUnits(rightText + Sentinel) - rightUnits - sentinelUnits)
            : MeasureUnits(leftText + rightText) - leftUnits - rightUnits;
        if (kern != 0)
        {
            pairs.Add(((left << 16) | right, checked((short)kern)));
        }
    }
});
(int Key, short Units)[] kerning = [.. pairs.OrderBy(p => p.Key)];

// Characters the tables do not cover are given the mean width of a lower-case letter.
int fallback = (int)Math.Round(Enumerable.Range('a', 26).Average(cp => advances[cp - LatinFirst]), MidpointRounding.AwayFromZero);

StringBuilder code = new();
code.AppendLine("// <auto-generated>");
code.AppendLine($"// Generated by tools/GenerateFontMetrics.cs from {fontName} \"{version}\" on {DateTime.UtcNow:yyyy-MM-dd}; do not edit.");
if (licence is not null)
{
    code.AppendLine($"// Font licence: {licence}");
}

if (source is not null)
{
    code.AppendLine($"// Font source: {source}");
}

code.AppendLine("// Regenerate with:");
code.AppendLine($"//   dotnet run tools/GenerateFontMetrics.cs -- {RegenerateArguments()}");
code.AppendLine("// These are metrics derived from the font, not the font itself.");
code.AppendLine("// </auto-generated>");
code.AppendLine($"namespace {namespaceName}");
code.AppendLine("{");
string table = "IFontMetricsTable";
if (namespaceName != "Endjin.Badger")
{
    code.AppendLine("    using Endjin.Badger;");
    code.AppendLine();
}

code.AppendLine($"    internal readonly partial struct {className} : {table}");
code.AppendLine("    {");
code.AppendLine("        /// <summary>The name and version string of the font the metrics were measured from.</summary>");
code.AppendLine($"        internal const string FontVersion = \"{fontName} {version}\";");
code.AppendLine();
code.AppendLine("        /// <summary>Font units per em.</summary>");
code.AppendLine($"        internal const int UnitsPerEm = {unitsPerEm};");
code.AppendLine();
code.AppendLine("        /// <summary>");
code.AppendLine("        /// The advance, in font units, of a character the tables do not cover: the mean advance of the");
code.AppendLine("        /// lower-case letters a to z, rounded to the nearest unit.");
code.AppendLine("        /// </summary>");
code.AppendLine($"        internal const int FallbackAdvanceUnits = {fallback};");
code.AppendLine();
code.AppendLine($"        static int {table}.UnitsPerEm => UnitsPerEm;");
code.AppendLine();
code.AppendLine($"        static int {table}.FallbackAdvanceUnits => FallbackAdvanceUnits;");
code.AppendLine();
code.AppendLine($"        // Advances in font units for U+{LatinFirst:X4}..U+{LatinLast:X4}, indexed by code point - FontMetrics.LatinFirst (0 = not in the font).");
code.AppendLine($"        static ReadOnlySpan<ushort> {table}.LatinAdvances =>");
code.AppendLine("        [");
for (int row = 0; row < advances.Length; row += 16)
{
    IEnumerable<ushort> slice = advances.Skip(row).Take(16);
    code.AppendLine($"            {string.Join(", ", slice.Select(a => a.ToString(CultureInfo.InvariantCulture).PadLeft(4)))}, // U+{row + LatinFirst:X4}");
}

code.AppendLine("        ];");
code.AppendLine();
code.AppendLine("        // Code points outside the Latin range that the font covers, in ascending order, and their advances.");
code.AppendLine($"        static ReadOnlySpan<int> {table}.ExtraCodePoints =>");
code.AppendLine("        [");
foreach ((int cp, int _) in extras)
{
    code.AppendLine($"            0x{cp:X4},");
}

code.AppendLine("        ];");
code.AppendLine();
code.AppendLine($"        static ReadOnlySpan<ushort> {table}.ExtraAdvances =>");
code.AppendLine("        [");
foreach ((int cp, int units) in extras)
{
    code.AppendLine($"            {units,4}, // U+{cp:X4} {char.ConvertFromUtf32(cp)}");
}

code.AppendLine("        ];");
code.AppendLine();
code.AppendLine("        // Kerning pairs between Latin characters, keyed by (left << 16) | right, in ascending order.");
code.AppendLine($"        static ReadOnlySpan<int> {table}.KerningPairs =>");
code.AppendLine("        [");
foreach ((int key, short _) in kerning)
{
    code.AppendLine($"            0x{key:X8}, // {Describe(key >> 16)}{Describe(key & 0xFFFF)}");
}

code.AppendLine("        ];");
code.AppendLine();
code.AppendLine("        // The kerning of each pair in KerningPairs, in font units.");
code.AppendLine($"        static ReadOnlySpan<short> {table}.KerningUnits =>");
code.AppendLine("        [");
for (int row = 0; row < kerning.Length; row += 16)
{
    code.AppendLine($"            {string.Join(", ", kerning.Skip(row).Take(16).Select(k => k.Units.ToString(CultureInfo.InvariantCulture).PadLeft(4)))},");
}

code.AppendLine("        ];");
code.AppendLine("    }");
code.AppendLine("}");

File.WriteAllText(outPath, code.ToString().ReplaceLineEndings("\r\n"), new UTF8Encoding(true));

Console.WriteLine($"Font: {fontName} {version}; {unitsPerEm} units per em");
Console.WriteLine($"Latin advances: {advances.Count(a => a != 0)} of {advances.Length}; extras: {extras.Count}; kerning pairs: {kerning.Length}; fallback: {fallback} units");
Console.WriteLine($"Spot check: a={advances['a' - LatinFirst]} M={advances['M' - LatinFirst]} space={advances[' ' - LatinFirst]}");
if (zeroWidth.Count > 0)
{
    Console.WriteLine($"Zero-width (left to the fallback/combining rule): {string.Join(" ", zeroWidth.Select(cp => $"U+{cp:X4}"))}");
}

return 0;

// The command line that regenerates the file, with the font's location left for the reader to fill
// in unless it is a font installed with Windows.
string RegenerateArguments()
{
    string font = fontPath.StartsWith(@"C:\Windows\Fonts\", StringComparison.OrdinalIgnoreCase)
        ? fontPath
        : $"<path to {Path.GetFileName(fontPath)}>";
    List<string> parts = ["--font", font, "--out", outPath.Replace('\\', '/'), "--class", className, "--namespace", namespaceName];
    if (licence is not null)
    {
        parts.AddRange(["--licence", licence]);
    }

    if (source is not null)
    {
        parts.AddRange(["--source", source]);
    }

    return string.Join(" ", parts.Select(p => p.Contains(' ', StringComparison.Ordinal) ? $"\"{p}\"" : p));
}

// Must match FontMetrics.IsTrailingSpaceIgnored in Endjin.Badger.
static bool IsTrailingSpaceIgnored(int cp) =>
    Rune.IsWhiteSpace(new Rune(cp)) && cp != 0x00A0;

static string Describe(int cp) =>
    cp is >= 0x21 and < 0x7F ? ((char)cp).ToString() : $"U+{cp:X4} ";
