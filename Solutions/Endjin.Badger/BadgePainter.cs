namespace Endjin.Badger
{
    using SixLabors.Fonts;
    using System.Collections.Concurrent;
    using System.Globalization;
    using System.Net;
    using System.Text;
    using System.Threading;

    public static class BadgePainter
    {
        /// <summary>
        /// The most distinct texts whose measured widths are remembered. When more than this have been
        /// measured, every remembered width is discarded and the cache starts filling again, so a
        /// stream of distinct rule names cannot grow memory without limit.
        /// </summary>
        internal const int MaxCachedMeasurements = 1024;

        // The templates are read from the resources once and parsed once, rather than looked up through
        // the ResourceManager and re-parsed by string.Format on every call.
        private static readonly CompositeFormat FlatTemplate = CompositeFormat.Parse(Resources.flat);
        private static readonly CompositeFormat FlatSquareTemplate = CompositeFormat.Parse(Resources.flatSquare);
        private static readonly CompositeFormat PlasticTemplate = CompositeFormat.Parse(Resources.plastic);

        // Finding a font family is a linear scan of every installed family, so the font is resolved once,
        // on first use. This is lazy rather than a static initialiser so that, on a host with no fonts,
        // each call fails with the font lookup's own exception rather than a TypeInitializationException
        // that would also break the templates above.
        // The TextOptions instance is shared by every call, on every thread: SixLabors.Fonts only reads it
        // while measuring, and it must never be mutated.
        private static readonly Lazy<TextOptions> MeasurementOptions = new(CreateMeasurementOptions, LazyThreadSafetyMode.ExecutionAndPublication);

        // The advance width of each (HTML-decoded) text measured so far. Measuring is the most expensive
        // step of drawing a badge, and the same subjects and statuses are drawn over and over.
        private static readonly ConcurrentDictionary<string, float> MeasuredWidths = new(StringComparer.Ordinal);

        // Approximately how many entries MeasuredWidths holds. ConcurrentDictionary.Count takes every lock
        // in the dictionary, so it is counted here instead; the count only needs to be close enough to
        // bound the cache's size.
        private static int cachedMeasurementCount;

        public static string DrawSVG(string subject, string status, string statusColor, Style style = Style.Flat)
        {
            CompositeFormat template = style switch
            {
                Style.Flat => FlatTemplate,
                Style.FlatSquare => FlatSquareTemplate,
                Style.Plastic => PlasticTemplate,
                _ => FlatTemplate,
            };

            float subjectWidth = MeasureWidth(subject) + 8; //34.25086
            float statusWidth = MeasureWidth(status) + 8; // 42.3075

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

        private static float MeasureWidth(string text)
        {
            string decoded = WebUtility.HtmlDecode(text);
            if (MeasuredWidths.TryGetValue(decoded, out float width))
            {
                return width;
            }

            width = TextMeasurer.MeasureAdvance(decoded, MeasurementOptions.Value).Width;

            if (MeasuredWidths.TryAdd(decoded, width) &&
                Interlocked.Increment(ref cachedMeasurementCount) > MaxCachedMeasurements)
            {
                MeasuredWidths.Clear();
                Interlocked.Exchange(ref cachedMeasurementCount, 0);
            }

            return width;
        }

        private static TextOptions CreateMeasurementOptions()
        {
            // This takes the fonts size in points, which are bigger than pixels. 1 points is 1/72 inches,
            // whereas in web coordinate systems, 1 pixel is 1/96 inches. So we need to multiple by 0.75
            // to provide the font size in pixels.
            // Verdana is not installed on every platform (e.g. most Linux hosts), so fall back to the
            // first available font family; badge widths will then differ slightly.
            FontFamily family = SystemFonts.TryGet("Verdana", out FontFamily verdana) ? verdana : SystemFonts.Families.First();
            Font font = new(family, 11 * 0.75f, FontStyle.Regular);
            return new TextOptions(font) { Dpi = 96 };
        }
    }
}
