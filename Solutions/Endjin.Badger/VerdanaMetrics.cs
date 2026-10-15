namespace Endjin.Badger
{
    using System.Text;

    /// <summary>
    /// The width of text set in Verdana at 11px, the font and size the badge templates ask for,
    /// computed from advance widths and kerning measured once from the font (see
    /// <c>tools/GenerateVerdanaMetrics.cs</c>) rather than from a font installed on the machine.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Badge widths are therefore the same on every machine, including hosts with no fonts at all, and
    /// measuring costs no font loading. The tables reproduce what SixLabors.Fonts measured with Verdana
    /// installed: advances are summed in font units, kerning between adjacent Latin characters is added,
    /// breaking whitespace at the end of the text is ignored, and the total is scaled to pixels once.
    /// </para>
    /// <para>
    /// Combining diacritical marks (U+0300 to U+036F) take no width. Any other character the tables do not
    /// cover is given <see cref="FallbackAdvanceUnits"/>.
    /// </para>
    /// </remarks>
    internal static partial class VerdanaMetrics
    {
        /// <summary>Font units per em in Verdana.</summary>
        internal const int UnitsPerEm = 2048;

        /// <summary>The font size, in pixels, the badge templates use.</summary>
        internal const float PixelSize = 11f;

        // 11/2048 is exact in binary, so (as long as the total stays below 2^24 / 11 units) the width
        // is exactly the total units scaled, with no rounding error.
        private const float PixelsPerUnit = PixelSize / UnitsPerEm;

        /// <summary>
        /// Measures the advance width of a text, in pixels.
        /// </summary>
        /// <param name="text">The text, as it will be displayed (i.e., already HTML-decoded).</param>
        /// <returns>The width in pixels.</returns>
        internal static float MeasureWidth(ReadOnlySpan<char> text)
        {
            ReadOnlySpan<char> measured = TrimIgnoredTrailingSpace(text);
            if (measured.IsEmpty && !text.IsEmpty)
            {
                // A text made only of whitespace is measured as its first character, as SixLabors.Fonts does.
                Rune.DecodeFromUtf16(text, out Rune first, out _);
                return AdvanceUnits(first.Value) * PixelsPerUnit;
            }

            int units = 0;
            int previous = -1;
            foreach (Rune rune in measured.EnumerateRunes())
            {
                int codePoint = rune.Value;
                if (IsCombiningMark(codePoint))
                {
                    continue;
                }

                units += AdvanceUnits(codePoint);
                if (previous >= 0)
                {
                    units += KerningUnitsFor(previous, codePoint);
                }

                previous = codePoint;
            }

            return units * PixelsPerUnit;
        }

        /// <summary>
        /// Gets the advance of a single character, in font units.
        /// </summary>
        /// <param name="codePoint">The Unicode code point.</param>
        /// <returns>The advance in font units.</returns>
        internal static int AdvanceUnits(int codePoint)
        {
            if (codePoint is >= LatinFirst and <= LatinLast)
            {
                int advance = LatinAdvances[codePoint - LatinFirst];
                return advance != 0 ? advance : FallbackAdvanceUnits;
            }

            int index = ExtraCodePoints.BinarySearch(codePoint);
            return index >= 0 ? ExtraAdvances[index] : FallbackAdvanceUnits;
        }

        /// <summary>
        /// Determines whether a character is left out of the width when it ends the text. This is
        /// breaking whitespace, which SixLabors.Fonts lets hang past the end of the line.
        /// </summary>
        /// <param name="codePoint">The Unicode code point.</param>
        /// <returns>True if the character is ignored at the end of a text.</returns>
        internal static bool IsTrailingSpaceIgnored(int codePoint) =>
            Rune.IsWhiteSpace(new Rune(codePoint)) && codePoint != 0x00A0;

        private static bool IsCombiningMark(int codePoint) => codePoint is >= 0x0300 and <= 0x036F;

        private static int KerningUnitsFor(int left, int right)
        {
            if (left > LatinLast || right > LatinLast)
            {
                return 0;
            }

            int index = KerningPairs.BinarySearch((left << 16) | right);
            return index >= 0 ? KerningUnits[index] : 0;
        }

        private static ReadOnlySpan<char> TrimIgnoredTrailingSpace(ReadOnlySpan<char> text)
        {
            while (!text.IsEmpty)
            {
                // Every character IsTrailingSpaceIgnored accepts is in the Basic Multilingual Plane.
                char last = text[^1];
                if (char.IsSurrogate(last) || !IsTrailingSpaceIgnored(last))
                {
                    break;
                }

                text = text[..^1];
            }

            return text;
        }
    }
}
