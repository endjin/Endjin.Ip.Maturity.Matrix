using Endjin.Badger;
using Endjin.Ip.Maturity.Model.Host.Tests.TestData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Shouldly;

namespace Endjin.Ip.Maturity.Model.Host.Tests;

/// <summary>
/// A project's README can show badges for its overall IMM score and for each rule. The badges are
/// coloured by how much of the maximum possible score the project has achieved: red below 33%,
/// yellow below 66%, yellow-green below 100%, and green for full marks. They are never cached,
/// so that changes to the IMM show up as soon as GitHub serves them. By default, the rule set is read
/// from its main branch and the IMM from the project's master branch; a query string can pick other branches.
/// </summary>
[TestClass]
public class BadgeEndpointTests
{
    [TestMethod]
    public async Task GitHubImmTotalScore_ScoredImm_ReturnsAnSvgBadgeShowingTheScoreOutOfTheMaximum()
    {
        // Arrange
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);
        HttpRequest request = ImmFunctionFixture.CreateRequest();

        // Act
        IActionResult result = await fixture.Functions.GitHubImmTotalScore(request, ImmFunctionFixture.Org, ImmFunctionFixture.Project);

        // Assert
        ContentResult content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(StatusCodes.Status200OK);
        content.ContentType.ShouldBe("image/svg+xml");
        BadgeSvg badge = BadgeSvg.Parse(content.Content!);
        badge.Subject.ShouldBe("IMM");
        badge.Status.ShouldBe("3 / 3");
    }

    [TestMethod]
    public async Task GitHubImmTotalScore_OptedOutRule_IsExcludedFromTheMaximum()
    {
        // Arrange
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.DeploymentOptedOut);

        // Act
        IActionResult result = await fixture.Functions.GitHubImmTotalScore(
            ImmFunctionFixture.CreateRequest(), ImmFunctionFixture.Org, ImmFunctionFixture.Project);

        // Assert
        BadgeSvg badge = BadgeSvg.Parse(result.ShouldBeOfType<ContentResult>().Content!);
        badge.Status.ShouldBe("1 / 1");
        badge.StatusColour.ShouldBe(ColorScheme.Green);
    }

    [TestMethod]
    [DataRow(0, ColorScheme.Red)]
    [DataRow(32, ColorScheme.Red)]
    [DataRow(33, ColorScheme.Yellow)]
    [DataRow(65, ColorScheme.Yellow)]
    [DataRow(66, ColorScheme.YellowGreen)]
    [DataRow(99, ColorScheme.YellowGreen)]
    [DataRow(100, ColorScheme.Green)]
    public async Task GitHubImmTotalScore_PercentageOfMaximum_ColoursTheBadgeByBand(int percentage, string expectedColour)
    {
        // Arrange
        (string ruleSetYaml, string immYaml) = HostYaml.Scoring(percentage);
        ImmFunctionFixture fixture = new(ruleSetYaml, immYaml);

        // Act
        IActionResult result = await fixture.Functions.GitHubImmTotalScore(
            ImmFunctionFixture.CreateRequest(), ImmFunctionFixture.Org, ImmFunctionFixture.Project);

        // Assert
        BadgeSvg badge = BadgeSvg.Parse(result.ShouldBeOfType<ContentResult>().Content!);
        badge.Status.ShouldBe($"{percentage} / 100");
        badge.StatusColour.ShouldBe(expectedColour);
    }

    [TestMethod]
    public async Task GitHubImmTotalScore_EveryRuleOptedOut_ShowsZeroOutOfZeroInRed()
    {
        // Arrange
        ImmFunctionFixture fixture = new(HostYaml.OptionalRuleOnly, HostYaml.OptionalRuleOptedOut);

        // Act
        IActionResult result = await fixture.Functions.GitHubImmTotalScore(
            ImmFunctionFixture.CreateRequest(), ImmFunctionFixture.Org, ImmFunctionFixture.Project);

        // Assert
        BadgeSvg badge = BadgeSvg.Parse(result.ShouldBeOfType<ContentResult>().Content!);
        badge.Subject.ShouldBe("IMM");
        badge.Status.ShouldBe("0 / 0");
        badge.StatusColour.ShouldBe(ColorScheme.Red);
    }

    [TestMethod]
    [DataRow(0, ColorScheme.Red)]
    [DataRow(32, ColorScheme.Red)]
    [DataRow(33, ColorScheme.Yellow)]
    [DataRow(65, ColorScheme.Yellow)]
    [DataRow(66, ColorScheme.YellowGreen)]
    [DataRow(99, ColorScheme.YellowGreen)]
    [DataRow(100, ColorScheme.Green)]
    public async Task GitHubImmByRule_RulePercentage_ShowsThePercentageColouredByBand(int percentage, string expectedColour)
    {
        // Arrange
        (string ruleSetYaml, string immYaml) = HostYaml.Scoring(percentage);
        ImmFunctionFixture fixture = new(ruleSetYaml, immYaml);

        // Act
        IActionResult result = await fixture.Functions.GitHubImmByRule(
            ImmFunctionFixture.CreateRequest(), ImmFunctionFixture.Org, ImmFunctionFixture.Project, HostYaml.ScoredRuleId);

        // Assert
        ContentResult content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(StatusCodes.Status200OK);
        content.ContentType.ShouldBe("image/svg+xml");
        BadgeSvg badge = BadgeSvg.Parse(content.Content!);
        badge.Subject.ShouldBe("Scored Rule");
        badge.Status.ShouldBe($"{percentage}%");
        badge.StatusColour.ShouldBe(expectedColour);
    }

    [TestMethod]
    [DataRow("Shared Engineering Standards")]
    [DataRow("R&D <Tooling>")]
    [DataRow("Owner's \"Sign-off\"")]
    public async Task GitHubImmByRule_RuleNameWithMarkupCharacters_ShowsTheRuleNameAsWritten(string ruleName)
    {
        // Arrange
        // In single-quoted YAML a quote is written by doubling it.
        (string ruleSetYaml, string immYaml) = HostYaml.Scoring(67, ruleName.Replace("'", "''", StringComparison.Ordinal));
        ImmFunctionFixture fixture = new(ruleSetYaml, immYaml);

        // Act
        IActionResult result = await fixture.Functions.GitHubImmByRule(
            ImmFunctionFixture.CreateRequest(), ImmFunctionFixture.Org, ImmFunctionFixture.Project, HostYaml.ScoredRuleId);

        // Assert
        BadgeSvg badge = BadgeSvg.Parse(result.ShouldBeOfType<ContentResult>().Content!);
        badge.Subject.ShouldBe(ruleName);
        badge.Status.ShouldBe("67%");
    }

    [TestMethod]
    [DataRow("Shared Engineering Standards")]
    [DataRow("R&D <Tooling>")]
    [DataRow("Owner's \"Sign-off\"")]
    public async Task GitHubImmByRule_RuleNameWithMarkupCharacters_GivesTheBadgeTheRuleNameAndScoreAsItsAccessibleName(string ruleName)
    {
        // Arrange
        (string ruleSetYaml, string immYaml) = HostYaml.Scoring(67, ruleName.Replace("'", "''", StringComparison.Ordinal));
        ImmFunctionFixture fixture = new(ruleSetYaml, immYaml);

        // Act
        IActionResult result = await fixture.Functions.GitHubImmByRule(
            ImmFunctionFixture.CreateRequest(), ImmFunctionFixture.Org, ImmFunctionFixture.Project, HostYaml.ScoredRuleId);

        // Assert
        string svg = result.ShouldBeOfType<ContentResult>().Content!;
        BadgeSvg badge = BadgeSvg.Parse(svg);
        badge.Title.ShouldBe($"{ruleName}: 67%");
        badge.AriaLabel.ShouldBe($"{ruleName}: 67%");
        svg.ShouldNotContain("&amp;amp;");
    }

    [TestMethod]
    public async Task GitHubImmByRule_ImmRuleWithoutAName_ShowsTheRuleSetsNameForTheRule()
    {
        // Arrange
        const string immYaml = $"""
            -
                Id: '{HostYaml.SharedEngineeringStandardsRuleId}'
                Measures:
                    -
                        Score: 1
                        Description: Configured
            """;
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, immYaml);

        // Act
        IActionResult result = await fixture.Functions.GitHubImmByRule(
            ImmFunctionFixture.CreateRequest(), ImmFunctionFixture.Org, ImmFunctionFixture.Project, HostYaml.SharedEngineeringStandardsRuleId);

        // Assert
        ContentResult content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(StatusCodes.Status200OK);
        BadgeSvg badge = BadgeSvg.Parse(content.Content!);
        badge.Subject.ShouldBe("Shared Engineering Standards");
        badge.Status.ShouldBe("100%");
    }

    [TestMethod]
    public async Task GitHubImmTotalScore_AnyRequest_TellsClientsAndProxiesNotToCacheTheBadge()
    {
        // Arrange
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);
        HttpRequest request = ImmFunctionFixture.CreateRequest();

        // Act
        await fixture.Functions.GitHubImmTotalScore(request, ImmFunctionFixture.Org, ImmFunctionFixture.Project);

        // Assert
        IHeaderDictionary headers = request.HttpContext.Response.Headers;
        headers.CacheControl.ToString().ShouldBe("no-cache, no-store, must-revalidate");
        headers.Pragma.ToString().ShouldBe("no-cache");
        headers.Expires.ToString().ShouldBe("-1");
        headers.ETag.ToString().ShouldMatch("^\"[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\"$");
    }

    [TestMethod]
    public async Task GitHubImmByRule_AnyRequest_TellsClientsAndProxiesNotToCacheTheBadge()
    {
        // Arrange
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);
        HttpRequest request = ImmFunctionFixture.CreateRequest();

        // Act
        await fixture.Functions.GitHubImmByRule(request, ImmFunctionFixture.Org, ImmFunctionFixture.Project, HostYaml.DeploymentRuleId);

        // Assert
        IHeaderDictionary headers = request.HttpContext.Response.Headers;
        headers.CacheControl.ToString().ShouldBe("no-cache, no-store, must-revalidate");
        headers.Pragma.ToString().ShouldBe("no-cache");
        headers.Expires.ToString().ShouldBe("-1");
    }

    [TestMethod]
    public async Task GitHubImmTotalScore_RepeatedRequests_GiveEachResponseADifferentETag()
    {
        // Arrange
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);
        HttpRequest firstRequest = ImmFunctionFixture.CreateRequest();
        HttpRequest secondRequest = ImmFunctionFixture.CreateRequest();

        // Act
        await fixture.Functions.GitHubImmTotalScore(firstRequest, ImmFunctionFixture.Org, ImmFunctionFixture.Project);
        await fixture.Functions.GitHubImmTotalScore(secondRequest, ImmFunctionFixture.Org, ImmFunctionFixture.Project);

        // Assert
        secondRequest.HttpContext.Response.Headers.ETag.ToString()
            .ShouldNotBe(firstRequest.HttpContext.Response.Headers.ETag.ToString());
    }

    [TestMethod]
    [DataRow("", "main", "master")]
    [DataRow("?projectBranch=", "main", "master")]
    [DataRow("?definitionsBranch=&projectBranch=", "main", "master")]
    [DataRow("?projectBranch=feature/x", "main", "feature/x")]
    [DataRow("?projectBranch=feature%2Fx", "main", "feature/x")]
    [DataRow("?definitionsBranch=v2", "v2", "master")]
    [DataRow("?definitionsBranch=v2&projectBranch=release/1.0", "v2", "release/1.0")]
    [DataRow("?projectBranch=main&projectBranch=dev", "main", "main")]
    [DataRow("?definitionsBranch=v2&definitionsBranch=v3&projectBranch=main", "v2", "main")]
    public async Task GitHubImmTotalScore_BranchQueryString_ReadsTheRuleSetAndImmFromThoseBranches(
        string queryString,
        string expectedDefinitionsBranch,
        string expectedProjectBranch)
    {
        // Arrange
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);

        // Act
        await fixture.Functions.GitHubImmTotalScore(
            ImmFunctionFixture.CreateRequest(queryString), ImmFunctionFixture.Org, ImmFunctionFixture.Project);

        // Assert
        await fixture.RuleDefinitionSource.Received(1).GetRuleDefinitionRepositoryAsync(expectedDefinitionsBranch);
        await fixture.ImmSource.Received(1).GetIpMaturityModelAsync(ImmFunctionFixture.Org, ImmFunctionFixture.Project, expectedProjectBranch);
    }

    [TestMethod]
    [DataRow("", "main", "master")]
    [DataRow("?definitionsBranch=v2&projectBranch=feature/x", "v2", "feature/x")]
    public async Task GitHubImmByRule_BranchQueryString_ReadsTheRuleSetAndImmFromThoseBranches(
        string queryString,
        string expectedDefinitionsBranch,
        string expectedProjectBranch)
    {
        // Arrange
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);

        // Act
        await fixture.Functions.GitHubImmByRule(
            ImmFunctionFixture.CreateRequest(queryString), ImmFunctionFixture.Org, ImmFunctionFixture.Project, HostYaml.DeploymentRuleId);

        // Assert
        await fixture.RuleDefinitionSource.Received(1).GetRuleDefinitionRepositoryAsync(expectedDefinitionsBranch);
        await fixture.ImmSource.Received(1).GetIpMaturityModelAsync(ImmFunctionFixture.Org, ImmFunctionFixture.Project, expectedProjectBranch);
    }
}
