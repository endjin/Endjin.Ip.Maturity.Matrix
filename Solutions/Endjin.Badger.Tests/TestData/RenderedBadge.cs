using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Endjin.Badger.Tests.TestData;

/// <summary>
/// The geometry and content of a badge produced by <see cref="BadgePainter.DrawSVG"/>, read back
/// from the SVG so that tests can make assertions about layout without depending on the exact
/// widths of the fonts installed on the machine.
/// </summary>
/// <remarks>
/// Text positions and lengths are given in pixels: the badge draws its texts ten times larger inside a
/// <c>scale(.1)</c> group, and the parser applies that scale.
/// </remarks>
internal sealed partial record RenderedBadge(
    XDocument Svg,
    double Width,
    double Height,
    double SubjectWidth,
    double StatusStart,
    double StatusWidth,
    string StatusColour,
    double SubjectTextX,
    double StatusTextX,
    IReadOnlyList<string> Texts,
    IReadOnlyList<double> SubjectTextLengths,
    IReadOnlyList<double> StatusTextLengths,
    string? Title,
    string? AriaLabel)
{
    public static readonly XNamespace SvgNamespace = "http://www.w3.org/2000/svg";

    /// <summary>
    /// Parses a badge. This fails if the SVG is not well-formed XML, or if any coordinate is not
    /// written as an invariant-culture number.
    /// </summary>
    /// <param name="svg">The SVG text.</param>
    /// <param name="subject">The subject text the badge is expected to show.</param>
    /// <param name="status">The status text the badge is expected to show.</param>
    /// <returns>The parsed badge.</returns>
    public static RenderedBadge Parse(string svg, string subject, string status)
    {
        XDocument document = XDocument.Parse(svg);
        XElement root = document.Root!;

        // The segments are the paths filled with a colour (the gradient overlay, if any, is filled
        // with a url(...) reference). The first is the subject's, the second the status's.
        XElement[] segments = [.. root.Descendants(SvgNamespace + "path").Where(p => !((string)p.Attribute("fill")!).StartsWith("url(", StringComparison.Ordinal))];
        (double subjectStart, double subjectWidth) = ParseSegment(segments[0]);
        (double statusStart, double statusWidth) = ParseSegment(segments[1]);
        if (subjectStart != 0)
        {
            throw new FormatException($"Subject segment starts at {subjectStart}, not 0");
        }

        XElement[] texts = [.. root.Descendants(SvgNamespace + "text")];
        XElement[] subjectTexts = [.. texts.Where(t => t.Value == subject)];
        XElement[] statusTexts = [.. texts.Where(t => t.Value == status)];

        return new RenderedBadge(
            document,
            ParseNumber((string)root.Attribute("width")!),
            ParseNumber((string)root.Attribute("height")!),
            subjectWidth,
            statusStart,
            statusWidth,
            (string)segments[1].Attribute("fill")!,
            ParseScaled(subjectTexts[0], "x"),
            ParseScaled(statusTexts[0], "x"),
            [.. texts.Select(t => t.Value)],
            [.. subjectTexts.Select(t => ParseScaled(t, "textLength"))],
            [.. statusTexts.Select(t => ParseScaled(t, "textLength"))],
            root.Element(SvgNamespace + "title")?.Value,
            (string?)root.Attribute("aria-label"));
    }

    /// <summary>
    /// Reads the overall width from a badge's SVG text without parsing it as XML (for badges whose
    /// text deliberately contains characters that are not valid in XML).
    /// </summary>
    public static double ReadWidth(string svg) => ParseNumber(SvgWidthPattern().Match(svg).Groups["width"].Value);

    private static (double Start, double Width) ParseSegment(XElement path)
    {
        Match match = SegmentPathPattern().Match((string)path.Attribute("d")!);
        if (!match.Success)
        {
            throw new FormatException($"Unexpected segment path '{path.Attribute("d")}'");
        }

        return (ParseNumber(match.Groups["start"].Value), ParseNumber(match.Groups["width"].Value));
    }

    // A text's attribute in pixels: its value multiplied by the scale of the group it is drawn in.
    private static double ParseScaled(XElement text, string attribute)
    {
        double scale = 1;
        foreach (XElement element in text.AncestorsAndSelf())
        {
            string? transform = (string?)element.Attribute("transform");
            if (transform is not null)
            {
                Match match = ScalePattern().Match(transform);
                if (!match.Success)
                {
                    throw new FormatException($"Unexpected transform '{transform}'");
                }

                scale *= ParseNumber(match.Groups["scale"].Value.StartsWith('.') ? "0" + match.Groups["scale"].Value : match.Groups["scale"].Value);
            }
        }

        string value = (string?)text.Attribute(attribute) ?? throw new FormatException($"Text '{text.Value}' has no {attribute}");
        return ParseNumber(value) * scale;
    }

    private static double ParseNumber(string value)
    {
        if (!InvariantNumberPattern().IsMatch(value))
        {
            throw new FormatException($"'{value}' is not an invariant-culture number");
        }

        return double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    // e.g. M0 0h34.25086v20H0z, or M34.25086 0h42.3075v20H34.25086z
    [GeneratedRegex(@"^M(?<start>[0-9.E+-]+) 0h(?<width>[0-9.E+-]+)v[0-9]+H[0-9.E+-]+z$")]
    private static partial Regex SegmentPathPattern();

    [GeneratedRegex(@"^-?[0-9]+(\.[0-9]+)?(E[+-]?[0-9]+)?$")]
    private static partial Regex InvariantNumberPattern();

    // e.g. scale(.1)
    [GeneratedRegex(@"^scale\((?<scale>[0-9]*\.?[0-9]+)\)$")]
    private static partial Regex ScalePattern();

    [GeneratedRegex(@"<svg [^>]*width=""(?<width>[^""]+)""")]
    private static partial Regex SvgWidthPattern();
}
