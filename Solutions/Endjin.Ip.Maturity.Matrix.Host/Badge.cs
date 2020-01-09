namespace Endjin.Imm.App
{
    using Endjin.Ip.Maturity.Matrix.Host;

    using SixLabors.Fonts;

    using System.Globalization;
    using System.IO;
    using System.Net;

    public enum Style
    {
        Flat,
        FlatSquare,
        Plastic
    }

    public static class ColorScheme
    {
        public static string BrightGreen = "#4c1";
        public static string Green = "#97CA00";
        public static string Yellow = "#dfb317";
        public static string YellowGreen = "#a4a61d";
        public static string Orange = "#fe7d37";
        public static string Red = "#e05d44";
        public static string Blue = "#007ec6";
        public static string Grey = "#555";
        public static string Gray = "#555";
        public static string LightGrey = "#9f9f9f";
        public static string LightGray = "#9f9f9f";
    }

    public class Badge
    {
        public string DrawSVG(string subject, string status, string statusColor, Style style = Style.Flat)
        {
            var template = style switch
            {
                Style.Flat       => Resources.flat,
                Style.FlatSquare => Resources.flatSquare,
                Style.Plastic    => Resources.plastic,
                _ =>                File.ReadAllText("templates/flat-template.xml"),
            };
            Font font = new Font(SystemFonts.Find("verdana"), 11, FontStyle.Regular);

            var subjectWidth = TextMeasurer.MeasureBounds(WebUtility.HtmlDecode(subject), new RendererOptions(font, 96)).Width; //34.25086
            var statusWidth = TextMeasurer.Measure(WebUtility.HtmlDecode(status), new RendererOptions(font, 96)).Width; // 42.3075

            var result = string.Format(
                CultureInfo.InvariantCulture,
                template,
                subjectWidth + statusWidth, //width
                subjectWidth,
                statusWidth,
                (subjectWidth / 2) + 1,
                subjectWidth + (statusWidth / 2) - 1,
                subject,
                status,
                statusColor);

            return result;
        }

        public string ParseColor(string input)
        {
            var fieldInfo = typeof(ColorScheme).GetField(input);

            return ((string?)fieldInfo?.GetValue(null)) ?? string.Empty;
        }
    }
}