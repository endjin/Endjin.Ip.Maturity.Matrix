using System.Globalization;
using System.Xml.Linq;
using Endjin.Badger.Tests.TestData;
using Shouldly;

namespace Endjin.Badger.Tests;

/// <summary>
/// A badge is an SVG image in two segments, a grey subject on the left and a coloured status on
/// the right, each sized to fit its text. Text is sized from a table of Verdana's metrics, whatever
/// fonts are installed. These tests check the structure and proportions of a badge;
/// <see cref="BadgeWidthGoldenTests"/> and <see cref="VerdanaMetricsOracleTests"/> check absolute widths.
/// </summary>
[TestClass]
public class BadgeRenderingTests
{
    private const double Tolerance = 0.01;

    [TestMethod]
    [DataRow(Style.Flat)]
    [DataRow(Style.FlatSquare)]
    [DataRow(Style.Plastic)]
    public void DrawSVG_AnyStyle_ProducesAnSvgImageShowingTheSubjectAndStatusInTheStatusColour(Style style)
    {
        // Act
        string svg = BadgePainter.DrawSVG("IMM", "21 / 32", ColorScheme.YellowGreen, style);

        // Assert
        RenderedBadge badge = RenderedBadge.Parse(svg, "IMM", "21 / 32");
        badge.Svg.Root!.Name.ShouldBe(RenderedBadge.SvgNamespace + "svg");
        badge.Texts.Distinct().ShouldBe(["IMM", "21 / 32"], ignoreOrder: true);
        badge.StatusColour.ShouldBe(ColorScheme.YellowGreen);
        badge.SubjectWidth.ShouldBeGreaterThan(8);
        badge.StatusWidth.ShouldBeGreaterThan(8);
        badge.Width.ShouldBeGreaterThan(16);
    }

    [TestMethod]
    [DataRow(Style.Flat)]
    [DataRow(Style.FlatSquare)]
    [DataRow(Style.Plastic)]
    public void DrawSVG_AnyStyle_PlacesTheStatusImmediatelyAfterTheSubjectWithEachTextCentredInItsSegment(Style style)
    {
        // Act
        string svg = BadgePainter.DrawSVG("Shared Engineering Standards", "100%", ColorScheme.Green, style);

        // Assert
        RenderedBadge badge = RenderedBadge.Parse(svg, "Shared Engineering Standards", "100%");
        badge.StatusStart.ShouldBe(badge.SubjectWidth, Tolerance);
        badge.Width.ShouldBe(badge.SubjectWidth + badge.StatusWidth, Tolerance);
        badge.SubjectTextX.ShouldBe(badge.SubjectWidth / 2, Tolerance);
        badge.StatusTextX.ShouldBe(badge.SubjectWidth + (badge.StatusWidth / 2), Tolerance);
    }

    [TestMethod]
    public void DrawSVG_LongerSubject_WidensOnlyTheSubjectSegment()
    {
        // Act
        RenderedBadge shortSubject = RenderedBadge.Parse(BadgePainter.DrawSVG("IMM", "50%", ColorScheme.Yellow), "IMM", "50%");
        RenderedBadge longSubject = RenderedBadge.Parse(
            BadgePainter.DrawSVG("Shared Engineering Standards", "50%", ColorScheme.Yellow),
            "Shared Engineering Standards",
            "50%");

        // Assert
        longSubject.SubjectWidth.ShouldBeGreaterThan(shortSubject.SubjectWidth);
        longSubject.StatusWidth.ShouldBe(shortSubject.StatusWidth, Tolerance);
        longSubject.Width.ShouldBeGreaterThan(shortSubject.Width);
    }

    [TestMethod]
    public void DrawSVG_LongerStatus_WidensOnlyTheStatusSegment()
    {
        // Act
        RenderedBadge shortStatus = RenderedBadge.Parse(BadgePainter.DrawSVG("IMM", "1 / 3", ColorScheme.Red), "IMM", "1 / 3");
        RenderedBadge longStatus = RenderedBadge.Parse(BadgePainter.DrawSVG("IMM", "121 / 300", ColorScheme.Red), "IMM", "121 / 300");

        // Assert
        longStatus.StatusWidth.ShouldBeGreaterThan(shortStatus.StatusWidth);
        longStatus.SubjectWidth.ShouldBe(shortStatus.SubjectWidth, Tolerance);
        longStatus.Width.ShouldBeGreaterThan(shortStatus.Width);
    }

    [TestMethod]
    public void DrawSVG_HtmlEncodedSubject_ShowsTheDecodedTextAndIsSizedForIt()
    {
        // Act
        string encoded = BadgePainter.DrawSVG("R&amp;D &lt;Tools&gt;", "67%", ColorScheme.YellowGreen);
        string unencoded = BadgePainter.DrawSVG("R&D <Tools>", "67%", ColorScheme.YellowGreen);
        string encodingShownLiterally = BadgePainter.DrawSVG("R&amp;amp;D &amp;lt;Tools&amp;gt;", "67%", ColorScheme.YellowGreen);

        // Assert
        RenderedBadge badge = RenderedBadge.Parse(encoded, "R&D <Tools>", "67%");
        badge.Texts.Distinct().ShouldBe(["R&D <Tools>", "67%"], ignoreOrder: true);
        badge.Width.ShouldBe(RenderedBadge.ReadWidth(unencoded), Tolerance);
        badge.Width.ShouldBeLessThan(RenderedBadge.ReadWidth(encodingShownLiterally));
    }

    [TestMethod]
    [DataRow("\U0001F600")]
    [DataRow("中")]
    [DataRow("\u0080")]
    public void DrawSVG_CharacterNotInTheVerdanaTables_IsGivenTheFallbackWidth(string character)
    {
        // Arrange
        float fallbackWidth = VerdanaMetrics.FallbackAdvanceUnits * VerdanaMetrics.PixelSize / VerdanaMetrics.UnitsPerEm;
        double withoutCharacter = RenderedBadge.Parse(BadgePainter.DrawSVG("IMM", "50%", ColorScheme.Yellow), "IMM", "50%").SubjectWidth;

        // Act
        string svg = BadgePainter.DrawSVG("IMM" + character, "50%", ColorScheme.Yellow);

        // Assert
        RenderedBadge badge = RenderedBadge.Parse(svg, "IMM" + character, "50%");
        badge.SubjectWidth.ShouldBe(withoutCharacter + fallbackWidth, Tolerance);
        fallbackWidth.ShouldBe(6.1821f, 0.0001f);
    }

    [TestMethod]
    public void DrawSVG_SubjectWithCombiningMarks_IsSizedAsIfTheMarksTookNoWidth()
    {
        // Act
        string combined = BadgePainter.DrawSVG("Résumé", "50%", ColorScheme.Yellow);
        string unmarked = BadgePainter.DrawSVG("Resume", "50%", ColorScheme.Yellow);

        // Assert
        RenderedBadge.ReadWidth(combined).ShouldBe(RenderedBadge.ReadWidth(unmarked), Tolerance);
    }

    [TestMethod]
    public void DrawSVG_SubjectWithTrailingSpace_IsSizedAsIfTheSpaceWereNotThere()
    {
        // Act
        string trailing = BadgePainter.DrawSVG("Coding Standards ", "50%", ColorScheme.Yellow);
        string trimmed = BadgePainter.DrawSVG("Coding Standards", "50%", ColorScheme.Yellow);

        // Assert
        RenderedBadge.ReadWidth(trailing).ShouldBe(RenderedBadge.ReadWidth(trimmed), Tolerance);
    }

    [TestMethod]
    [DataRow(Style.Flat, 20, true, "3", false)]
    [DataRow(Style.FlatSquare, 20, false, null, true)]
    [DataRow(Style.Plastic, 18, true, "4", false)]
    public void DrawSVG_Style_SelectsTheShapeAndShadingOfTheBadge(
        Style style,
        int expectedHeight,
        bool expectedGradient,
        string? expectedCornerRadius,
        bool expectedCrispEdges)
    {
        // Act
        string svg = BadgePainter.DrawSVG("IMM", "21 / 32", ColorScheme.YellowGreen, style);

        // Assert
        RenderedBadge badge = RenderedBadge.Parse(svg, "IMM", "21 / 32");
        XElement root = badge.Svg.Root!;
        XNamespace svgNs = RenderedBadge.SvgNamespace;
        badge.Height.ShouldBe(expectedHeight);
        root.Descendants(svgNs + "linearGradient").Any().ShouldBe(expectedGradient);
        ((string?)root.Descendants(svgNs + "rect").SingleOrDefault()?.Attribute("rx")).ShouldBe(expectedCornerRadius);
        root.Descendants(svgNs + "g").Any(g => (string?)g.Attribute("shape-rendering") == "crispEdges").ShouldBe(expectedCrispEdges);
    }

    [TestMethod]
    public void DrawSVG_UnrecognisedStyle_RendersAFlatBadge()
    {
        // Act
        string svg = BadgePainter.DrawSVG("IMM", "21 / 32", ColorScheme.YellowGreen, (Style)99);

        // Assert
        svg.ShouldBe(BadgePainter.DrawSVG("IMM", "21 / 32", ColorScheme.YellowGreen, Style.Flat));
    }

    [TestMethod]
    public void DrawSVG_DefaultStyle_IsFlat()
    {
        // Act
        string svg = BadgePainter.DrawSVG("IMM", "21 / 32", ColorScheme.YellowGreen);

        // Assert
        svg.ShouldBe(BadgePainter.DrawSVG("IMM", "21 / 32", ColorScheme.YellowGreen, Style.Flat));
    }

    [TestMethod]
    [DataRow("de-DE")]
    [DataRow("fr-FR")]
    [DataRow("ar-SA")]
    public void DrawSVG_CultureWithDifferentNumberFormat_WritesInvariantNumbers(string cultureName)
    {
        // Arrange
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        string invariantSvg = BadgePainter.DrawSVG("Shared Engineering Standards", "67%", ColorScheme.YellowGreen);
        string svg;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);

            // Act
            svg = BadgePainter.DrawSVG("Shared Engineering Standards", "67%", ColorScheme.YellowGreen);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }

        // Assert
        svg.ShouldBe(invariantSvg);
        Should.NotThrow(() => RenderedBadge.Parse(svg, "Shared Engineering Standards", "67%"));
    }
}
