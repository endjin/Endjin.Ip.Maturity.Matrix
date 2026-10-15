using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Endjin.Badger.Tests.TestData;

/// <summary>
/// The geometry and content of a badge produced by <see cref="BadgePainter.DrawSVG"/>, read back
/// from the SVG so that tests can make assertions about layout without depending on the exact
/// widths of the fonts installed on the machine.
/// </summary>
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
    IReadOnlyList<string> Texts)
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

        return new RenderedBadge(
            document,
            ParseNumber((string)root.Attribute("width")!),
            ParseNumber((string)root.Attribute("height")!),
            subjectWidth,
            statusStart,
            statusWidth,
            (string)segments[1].Attribute("fill")!,
            ParseNumber((string)texts.First(t => t.Value == subject).Attribute("x")!),
            ParseNumber((string)texts.First(t => t.Value == status).Attribute("x")!),
            [.. texts.Select(t => t.Value)]);
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

    [GeneratedRegex(@"<svg [^>]*width=""(?<width>[^""]+)""")]
    private static partial Regex SvgWidthPattern();
}
