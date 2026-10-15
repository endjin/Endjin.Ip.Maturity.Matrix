#:package SixLabors.Fonts@2.1.3
#:property PublishAot=false

// Generates Solutions/Endjin.Badger/VerdanaMetrics.g.cs: the advance widths and kerning of Verdana at
// 11px, so that Endjin.Badger can size badge text without any fonts installed.
//
// Usage (Windows, where Verdana is installed):
//   dotnet run tools/GenerateVerdanaMetrics.cs -- --font C:\Windows\Fonts\verdana.ttf --out Solutions/Endjin.Badger/VerdanaMetrics.g.cs
//
// Only numbers derived from the font are written out. The Verdana licence does not allow the font file
// itself to be redistributed, so never commit verdana.ttf or a copy of it.
//
// Every number is measured with SixLabors.Fonts exactly as BadgePainter measured text before this table
// existed (8.25pt at 96 DPI, i.e. 11px, with the default kerning), from a private FontCollection so that
// the result never depends on which fonts happen to be installed.
using System.Globalization;
using System.Text;
using SixLabors.Fonts;
using SixLabors.Fonts.Unicode;
using SixLabors.Fonts.WellKnownIds;

string? fontPath = null;
string? outPath = null;
for (int i = 0; i < args.Length - 1; i++)
{
    switch (args[i])
    {
        case "--font": fontPath = args[++i]; break;
        case "--out": outPath = args[++i]; break;
    }
}

if (fontPath is null || outPath is null)
{
    Console.Error.WriteLine("Usage: dotnet run tools/GenerateVerdanaMetrics.cs -- --font <verdana.ttf> --out <VerdanaMetrics.g.cs>");
    return 1;
}

const int UnitsPerEm = 2048;
const float PixelSize = 11f;
const int LatinFirst = 0x20;
const int LatinLast = 0x24F;

FontCollection collection = new();
FontFamily family = collection.Add(fontPath, out FontDescription description);
Font font = new(family, PixelSize * 0.75f, FontStyle.Regular);
TextOptions options = new(font) { Dpi = 96 };

if (font.FontMetrics.UnitsPerEm != UnitsPerEm)
{
    Console.Error.WriteLine($"Expected {UnitsPerEm} units per em but the font has {font.FontMetrics.UnitsPerEm}.");
    return 1;
}

string version = description.GetNameById(CultureInfo.InvariantCulture, KnownNameIds.Version);
string fontName = description.FontNameInvariantCulture;
HashSet<int> available = [.. font.FontMetrics.GetAvailableCodePoints().Select(cp => cp.Value)];

// The advance of one character, in font units. Measured widths at 11px are exact multiples of
// 11/2048px, so rounding only removes float noise.
int MeasureUnits(string text) =>
    (int)Math.Round(TextMeasurer.MeasureAdvance(text, options).Width * UnitsPerEm / PixelSize, MidpointRounding.AwayFromZero);

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
// past the end of the line), so "a " measures the same as "a". VerdanaMetrics mirrors that with
// VerdanaMetrics.IsTrailingSpaceIgnored; check that rule agrees with every character the table covers.
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
code.AppendLine($"// Generated by tools/GenerateVerdanaMetrics.cs from {fontName} \"{version}\" on {DateTime.UtcNow:yyyy-MM-dd}; do not edit.");
code.AppendLine("// Regenerate with:");
code.AppendLine("//   dotnet run tools/GenerateVerdanaMetrics.cs -- --font C:\\Windows\\Fonts\\verdana.ttf --out Solutions/Endjin.Badger/VerdanaMetrics.g.cs");
code.AppendLine("// These are metrics derived from the font, not the font itself.");
code.AppendLine("// </auto-generated>");
code.AppendLine("namespace Endjin.Badger");
code.AppendLine("{");
code.AppendLine("    internal static partial class VerdanaMetrics");
code.AppendLine("    {");
code.AppendLine("        /// <summary>The version string of the font the metrics were measured from.</summary>");
code.AppendLine($"        internal const string FontVersion = \"{fontName} {version}\";");
code.AppendLine();
code.AppendLine("        /// <summary>");
code.AppendLine("        /// The advance, in font units, of a character the tables do not cover: the mean advance of the");
code.AppendLine("        /// lower-case letters a to z, rounded to the nearest unit.");
code.AppendLine("        /// </summary>");
code.AppendLine($"        internal const int FallbackAdvanceUnits = {fallback};");
code.AppendLine();
code.AppendLine($"        private const int LatinFirst = 0x{LatinFirst:X4};");
code.AppendLine($"        private const int LatinLast = 0x{LatinLast:X4};");
code.AppendLine();
code.AppendLine("        // Advances in font units for U+0020..U+024F, indexed by code point - LatinFirst (0 = not in the font).");
code.AppendLine("        private static ReadOnlySpan<ushort> LatinAdvances =>");
code.AppendLine("        [");
for (int row = 0; row < advances.Length; row += 16)
{
    IEnumerable<ushort> slice = advances.Skip(row).Take(16);
    code.AppendLine($"            {string.Join(", ", slice.Select(a => a.ToString(CultureInfo.InvariantCulture).PadLeft(4)))}, // U+{row + LatinFirst:X4}");
}

code.AppendLine("        ];");
code.AppendLine();
code.AppendLine("        // Code points outside the Latin range that the font covers, in ascending order, and their advances.");
code.AppendLine("        private static ReadOnlySpan<int> ExtraCodePoints =>");
code.AppendLine("        [");
foreach ((int cp, int _) in extras)
{
    code.AppendLine($"            0x{cp:X4},");
}

code.AppendLine("        ];");
code.AppendLine();
code.AppendLine("        private static ReadOnlySpan<ushort> ExtraAdvances =>");
code.AppendLine("        [");
foreach ((int cp, int units) in extras)
{
    code.AppendLine($"            {units,4}, // U+{cp:X4} {char.ConvertFromUtf32(cp)}");
}

code.AppendLine("        ];");
code.AppendLine();
code.AppendLine("        // Kerning pairs between Latin characters, keyed by (left << 16) | right, in ascending order.");
code.AppendLine("        private static ReadOnlySpan<int> KerningPairs =>");
code.AppendLine("        [");
foreach ((int key, short _) in kerning)
{
    code.AppendLine($"            0x{key:X8}, // {Describe(key >> 16)}{Describe(key & 0xFFFF)}");
}

code.AppendLine("        ];");
code.AppendLine();
code.AppendLine("        // The kerning of each pair in KerningPairs, in font units.");
code.AppendLine("        private static ReadOnlySpan<short> KerningUnits =>");
code.AppendLine("        [");
for (int row = 0; row < kerning.Length; row += 16)
{
    code.AppendLine($"            {string.Join(", ", kerning.Skip(row).Take(16).Select(k => k.Units.ToString(CultureInfo.InvariantCulture).PadLeft(4)))},");
}

code.AppendLine("        ];");
code.AppendLine("    }");
code.AppendLine("}");

File.WriteAllText(outPath, code.ToString().ReplaceLineEndings("\r\n"), new UTF8Encoding(true));

Console.WriteLine($"Font: {fontName} {version}");
Console.WriteLine($"Latin advances: {advances.Count(a => a != 0)} of {advances.Length}; extras: {extras.Count}; kerning pairs: {kerning.Length}; fallback: {fallback} units");
Console.WriteLine($"Spot check: a={advances['a' - LatinFirst]} M={advances['M' - LatinFirst]} space={advances[' ' - LatinFirst]}");
if (zeroWidth.Count > 0)
{
    Console.WriteLine($"Zero-width (left to the fallback/combining rule): {string.Join(" ", zeroWidth.Select(cp => $"U+{cp:X4}"))}");
}

return 0;

// Must match VerdanaMetrics.IsTrailingSpaceIgnored in Endjin.Badger.
static bool IsTrailingSpaceIgnored(int cp) =>
    Rune.IsWhiteSpace(new Rune(cp)) && cp != 0x00A0;

static string Describe(int cp) =>
    cp is >= 0x21 and < 0x7F ? ((char)cp).ToString() : $"U+{cp:X4} ";
