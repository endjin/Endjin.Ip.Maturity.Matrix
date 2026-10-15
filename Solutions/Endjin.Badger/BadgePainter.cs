namespace Endjin.Badger
{
    using SixLabors.Fonts;
    using System.Globalization;
    using System.Net;

    public static class BadgePainter
    {
        public static string DrawSVG(string subject, string status, string statusColor, Style style = Style.Flat)
        {
            var template = style switch
            {
                Style.Flat => Resources.flat,
                Style.FlatSquare => Resources.flatSquare,
                Style.Plastic => Resources.plastic,
                _ => Resources.flat,
            };

            // This takes the fonts size in points, which are bigger than pixels. 1 points is 1/72 inches,
            // whereas in web coordinate systems, 1 pixel is 1/96 inches. So we need to multiple by 0.75
            // to provide the font size in pixels.
            // Verdana is not installed on every platform (e.g. most Linux hosts), so fall back to the
            // first available font family; badge widths will then differ slightly.
            FontFamily family = SystemFonts.TryGet("Verdana", out FontFamily verdana) ? verdana : SystemFonts.Families.First();
            Font font = new(family, 11 * 0.75f, FontStyle.Regular);
            TextOptions options = new(font) { Dpi = 96 };

            float subjectWidth = TextMeasurer.MeasureAdvance(WebUtility.HtmlDecode(subject), options).Width + 8; //34.25086
            float statusWidth = TextMeasurer.MeasureAdvance(WebUtility.HtmlDecode(status), options).Width + 8; // 42.3075

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
    }
}
