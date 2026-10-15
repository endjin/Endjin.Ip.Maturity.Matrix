using Shouldly;

namespace Endjin.Badger.Tests;

/// <summary>
/// Badge widths come from a table of Verdana's metrics rather than the fonts installed, so a badge
/// must be byte-for-byte the same on every operating system. The expected SVGs were rendered once
/// on Windows; <see cref="VerdanaMetricsOracleTests"/> shows that the widths in them match Verdana
/// itself, and they match what BadgePainter produced with Verdana installed before the table
/// existed to within 0.0001px. Each text's <c>textLength</c> is its measured width (the segment width
/// less 8px of padding) and its <c>x</c> the centre of its segment, both in tenths of a pixel rounded
/// to the nearest whole number, because the texts are drawn ten times larger in a <c>scale(.1)</c> group.
/// </summary>
[TestClass]
public class BadgeWidthGoldenTests
{
    [TestMethod]
    public void DrawSVG_TotalBadgeFlat_IsTheExpectedSvg()
    {
        // Act
        string svg = BadgePainter.DrawSVG("IMM", "42 / 54", ColorScheme.YellowGreen, Style.Flat);

        // Assert
        Normalise(svg).ShouldBe(Normalise(
            """
            <svg xmlns="http://www.w3.org/2000/svg" width="79.87305" height="20" role="img" aria-label="IMM: 42 / 54">
              <title>IMM: 42 / 54</title>
              <linearGradient id="b" x2="0" y2="100%">
                <stop offset="0" stop-color="#bbb" stop-opacity=".1"/>
                <stop offset="1" stop-opacity=".1"/>
              </linearGradient>
              <mask id="a">
                <rect width="79.87305" height="20" rx="3" fill="#fff"/>
              </mask>
              <g mask="url(#a)">
                <path fill="#555" d="M0 0h31.170898v20H0z"/>
                <path fill="#a4a61d" d="M31.170898 0h48.70215v20H31.170898z"/>
                <path fill="url(#b)" d="M0 0h79.87305v20H0z"/>
              </g>
              <g fill="#fff" text-anchor="middle" font-family="Verdana,Geneva,DejaVu Sans,sans-serif" text-rendering="geometricPrecision" font-size="110">
                <g transform="scale(.1)">
                  <text aria-hidden="true" x="156" y="150" fill="#010101" fill-opacity=".3" textLength="232">IMM</text>
                  <text x="156" y="140" textLength="232">IMM</text>
                  <text aria-hidden="true" x="555" y="150" fill="#010101" fill-opacity=".3" textLength="407">42 / 54</text>
                  <text x="555" y="140" textLength="407">42 / 54</text>
                </g>
              </g>
            </svg>
            """));
    }

    [TestMethod]
    public void DrawSVG_HtmlEncodedRuleNameFlatSquare_IsTheExpectedSvgSizedForTheDecodedText()
    {
        // Act
        string svg = BadgePainter.DrawSVG("Design &amp; Implementation Documentation", "67%", ColorScheme.Yellow, Style.FlatSquare);

        // Assert
        Normalise(svg).ShouldBe(Normalise(
            """
            <svg xmlns="http://www.w3.org/2000/svg" width="270.896" height="20" role="img" aria-label="Design &amp; Implementation Documentation: 67%">
              <title>Design &amp; Implementation Documentation: 67%</title>
              <g shape-rendering="crispEdges">
                <path fill="#555" d="M0 0h237.07178v20H0z"/>
                <path fill="#dfb317" d="M237.07178 0h33.82422v20H237.07178z"/>
              </g>
              <g fill="#fff" text-anchor="middle" font-family="Verdana,Geneva,DejaVu Sans,sans-serif" text-rendering="geometricPrecision" font-size="110">
                <g transform="scale(.1)">
                  <text x="1185" y="140" textLength="2291">Design &amp; Implementation Documentation</text>
                  <text x="2540" y="140" textLength="258">67%</text>
                </g>
              </g>
            </svg>
            """));
    }

    [TestMethod]
    public void DrawSVG_KernedSubjectPlastic_IsTheExpectedSvgIncludingKerning()
    {
        // Act
        string svg = BadgePainter.DrawSVG("AVAWAY To Ty", "100%", ColorScheme.BrightGreen, Style.Plastic);

        // Assert
        Normalise(svg).ShouldBe(Normalise(
            """
            <svg xmlns="http://www.w3.org/2000/svg" width="127.149414" height="18" role="img" aria-label="AVAWAY To Ty: 100%">
              <title>AVAWAY To Ty: 100%</title>
              <linearGradient id="b" x2="0" y2="100%">
                <stop offset="0" stop-color="#fff" stop-opacity=".7"/>
                <stop offset=".1" stop-color="#aaa" stop-opacity=".1"/>
                <stop offset=".9" stop-opacity=".3"/>
                <stop offset="1" stop-opacity=".5"/>
              </linearGradient>
              <mask id="a">
                <rect width="127.149414" height="18" rx="4" fill="#fff"/>
              </mask>
              <g mask="url(#a)">
                <path fill="#555" d="M0 0h86.33203v18H0z"/>
                <path fill="#4c1" d="M86.33203 0h40.817383v18H86.33203z"/>
                <path fill="url(#b)" d="M0 0h127.149414v18H0z"/>
              </g>
              <g fill="#fff" text-anchor="middle" font-family="Verdana,Geneva,DejaVu Sans,sans-serif" text-rendering="geometricPrecision" font-size="110">
                <g transform="scale(.1)">
                  <text aria-hidden="true" x="432" y="140" fill="#010101" fill-opacity=".3" textLength="783">AVAWAY To Ty</text>
                  <text x="432" y="130" textLength="783">AVAWAY To Ty</text>
                  <text aria-hidden="true" x="1067" y="140" fill="#010101" fill-opacity=".3" textLength="328">100%</text>
                  <text x="1067" y="130" textLength="328">100%</text>
                </g>
              </g>
            </svg>
            """));
    }

    // The templates' line endings depend on how the resources were checked out and compiled; only
    // the content matters here.
    private static string Normalise(string svg) => svg.ReplaceLineEndings("\n").TrimEnd('\n');
}
