namespace Endjin.Badger
{
    using System.Globalization;
    using System.Net;
    using System.Text;

    /// <summary>
    /// Draws SVG status badges.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Text is sized with <see cref="VerdanaMetrics"/>, a table of Verdana's advance widths and kerning,
    /// so no font is loaded and a badge is identical on every machine, whatever fonts it has installed.
    /// </para>
    /// <para>
    /// The viewer's browser draws the text with whichever font in the templates' font-family list it
    /// has, which need not be Verdana, so every text is given a <c>textLength</c> of
    /// the measured width: the browser spaces the glyphs of whatever font it uses to fill exactly the
    /// width the badge was sized for. Texts are drawn at ten times the size inside a <c>scale(.1)</c>
    /// group, so that <c>x</c> and <c>textLength</c> can be written as whole numbers of tenths of a pixel:
    /// each is ten times the width in pixels rounded to the nearest whole number, midpoints away from zero.
    /// The segment widths are written unrounded.
    /// </para>
    /// </remarks>
    public static class BadgePainter
    {
        // The templates are read from the resources once and parsed once, rather than looked up through
        // the ResourceManager and re-parsed by string.Format on every call.
        private static readonly CompositeFormat FlatTemplate = CompositeFormat.Parse(Resources.flat);
        private static readonly CompositeFormat FlatSquareTemplate = CompositeFormat.Parse(Resources.flatSquare);
        private static readonly CompositeFormat PlasticTemplate = CompositeFormat.Parse(Resources.plastic);

        /// <summary>
        /// Draws a badge.
        /// </summary>
        /// <param name="subject">
        /// The text for the subject (left-hand) segment, HTML-encoded (for example with
        /// <see cref="WebUtility.HtmlEncode(string)"/>): it is written into the SVG, including its title and
        /// <c>aria-label</c> attribute, as it is, and measured as the text it decodes to.
        /// </param>
        /// <param name="status">The text for the status (right-hand) segment, HTML-encoded as <paramref name="subject"/> is.</param>
        /// <param name="statusColor">The fill colour of the status segment, such as a <see cref="ColorScheme"/> colour.</param>
        /// <param name="style">The style of badge.</param>
        /// <returns>The SVG.</returns>
        public static string DrawSVG(string subject, string status, string statusColor, Style style = Style.Flat)
        {
            CompositeFormat template = style switch
            {
                Style.Flat => FlatTemplate,
                Style.FlatSquare => FlatSquareTemplate,
                Style.Plastic => PlasticTemplate,
                _ => FlatTemplate,
            };

            float subjectTextWidth = MeasureWidth(subject);
            float statusTextWidth = MeasureWidth(status);
            float subjectWidth = subjectTextWidth + 8;
            float statusWidth = statusTextWidth + 8;

            var result = string.Format(
                CultureInfo.InvariantCulture,
                template,
                subjectWidth + statusWidth, //width
                subjectWidth,
                statusWidth,
                InTenths(subjectWidth / 2),
                InTenths(subjectWidth + (statusWidth / 2)),
                subject,
                status,
                statusColor,
                InTenths(subjectTextWidth),
                InTenths(statusTextWidth));

            return result;
        }

        /// <summary>
        /// Draws one badge in each style, so that the templates are loaded and parsed and the drawing
        /// code compiled before the first real request.
        /// </summary>
        public static void WarmUp()
        {
            foreach (Style style in (ReadOnlySpan<Style>)[Style.Flat, Style.FlatSquare, Style.Plastic])
            {
                _ = DrawSVG("IMM", "0 / 0", ColorScheme.Green, style);
            }
        }

        private static float MeasureWidth(string text) => VerdanaMetrics.MeasureWidth(WebUtility.HtmlDecode(text));

        // A length in pixels as the whole number of tenths of a pixel the templates' scaled-up text
        // coordinates are written in: ten times the length, rounded to the nearest whole number,
        // midpoints away from zero.
        private static int InTenths(float pixels) => (int)Math.Round(pixels * 10d, MidpointRounding.AwayFromZero);
    }
}
