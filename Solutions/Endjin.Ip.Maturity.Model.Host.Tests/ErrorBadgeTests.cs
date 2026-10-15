using System.Net;
using Endjin.Badger;
using Endjin.Imm.Caching;
using Endjin.Imm.Domain;
using Endjin.Ip.Maturity.Model.Host.Tests.TestData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Shouldly;
using YamlDotNet.Core;

namespace Endjin.Ip.Maturity.Model.Host.Tests;

/// <summary>
/// A badge in a README is an image, so when the badge cannot show a score it shows why instead,
/// as a light grey badge, rather than appearing as a broken image. Light grey is never used for a
/// score, so an error badge cannot be mistaken for one. Error badges are returned with status 200,
/// because image proxies such as GitHub's do not reliably show images returned with error statuses,
/// and the reason is also given, as a short code, in the X-Imm-Error header. Failures that are not
/// caused by the request or the content it names are not turned into badges.
/// </summary>
[TestClass]
public class ErrorBadgeTests
{
    private const string UnknownRuleId = "0b2c6a3e-58a4-4d2c-9a55-5c1f0f4f8f11";

    private const string ImmUrl = "https://raw.githubusercontent.com/endjin/Endjin.Ip.Maturity.Model/master/imm.yaml";

    private static readonly Uri RuleSetUrl = new(IpMaturityMatrixRuleset.DefinitionsUrlForName("master"));

    [TestMethod]
    public async Task GitHubImmTotalScore_ImmNotFound_ReturnsA200SvgBadgeSayingImmYamlNotFoundInLightGrey()
    {
        // Arrange
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);
        fixture.ImmFailsWith(new GitHubContentNotFoundException(new Uri(ImmUrl), HttpStatusCode.NotFound));
        HttpRequest request = ImmFunctionFixture.CreateRequest();

        // Act
        IActionResult result = await fixture.Functions.GitHubImmTotalScore(request, ImmFunctionFixture.Org, ImmFunctionFixture.Project);

        // Assert
        ShouldBeErrorBadge(result, request, "IMM", "imm.yaml not found", "imm-not-found");
    }

    [TestMethod]
    public async Task GitHubImmByRule_ImmNotFound_ReturnsA200SvgBadgeSayingImmYamlNotFound()
    {
        // Arrange
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);
        fixture.ImmFailsWith(new GitHubContentNotFoundException(new Uri(ImmUrl), HttpStatusCode.NotFound));
        HttpRequest request = ImmFunctionFixture.CreateRequest();

        // Act
        IActionResult result = await fixture.Functions.GitHubImmByRule(
            request, ImmFunctionFixture.Org, ImmFunctionFixture.Project, HostYaml.SharedEngineeringStandardsRuleId);

        // Assert
        // Without the IMM, the rule set still names the rule.
        ShouldBeErrorBadge(result, request, "Shared Engineering Standards", "imm.yaml not found", "imm-not-found");
    }

    [TestMethod]
    public async Task GitHubImmByRule_RuleSetAndImmNotFound_NamesTheBadgeGenerically()
    {
        // Arrange
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);
        fixture.RuleSetFailsWith(new GitHubContentNotFoundException(RuleSetUrl, HttpStatusCode.NotFound));
        fixture.ImmFailsWith(new GitHubContentNotFoundException(new Uri(ImmUrl), HttpStatusCode.NotFound));
        HttpRequest request = ImmFunctionFixture.CreateRequest();

        // Act
        IActionResult result = await fixture.Functions.GitHubImmByRule(
            request, ImmFunctionFixture.Org, ImmFunctionFixture.Project, HostYaml.SharedEngineeringStandardsRuleId);

        // Assert
        ShouldBeErrorBadge(result, request, "IMM rule", "rule set not found", "ruleset-not-found");
    }

    [TestMethod]
    public async Task GitHubImmTotalScore_RuleSetNotFound_ReturnsASvgBadgeSayingRuleSetNotFound()
    {
        // Arrange
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);
        fixture.RuleSetFailsWith(new GitHubContentNotFoundException(
            new Uri(IpMaturityMatrixRuleset.DefinitionsUrlForName("does-not-exist")), HttpStatusCode.NotFound));
        HttpRequest request = ImmFunctionFixture.CreateRequest("?definitionsBranch=does-not-exist");

        // Act
        IActionResult result = await fixture.Functions.GitHubImmTotalScore(request, ImmFunctionFixture.Org, ImmFunctionFixture.Project);

        // Assert
        ShouldBeErrorBadge(result, request, "IMM", "rule set not found", "ruleset-not-found");
    }

    [TestMethod]
    [DataRow(HttpStatusCode.ServiceUnavailable)]
    [DataRow(null)]
    public async Task GitHubImmTotalScore_GitHubUnavailable_ReturnsASvgBadgeSayingUnavailable(HttpStatusCode? statusCode)
    {
        // Arrange
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);
        GitHubContentUnavailableException unavailable = new(
            new Uri(ImmUrl), statusCode, statusCode is null ? new HttpRequestException("No such host is known.") : null);
        fixture.ImmFailsWith(unavailable);
        HttpRequest request = ImmFunctionFixture.CreateRequest();

        // Act
        IActionResult result = await fixture.Functions.GitHubImmTotalScore(request, ImmFunctionFixture.Org, ImmFunctionFixture.Project);

        // Assert
        ShouldBeErrorBadge(result, request, "IMM", "unavailable", "upstream-unavailable");
        (LogLevel level, string message, Exception? exception) = fixture.Logger.Entries.ShouldHaveSingleItem();
        level.ShouldBe(LogLevel.Warning);
        message.ShouldContain(ImmUrl);
        exception.ShouldBeSameAs(unavailable);
    }

    [TestMethod]
    public async Task GitHubImmTotalScore_MalformedImm_ReturnsASvgBadgeSayingInvalidImmYaml()
    {
        // Arrange
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);
        fixture.ImmFailsWith(new InvalidImmContentException(new Uri(ImmUrl), YamlErrorFrom("- Name: 'unterminated")));
        HttpRequest request = ImmFunctionFixture.CreateRequest();

        // Act
        IActionResult result = await fixture.Functions.GitHubImmTotalScore(request, ImmFunctionFixture.Org, ImmFunctionFixture.Project);

        // Assert
        ShouldBeErrorBadge(result, request, "IMM", "invalid imm.yaml", "invalid-imm");
    }

    [TestMethod]
    public async Task GitHubImmTotalScore_ImmAssertingTwoLevelsOfADiscreteRule_ReturnsASvgBadgeSayingInvalidImmYaml()
    {
        // Arrange
        const string immYaml = $"""
            -
                Name: 'Shared Engineering Standards'
                Id: '{HostYaml.SharedEngineeringStandardsRuleId}'
                Measures:
                    -
                        Score: 0
                        Description: None
                    -
                        Score: 1
                        Description: Configured
            """;
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, immYaml);
        HttpRequest request = ImmFunctionFixture.CreateRequest();

        // Act
        IActionResult result = await fixture.Functions.GitHubImmTotalScore(request, ImmFunctionFixture.Org, ImmFunctionFixture.Project);

        // Assert
        ShouldBeErrorBadge(result, request, "IMM", "invalid imm.yaml", "invalid-imm");
    }

    [TestMethod]
    public async Task GitHubImmTotalScore_ImmRuleNotInRuleSet_ReturnsASvgBadgeSayingUnknownRule()
    {
        // Arrange
        const string immYaml = $"""
            -
                Name: 'A rule from a newer rule set'
                Id: '{UnknownRuleId}'
                Measures:
                    -
                        Score: 1
            """;
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, immYaml);
        HttpRequest request = ImmFunctionFixture.CreateRequest();

        // Act
        IActionResult result = await fixture.Functions.GitHubImmTotalScore(request, ImmFunctionFixture.Org, ImmFunctionFixture.Project);

        // Assert
        ShouldBeErrorBadge(result, request, "IMM", "unknown rule", "unknown-rule");
    }

    [TestMethod]
    [DataRow("not-a-guid")]
    [DataRow("74e29f9b")]
    public async Task GitHubImmByRule_RuleIdNotAGuid_ReturnsASvgBadgeSayingInvalidRuleId(string ruleId)
    {
        // Arrange
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);
        HttpRequest request = ImmFunctionFixture.CreateRequest();

        // Act
        IActionResult result = await fixture.Functions.GitHubImmByRule(request, ImmFunctionFixture.Org, ImmFunctionFixture.Project, ruleId);

        // Assert
        ShouldBeErrorBadge(result, request, "IMM rule", "invalid rule id", "invalid-rule-id");
    }

    [TestMethod]
    public async Task GitHubImmByRule_RuleIdNotInImm_ReturnsASvgBadgeSayingRuleNotFound()
    {
        // Arrange
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);
        HttpRequest request = ImmFunctionFixture.CreateRequest();

        // Act
        IActionResult result = await fixture.Functions.GitHubImmByRule(request, ImmFunctionFixture.Org, ImmFunctionFixture.Project, UnknownRuleId);

        // Assert
        ShouldBeErrorBadge(result, request, "IMM rule", "rule not found", "rule-not-found");
    }

    [TestMethod]
    public async Task GitHubImmByRule_OptedOutRule_ReturnsASvgBadgeSayingOptedOut()
    {
        // Arrange
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.DeploymentOptedOut);
        HttpRequest request = ImmFunctionFixture.CreateRequest();

        // Act
        IActionResult result = await fixture.Functions.GitHubImmByRule(request, ImmFunctionFixture.Org, ImmFunctionFixture.Project, HostYaml.DeploymentRuleId);

        // Assert
        ShouldBeErrorBadge(result, request, "Deployment", "opted out", "rule-opted-out");
    }

    [TestMethod]
    public async Task GitHubImmTotalScore_ErrorBadge_TellsClientsAndProxiesNotToCacheTheBadge()
    {
        // Arrange
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);
        fixture.ImmFailsWith(new GitHubContentNotFoundException(new Uri(ImmUrl), HttpStatusCode.NotFound));
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
    public async Task GitHubImmTotalScore_ScoredImm_DoesNotSetTheImmErrorHeader()
    {
        // Arrange
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);
        HttpRequest request = ImmFunctionFixture.CreateRequest();

        // Act
        await fixture.Functions.GitHubImmTotalScore(request, ImmFunctionFixture.Org, ImmFunctionFixture.Project);

        // Assert
        request.HttpContext.Response.Headers.ContainsKey("X-Imm-Error").ShouldBeFalse();
        fixture.Logger.Entries.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GitHubImmTotalScore_ImmNotFound_LogsAtInformationWithTheUrl()
    {
        // Arrange
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);
        fixture.ImmFailsWith(new GitHubContentNotFoundException(new Uri(ImmUrl), HttpStatusCode.NotFound));

        // Act
        await fixture.Functions.GitHubImmTotalScore(ImmFunctionFixture.CreateRequest(), ImmFunctionFixture.Org, ImmFunctionFixture.Project);

        // Assert
        (LogLevel level, string message, Exception? exception) = fixture.Logger.Entries.ShouldHaveSingleItem();
        level.ShouldBe(LogLevel.Information);
        message.ShouldContain("imm-not-found");
        message.ShouldContain(ImmUrl);
        exception.ShouldBeNull();
    }

    [TestMethod]
    public async Task GitHubImmTotalScore_UnexpectedException_PropagatesRatherThanRenderingABadge()
    {
        // Arrange
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);
        fixture.ImmFailsWith(new InvalidOperationException("A bug"));

        // Act
        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(
            () => fixture.Functions.GitHubImmTotalScore(ImmFunctionFixture.CreateRequest(), ImmFunctionFixture.Org, ImmFunctionFixture.Project));

        // Assert
        exception.Message.ShouldBe("A bug");
    }

    // The rule set is ours, so a rule set that is not valid is a fault in the service, not in the request.
    [TestMethod]
    public async Task GitHubImmTotalScore_MalformedRuleSet_PropagatesRatherThanRenderingABadge()
    {
        // Arrange
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);
        InvalidImmContentException invalidRuleSet = new(RuleSetUrl, YamlErrorFrom("- Name: 'unterminated"));
        fixture.RuleSetFailsWith(invalidRuleSet);

        // Act
        InvalidImmContentException exception = await Should.ThrowAsync<InvalidImmContentException>(
            () => fixture.Functions.GitHubImmTotalScore(ImmFunctionFixture.CreateRequest(), ImmFunctionFixture.Org, ImmFunctionFixture.Project));

        // Assert
        exception.ShouldBeSameAs(invalidRuleSet);
    }

    [TestMethod]
    public void RenderErrorBadge_ImmNotFound_RendersExactlyTheExpectedSvg()
    {
        // Act
        string svg = ImmBadges.RenderErrorBadge("IMM", "imm.yaml not found");

        // Assert
        svg.ReplaceLineEndings("\n").TrimEnd('\n').ShouldBe(
            """
            <svg xmlns="http://www.w3.org/2000/svg" width="151.2334" height="20" role="img" aria-label="IMM: imm.yaml not found">
              <title>IMM: imm.yaml not found</title>
              <linearGradient id="b" x2="0" y2="100%">
                <stop offset="0" stop-color="#bbb" stop-opacity=".1"/>
                <stop offset="1" stop-opacity=".1"/>
              </linearGradient>
              <mask id="a">
                <rect width="151.2334" height="20" rx="3" fill="#fff"/>
              </mask>
              <g mask="url(#a)">
                <path fill="#555" d="M0 0h31.170898v20H0z"/>
                <path fill="#9f9f9f" d="M31.170898 0h120.0625v20H31.170898z"/>
                <path fill="url(#b)" d="M0 0h151.2334v20H0z"/>
              </g>
              <g fill="#fff" text-anchor="middle" font-family="Verdana,Geneva,DejaVu Sans,sans-serif" text-rendering="geometricPrecision" font-size="110">
                <g transform="scale(.1)">
                  <text aria-hidden="true" x="156" y="150" fill="#010101" fill-opacity=".3" textLength="232">IMM</text>
                  <text x="156" y="140" textLength="232">IMM</text>
                  <text aria-hidden="true" x="912" y="150" fill="#010101" fill-opacity=".3" textLength="1121">imm.yaml not found</text>
                  <text x="912" y="140" textLength="1121">imm.yaml not found</text>
                </g>
              </g>
            </svg>
            """);
    }

    private static YamlException YamlErrorFrom(string yaml)
    {
        try
        {
            IpMaturityMatrix.FromYaml(yaml);
        }
        catch (YamlException ex)
        {
            return ex;
        }

        throw new InvalidOperationException("The YAML was expected not to load.");
    }

    private static void ShouldBeErrorBadge(IActionResult result, HttpRequest request, string subject, string status, string errorCode)
    {
        ContentResult content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(StatusCodes.Status200OK);
        content.ContentType.ShouldBe("image/svg+xml");
        BadgeSvg badge = BadgeSvg.Parse(content.Content!);
        badge.Subject.ShouldBe(subject);
        badge.Status.ShouldBe(status);
        badge.StatusColour.ShouldBe(ColorScheme.LightGrey);
        badge.AriaLabel.ShouldBe($"{subject}: {status}");
        request.HttpContext.Response.Headers["X-Imm-Error"].ToString().ShouldBe(errorCode);
    }
}
