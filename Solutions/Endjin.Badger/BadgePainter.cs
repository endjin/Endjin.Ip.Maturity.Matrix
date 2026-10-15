namespace Endjin.Badger
{
    using System.Globalization;
    using System.Net;
    using System.Text;

    /// <summary>
    /// Draws shields-style SVG badges.
    /// </summary>
    /// <remarks>
    /// Text is sized with <see cref="VerdanaMetrics"/>, a table of Verdana's advance widths and kerning,
    /// so no font is loaded and a badge is identical on every machine, whatever fonts it has installed.
    /// </remarks>
    public static class BadgePainter
    {
        // The templates are read from the resources once and parsed once, rather than looked up through
        // the ResourceManager and re-parsed by string.Format on every call.
        private static readonly CompositeFormat FlatTemplate = CompositeFormat.Parse(Resources.flat);
        private static readonly CompositeFormat FlatSquareTemplate = CompositeFormat.Parse(Resources.flatSquare);
        private static readonly CompositeFormat PlasticTemplate = CompositeFormat.Parse(Resources.plastic);

        public static string DrawSVG(string subject, string status, string statusColor, Style style = Style.Flat)
        {
            CompositeFormat template = style switch
            {
                Style.Flat => FlatTemplate,
                Style.FlatSquare => FlatSquareTemplate,
                Style.Plastic => PlasticTemplate,
                _ => FlatTemplate,
            };

            float subjectWidth = MeasureWidth(subject) + 8;
            float statusWidth = MeasureWidth(status) + 8;

            var result = string.Format(
                CultureInfo.InvariantCulture,
                template,
                subjectWidth + statusWidth, //width
                subjectWidth,
                statusWidth,
                subjectWidth / 2,
                subjectWidth + (statusWidth / 2),
                subject,
                status,
                statusColor);

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
    }
}
