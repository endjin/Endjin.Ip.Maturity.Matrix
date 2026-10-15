using System.Net;
using System.Text.RegularExpressions;
using Endjin.Ip.Maturity.Matrix.Host.Tests.TestData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Shouldly;

namespace Endjin.Ip.Maturity.Matrix.Host.Tests;

/// <summary>
/// The showbadges page lets a project's maintainers preview all of its IMM badges at once: the
/// total score, then one badge for each rule the project has not opted out of. The page title
/// names the project and branch, and every badge is requested from the same branches as the page.
/// </summary>
[TestClass]
public partial class ShowBadgesPageTests
{
    [TestMethod]
    public async Task GitHubImmAllBadges_DefaultBranches_IsAnUncachedHtmlPageTitledWithTheProjectAndBranch()
    {
        // Arrange
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);
        HttpRequest request = ImmFunctionFixture.CreateRequest();

        // Act
        IActionResult result = await fixture.Functions.GitHubImmAllBadges(request, ImmFunctionFixture.Org, ImmFunctionFixture.Project);

        // Assert
        ContentResult content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(StatusCodes.Status200OK);
        content.ContentType.ShouldBe("text/html");
        Title(content.Content!).ShouldBe("IP Maturity Matrix for endjin/Endjin.Ip.Maturity.Matrix (master)");
        request.HttpContext.Response.Headers.CacheControl.ToString().ShouldBe("no-cache, no-store, must-revalidate");
    }

    [TestMethod]
    [DataRow("?projectBranch=feature/x", "IP Maturity Matrix for endjin/Endjin.Ip.Maturity.Matrix (feature/x)")]
    [DataRow("?definitionsBranch=master&projectBranch=feature/x", "IP Maturity Matrix for endjin/Endjin.Ip.Maturity.Matrix (feature/x)")]
    [DataRow("?definitionsBranch=v2", "IP Maturity Matrix for endjin/Endjin.Ip.Maturity.Matrix (master, rules: v2)")]
    [DataRow("?definitionsBranch=v2&projectBranch=feature/x", "IP Maturity Matrix for endjin/Endjin.Ip.Maturity.Matrix (feature/x, rules: v2)")]
    public async Task GitHubImmAllBadges_BranchQueryString_TitleShowsTheProjectBranchAndAnyNonDefaultRuleSetBranch(
        string queryString,
        string expectedTitle)
    {
        // Arrange
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);

        // Act
        IActionResult result = await fixture.Functions.GitHubImmAllBadges(
            ImmFunctionFixture.CreateRequest(queryString), ImmFunctionFixture.Org, ImmFunctionFixture.Project);

        // Assert
        Title(result.ShouldBeOfType<ContentResult>().Content!).ShouldBe(expectedTitle);
    }

    [TestMethod]
    [DataRow("?projectBranch=</title><script>alert(1)</script>")]
    [DataRow("?definitionsBranch=</title><script>alert(1)</script>")]
    public async Task GitHubImmAllBadges_BranchContainingMarkup_IsShownAsTextNotInterpretedAsHtml(string queryString)
    {
        // Arrange
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);

        // Act
        IActionResult result = await fixture.Functions.GitHubImmAllBadges(
            ImmFunctionFixture.CreateRequest(queryString), ImmFunctionFixture.Org, ImmFunctionFixture.Project);

        // Assert
        string html = result.ShouldBeOfType<ContentResult>().Content!;
        html.ShouldNotContain("<script>");
        Title(html).ShouldContain("&lt;/title&gt;&lt;script&gt;alert(1)&lt;/script&gt;");
    }

    [TestMethod]
    [DataRow("", "master", "master")]
    [DataRow("?definitionsBranch=v2&projectBranch=feature/x", "v2", "feature%2Fx")]
    [DataRow("?projectBranch=release%201.0", "master", "release%201.0")]
    [DataRow("?projectBranch=a%26b%3Dc", "master", "a%26b%3Dc")]
    public async Task GitHubImmAllBadges_ScoredRules_ShowsTheTotalThenEachRuleBadgeFromTheSameBranches(
        string queryString,
        string expectedEncodedDefinitionsBranch,
        string expectedEncodedProjectBranch)
    {
        // Arrange
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);
        string branches = $"?cache=false&definitionsBranch={expectedEncodedDefinitionsBranch}&projectBranch={expectedEncodedProjectBranch}";

        // Act
        IActionResult result = await fixture.Functions.GitHubImmAllBadges(
            ImmFunctionFixture.CreateRequest(queryString), ImmFunctionFixture.Org, ImmFunctionFixture.Project);

        // Assert
        ImageSources(result.ShouldBeOfType<ContentResult>().Content!).ShouldBe(
        [
            $"/api/imm/github/endjin/Endjin.Ip.Maturity.Matrix/total{branches}",
            $"/api/imm/github/endjin/Endjin.Ip.Maturity.Matrix/rule/{HostYaml.DeploymentRuleId}{branches}",
            $"/api/imm/github/endjin/Endjin.Ip.Maturity.Matrix/rule/{HostYaml.SharedEngineeringStandardsRuleId}{branches}",
        ]);
    }

    [TestMethod]
    [DataRow("my org", "my project", "my%20org", "my%20project")]
    [DataRow("endjin", "x\" onerror=\"alert(1)", "endjin", "x%22%20onerror%3D%22alert%281%29")]
    [DataRow("a<b>&c", "d/e?f#g", "a%3Cb%3E%26c", "d%2Fe%3Ff%23g")]
    public async Task GitHubImmAllBadges_OrgOrProjectNeedingEncoding_IsUrlEncodedInEachBadgeUrlAndCannotBreakOutOfTheAttribute(
        string org,
        string project,
        string expectedEncodedOrg,
        string expectedEncodedProject)
    {
        // Arrange
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);
        string badges = $"/api/imm/github/{expectedEncodedOrg}/{expectedEncodedProject}";
        const string branches = "?cache=false&definitionsBranch=master&projectBranch=master";

        // Act
        IActionResult result = await fixture.Functions.GitHubImmAllBadges(ImmFunctionFixture.CreateRequest(), org, project);

        // Assert
        string html = result.ShouldBeOfType<ContentResult>().Content!;
        ImageSources(html).ShouldBe(
        [
            $"{badges}/total{branches}",
            $"{badges}/rule/{HostYaml.DeploymentRuleId}{branches}",
            $"{badges}/rule/{HostYaml.SharedEngineeringStandardsRuleId}{branches}",
        ]);
        List<string> imageElements = [.. ImageElementPattern().Matches(html).Select(m => m.Value)];
        imageElements.Count.ShouldBe(3);
        imageElements.ShouldAllBe(img => WellFormedImageElementPattern().IsMatch(img));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("?definitionsBranch=v2&projectBranch=feature/x")]
    public async Task GitHubImmAllBadges_BadgeUrlQueryStrings_SeparateParametersWithHtmlEncodedAmpersands(string queryString)
    {
        // Arrange
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);

        // Act
        IActionResult result = await fixture.Functions.GitHubImmAllBadges(
            ImmFunctionFixture.CreateRequest(queryString), ImmFunctionFixture.Org, ImmFunctionFixture.Project);

        // Assert
        List<string> rawSources = RawImageSources(result.ShouldBeOfType<ContentResult>().Content!);
        rawSources.Count.ShouldBe(3);
        rawSources.ShouldAllBe(src => src.Contains("?cache=false&amp;definitionsBranch=") && src.Contains("&amp;projectBranch="));
        rawSources.ShouldAllBe(src => !BareAmpersandPattern().IsMatch(src));
    }

    [TestMethod]
    public async Task GitHubImmAllBadges_OptedOutRule_HasNoRuleBadge()
    {
        // Arrange
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.DeploymentOptedOut);

        // Act
        IActionResult result = await fixture.Functions.GitHubImmAllBadges(
            ImmFunctionFixture.CreateRequest(), ImmFunctionFixture.Org, ImmFunctionFixture.Project);

        // Assert
        ImageSources(result.ShouldBeOfType<ContentResult>().Content!).ShouldBe(
        [
            "/api/imm/github/endjin/Endjin.Ip.Maturity.Matrix/total?cache=false&definitionsBranch=master&projectBranch=master",
            $"/api/imm/github/endjin/Endjin.Ip.Maturity.Matrix/rule/{HostYaml.SharedEngineeringStandardsRuleId}?cache=false&definitionsBranch=master&projectBranch=master",
        ]);
    }

    [TestMethod]
    public async Task GitHubImmAllBadges_BranchQueryString_ReadsTheRuleSetAndImmFromThoseBranches()
    {
        // Arrange
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);

        // Act
        await fixture.Functions.GitHubImmAllBadges(
            ImmFunctionFixture.CreateRequest("?definitionsBranch=v2&projectBranch=feature/x"),
            ImmFunctionFixture.Org,
            ImmFunctionFixture.Project);

        // Assert
        await fixture.RuleDefinitionSource.Received(1).GetRuleDefinitionRepositoryAsync("v2");
        await fixture.ImmSource.Received(1).GetIpMaturityMatrixAsync(ImmFunctionFixture.Org, ImmFunctionFixture.Project, "feature/x");
    }

    private static string Title(string html) => TitlePattern().Match(html).Groups["title"].Value;

    // The URLs the browser requests: the src attribute values with HTML character references decoded.
    private static List<string> ImageSources(string html) =>
        [.. RawImageSources(html).Select(src => WebUtility.HtmlDecode(src))];

    // The src attribute values exactly as they appear in the HTML.
    private static List<string> RawImageSources(string html) =>
        [.. ImageSourcePattern().Matches(html).Select(m => m.Groups["src"].Value)];

    [GeneratedRegex("<title>(?<title>.*?)</title>", RegexOptions.Singleline)]
    private static partial Regex TitlePattern();

    [GeneratedRegex("<img src=\"(?<src>[^\"]*)\"")]
    private static partial Regex ImageSourcePattern();

    // An img element, up to the first '>'.
    [GeneratedRegex("<img\\b[^>]*>")]
    private static partial Regex ImageElementPattern();

    // An img element with a src attribute and nothing else, its value containing no markup.
    [GeneratedRegex("^<img src=\"[^\"<>]*\" />$")]
    private static partial Regex WellFormedImageElementPattern();

    // An ampersand that does not start a character reference.
    [GeneratedRegex("&(?!amp;|lt;|gt;|quot;|#)")]
    private static partial Regex BareAmpersandPattern();
}
