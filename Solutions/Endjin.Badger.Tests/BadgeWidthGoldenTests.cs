using Shouldly;

namespace Endjin.Badger.Tests;

/// <summary>
/// Badge widths come from a table of Verdana's metrics rather than the fonts installed, so a badge
/// must be byte-for-byte the same on every operating system. The expected SVGs were rendered once
/// on Windows; <see cref="VerdanaMetricsOracleTests"/> shows that the widths in them match Verdana
/// itself, and they match what BadgePainter produced with Verdana installed before the table
/// existed to within 0.0001px.
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
            <svg xmlns="http://www.w3.org/2000/svg" width="79.87305" height="20">
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
              <g fill="#fff" text-anchor="middle" font-family="DejaVu Sans,Verdana,Geneva,sans-serif" font-size="11px">
                <text x="15.585449" y="15" fill="#010101" fill-opacity=".3">IMM</text>
                <text x="15.585449" y="14">IMM</text>
                <text x="55.521973" y="15" fill="#010101" fill-opacity=".3">42 / 54</text>
                <text x="55.521973" y="14">42 / 54</text>
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
            <svg xmlns="http://www.w3.org/2000/svg" width="270.896" height="20">
              <g shape-rendering="crispEdges">
                <path fill="#555" d="M0 0h237.07178v20H0z"/>
                <path fill="#dfb317" d="M237.07178 0h33.82422v20H237.07178z"/>
              </g>
              <g fill="#fff" text-anchor="middle" font-family="DejaVu Sans,Verdana,Geneva,sans-serif" font-size="11px">
                <text x="118.53589" y="14">Design &amp; Implementation Documentation</text>
                <text x="253.98389" y="14">67%</text>
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
            <svg xmlns="http://www.w3.org/2000/svg" width="127.149414" height="18">
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
              <g fill="#fff" text-anchor="middle" font-family="DejaVu Sans,Verdana,Geneva,sans-serif" font-size="11px">
                <text x="43.166016" y="14" fill="#010101" fill-opacity=".3">AVAWAY To Ty</text>
                <text x="43.166016" y="13">AVAWAY To Ty</text>
                <text x="106.74072" y="14" fill="#010101" fill-opacity=".3">100%</text>
                <text x="106.74072" y="13">100%</text>
              </g>
            </svg>
            """));
    }

    // The templates' line endings depend on how the resources were checked out and compiled; only
    // the content matters here.
    private static string Normalise(string svg) => svg.ReplaceLineEndings("\n").TrimEnd('\n');
}
