using System.Xml.Linq;

namespace Endjin.Ip.Maturity.Model.Host.Tests.TestData;

/// <summary>
/// The text, status colour and accessible name of a badge returned by a badge function, read back from its SVG.
/// </summary>
/// <param name="Subject">The text in the subject (left-hand) segment.</param>
/// <param name="Status">The text in the status (right-hand) segment.</param>
/// <param name="StatusColour">The fill colour of the status segment.</param>
/// <param name="Title">The badge's title, which browsers show as a tooltip.</param>
/// <param name="AriaLabel">The badge's accessible name, which screen readers read out.</param>
internal sealed record BadgeSvg(string Subject, string Status, string StatusColour, string? Title, string? AriaLabel)
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    public static BadgeSvg Parse(string svg)
    {
        XElement root = XDocument.Parse(svg).Root!;

        // The coloured segments are the paths not filled with a url(...) gradient reference: subject first, then status.
        string statusColour = root.Descendants(Svg + "path")
            .Select(p => (string)p.Attribute("fill")!)
            .Where(fill => !fill.StartsWith("url(", StringComparison.Ordinal))
            .ElementAt(1);

        // Each text is drawn once, after its shadow (if any), which is hidden from assistive technology.
        string[] texts = [.. root.Descendants(Svg + "text").Where(t => (string?)t.Attribute("aria-hidden") != "true").Select(t => t.Value)];

        return new BadgeSvg(texts[0], texts[1], statusColour, root.Element(Svg + "title")?.Value, (string?)root.Attribute("aria-label"));
    }
}
