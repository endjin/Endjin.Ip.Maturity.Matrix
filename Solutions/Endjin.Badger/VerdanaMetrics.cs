namespace Endjin.Badger
{
    /// <summary>
    /// The width of text set in Verdana at 11px, the font and size the badge templates ask for,
    /// computed from advance widths and kerning measured once from the font (see
    /// <c>tools/GenerateFontMetrics.cs</c>) rather than from a font installed on the machine.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Badge widths are therefore the same on every machine, including hosts with no fonts at all, and
    /// measuring costs no font loading. The tables reproduce what SixLabors.Fonts measured with Verdana
    /// installed; <see cref="FontMetrics{TTable}"/> describes how text is measured with them.
    /// </para>
    /// <para>
    /// Combining diacritical marks (U+0300 to U+036F) take no width. Any other character the tables do not
    /// cover is given <see cref="FallbackAdvanceUnits"/>.
    /// </para>
    /// </remarks>
    internal readonly partial struct VerdanaMetrics
    {
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
        internal static float MeasureWidth(ReadOnlySpan<char> text) =>
            FontMetrics<VerdanaMetrics>.MeasureUnits(text) * PixelsPerUnit;
    }
}
