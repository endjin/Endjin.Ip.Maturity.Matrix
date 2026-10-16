using System.Globalization;
using System.Xml.Linq;
using Endjin.Badger.Tests.TestData;
using Shouldly;

namespace Endjin.Badger.Tests;

/// <summary>
/// A badge is an SVG image in two segments, a grey subject on the left and a coloured status on
/// the right, each sized to fit its text. Text is sized from a table of Verdana's metrics, whatever
/// fonts are installed, and every text is given that width as its <c>textLength</c>, so that a browser
/// drawing it in another font still fills the same width. These tests check the structure and
/// proportions of a badge; <see cref="BadgeWidthGoldenTests"/> and <see cref="VerdanaMetricsWidthTests"/>
/// check absolute widths, and <see cref="FallbackFontWidthTests"/> how far other fonts are from Verdana.
/// </summary>
[TestClass]
public class BadgeRenderingTests
{
    private const double Tolerance = 0.01;

    // Text positions and lengths are written in whole tenths of a pixel, so are up to 0.05px from exact.
    private const double TextTolerance = 0.05;

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
        badge.SubjectTextX.ShouldBe(badge.SubjectWidth / 2, TextTolerance);
        badge.StatusTextX.ShouldBe(badge.SubjectWidth + (badge.StatusWidth / 2), TextTolerance);
    }

    [TestMethod]
    [DataRow(Style.Flat, "IMM", "42 / 54")]
    [DataRow(Style.FlatSquare, "Shared Engineering Standards", "100%")]
    [DataRow(Style.Plastic, "AVAWAY To Ty", "0%")]
    [DataRow(Style.Flat, "Design &amp; Implementation Documentation", "imm.yaml not found")]
    public void DrawSVG_AnyStyle_GivesEveryTextATextLengthOfTenTimesItsMeasuredWidthInTheScaledGroup(Style style, string subject, string status)
    {
        // Arrange
        string displayedSubject = System.Net.WebUtility.HtmlDecode(subject);
        double subjectTenths = Math.Round(VerdanaMetrics.MeasureWidth(displayedSubject) * 10d, MidpointRounding.AwayFromZero);
        double statusTenths = Math.Round(VerdanaMetrics.MeasureWidth(status) * 10d, MidpointRounding.AwayFromZero);

        // Act
        string svg = BadgePainter.DrawSVG(subject, status, ColorScheme.Green, style);

        // Assert
        RenderedBadge badge = RenderedBadge.Parse(svg, displayedSubject, status);
        int textsPerSegment = style == Style.FlatSquare ? 1 : 2;
        badge.SubjectTextLengths.Count.ShouldBe(textsPerSegment);
        badge.StatusTextLengths.Count.ShouldBe(textsPerSegment);
        XNamespace svgNs = RenderedBadge.SvgNamespace;
        foreach (XElement text in badge.Svg.Root!.Descendants(svgNs + "text"))
        {
            double expectedTenths = text.Value == displayedSubject ? subjectTenths : statusTenths;
            ((string?)text.Attribute("textLength")).ShouldBe(expectedTenths.ToString(CultureInfo.InvariantCulture));
            ((string?)text.Parent!.Attribute("transform")).ShouldBe("scale(.1)");
        }

        badge.SubjectTextLengths.ShouldAllBe(length => Math.Abs(length - (badge.SubjectWidth - 8)) <= TextTolerance);
        badge.StatusTextLengths.ShouldAllBe(length => Math.Abs(length - (badge.StatusWidth - 8)) <= TextTolerance);
    }

    [TestMethod]
    [DataRow(Style.Flat)]
    [DataRow(Style.FlatSquare)]
    [DataRow(Style.Plastic)]
    public void DrawSVG_AnyStyle_AsksForVerdanaFirstAtElevenPixelsWithGeometricPrecision(Style style)
    {
        // Act
        string svg = BadgePainter.DrawSVG("IMM", "21 / 32", ColorScheme.YellowGreen, style);

        // Assert
        XElement textGroup = XDocument.Parse(svg).Root!
            .Elements(RenderedBadge.SvgNamespace + "g")
            .Single(g => g.Attribute("font-family") is not null);
        ((string?)textGroup.Attribute("font-family")).ShouldBe("Verdana,Geneva,DejaVu Sans,sans-serif");
        ((string?)textGroup.Attribute("text-rendering")).ShouldBe("geometricPrecision");

        // 110 user units, drawn at a scale of .1, is 11px.
        ((string?)textGroup.Attribute("font-size")).ShouldBe("110");
        ((string?)textGroup.Element(RenderedBadge.SvgNamespace + "g")!.Attribute("transform")).ShouldBe("scale(.1)");
    }

    [TestMethod]
    [DataRow(Style.Flat)]
    [DataRow(Style.FlatSquare)]
    [DataRow(Style.Plastic)]
    public void DrawSVG_HtmlEncodedTextWithMarkupCharactersAndQuotes_IsAnImageWhoseAccessibleNameIsTheDecodedText(Style style)
    {
        // Arrange
        const string Subject = "R&D <Tools> \"Quoted\" & 'Single'";
        string encodedSubject = System.Net.WebUtility.HtmlEncode(Subject);

        // Act
        string svg = BadgePainter.DrawSVG(encodedSubject, "67%", ColorScheme.YellowGreen, style);

        // Assert
        RenderedBadge badge = RenderedBadge.Parse(svg, Subject, "67%");
        badge.Svg.Root!.Attribute("role")!.Value.ShouldBe("img");
        badge.AriaLabel.ShouldBe(Subject + ": 67%");
        badge.Title.ShouldBe(Subject + ": 67%");
        badge.Texts.Distinct().ShouldBe([Subject, "67%"], ignoreOrder: true);
        svg.ShouldNotContain("&amp;amp;");
    }

    [TestMethod]
    [DataRow(Style.Flat, 1)]
    [DataRow(Style.FlatSquare, 0)]
    [DataRow(Style.Plastic, 1)]
    public void DrawSVG_Style_HidesOnlyTheShadowTextsFromAssistiveTechnology(Style style, int expectedShadowsPerText)
    {
        // Act
        string svg = BadgePainter.DrawSVG("IMM", "21 / 32", ColorScheme.YellowGreen, style);

        // Assert
        XElement[] texts = [.. XDocument.Parse(svg).Root!.Descendants(RenderedBadge.SvgNamespace + "text")];
        XElement[] hidden = [.. texts.Where(t => (string?)t.Attribute("aria-hidden") == "true")];
        hidden.Length.ShouldBe(2 * expectedShadowsPerText);
        hidden.ShouldAllBe(t => (string?)t.Attribute("fill") == "#010101");
        texts.Except(hidden).Select(t => t.Value).ShouldBe(["IMM", "21 / 32"]);
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
