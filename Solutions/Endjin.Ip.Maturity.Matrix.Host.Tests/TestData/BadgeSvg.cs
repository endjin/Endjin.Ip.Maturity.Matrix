using System.Xml.Linq;

namespace Endjin.Ip.Maturity.Matrix.Host.Tests.TestData;

/// <summary>
/// The text and status colour of a badge returned by a badge function, read back from its SVG.
/// </summary>
/// <param name="Subject">The text in the subject (left-hand) segment.</param>
/// <param name="Status">The text in the status (right-hand) segment.</param>
/// <param name="StatusColour">The fill colour of the status segment.</param>
internal sealed record BadgeSvg(string Subject, string Status, string StatusColour)
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
        string[] texts = [.. root.Descendants(Svg + "text").Select(t => t.Value).Distinct()];

        return new BadgeSvg(texts[0], texts[1], statusColour);
    }
}
