using System.Net;
using Endjin.Imm.Caching;
using Endjin.Imm.Domain;
using Endjin.Ip.Maturity.Model.Host.Tests.TestData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using YamlDotNet.Core;

namespace Endjin.Ip.Maturity.Model.Host.Tests;

/// <summary>
/// When the showbadges page cannot show a project's badges, it shows the error badge and a short page saying why, with
/// a status code to match: 404 when the IMM or rule set does not exist, 422 when the IMM is not
/// valid, and 502 when GitHub is unavailable. The page names the URL requested from GitHub where
/// that helps, and suggests what to try next. Like the badges, it is never cached, and the
/// X-Imm-Error header identifies the problem.
/// </summary>
[TestClass]
public class ShowBadgesErrorPageTests
{
    private const string ImmUrl = "https://raw.githubusercontent.com/endjin/Endjin.Ip.Maturity.Model/master/imm.yaml";

    [TestMethod]
    public async Task GitHubImmAllBadges_ImmNotFound_Returns404HtmlPageNamingTheUrlAndSuggestingProjectBranch()
    {
        // Arrange
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);
        fixture.ImmFailsWith(new GitHubContentNotFoundException(new Uri(ImmUrl), HttpStatusCode.NotFound));
        HttpRequest request = ImmFunctionFixture.CreateRequest();

        // Act
        IActionResult result = await fixture.Functions.GitHubImmAllBadges(request, ImmFunctionFixture.Org, ImmFunctionFixture.Project);

        // Assert
        ContentResult content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        content.ContentType.ShouldBe("text/html");
        content.Content!.ShouldContain(ImmUrl);
        content.Content!.ShouldContain("?projectBranch=main");
        content.Content!.ShouldContain(ImmBadges.RenderErrorBadge("IMM", "imm.yaml not found"));
        IHeaderDictionary headers = request.HttpContext.Response.Headers;
        headers["X-Imm-Error"].ToString().ShouldBe("imm-not-found");
        headers.CacheControl.ToString().ShouldBe("no-cache, no-store, must-revalidate");
    }

    [TestMethod]
    public async Task GitHubImmAllBadges_RuleSetNotFound_Returns404HtmlPageNamingTheDefinitionsBranch()
    {
        // Arrange
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);
        string ruleSetUrl = IpMaturityMatrixRuleset.DefinitionsUrlForName("does-not-exist");
        fixture.RuleSetFailsWith(new GitHubContentNotFoundException(new Uri(ruleSetUrl), HttpStatusCode.NotFound));
        HttpRequest request = ImmFunctionFixture.CreateRequest("?definitionsBranch=does-not-exist");

        // Act
        IActionResult result = await fixture.Functions.GitHubImmAllBadges(request, ImmFunctionFixture.Org, ImmFunctionFixture.Project);

        // Assert
        ContentResult content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        content.Content!.ShouldContain(ruleSetUrl);
        content.Content!.ShouldContain("definitionsBranch");
        request.HttpContext.Response.Headers["X-Imm-Error"].ToString().ShouldBe("ruleset-not-found");
    }

    [TestMethod]
    public async Task GitHubImmAllBadges_GitHubUnavailable_Returns502HtmlPage()
    {
        // Arrange
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);
        fixture.ImmFailsWith(new GitHubContentUnavailableException(new Uri(ImmUrl), HttpStatusCode.ServiceUnavailable));
        HttpRequest request = ImmFunctionFixture.CreateRequest();

        // Act
        IActionResult result = await fixture.Functions.GitHubImmAllBadges(request, ImmFunctionFixture.Org, ImmFunctionFixture.Project);

        // Assert
        ContentResult content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(StatusCodes.Status502BadGateway);
        content.ContentType.ShouldBe("text/html");
        content.Content!.ShouldContain("503");
        content.Content!.ShouldContain(ImmBadges.RenderErrorBadge("IMM", "unavailable"));
        request.HttpContext.Response.Headers["X-Imm-Error"].ToString().ShouldBe("upstream-unavailable");
    }

    [TestMethod]
    public async Task GitHubImmAllBadges_MalformedImm_Returns422HtmlPageWithTheYamlErrorPosition()
    {
        // Arrange
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);
        YamlException yamlError = Should.Throw<YamlException>(() => IpMaturityMatrix.FromYaml("-\n    Name: 'unterminated"));
        fixture.ImmFailsWith(new InvalidImmContentException(new Uri(ImmUrl), yamlError));
        HttpRequest request = ImmFunctionFixture.CreateRequest();

        // Act
        IActionResult result = await fixture.Functions.GitHubImmAllBadges(request, ImmFunctionFixture.Org, ImmFunctionFixture.Project);

        // Assert
        ContentResult content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(StatusCodes.Status422UnprocessableEntity);
        content.Content!.ShouldContain($"(line {yamlError.Start.Line}, column {yamlError.Start.Column})");
        request.HttpContext.Response.Headers["X-Imm-Error"].ToString().ShouldBe("invalid-imm");
    }

    [TestMethod]
    public async Task GitHubImmAllBadges_ImmRuleNotInRuleSet_Returns422HtmlPageNamingTheRuleId()
    {
        // Arrange
        const string unknownRuleId = "0b2c6a3e-58a4-4d2c-9a55-5c1f0f4f8f11";
        const string immYaml = $"""
            -
                Name: 'A rule from a newer rule set'
                Id: '{unknownRuleId}'
                Measures:
                    -
                        Score: 1
            """;
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, immYaml);
        HttpRequest request = ImmFunctionFixture.CreateRequest();

        // Act
        IActionResult result = await fixture.Functions.GitHubImmAllBadges(request, ImmFunctionFixture.Org, ImmFunctionFixture.Project);

        // Assert
        ContentResult content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(StatusCodes.Status422UnprocessableEntity);
        content.Content!.ShouldContain(unknownRuleId);
        request.HttpContext.Response.Headers["X-Imm-Error"].ToString().ShouldBe("unknown-rule");
    }

    [TestMethod]
    public async Task GitHubImmAllBadges_ErrorPage_HtmlEncodesOrgProjectAndBranch()
    {
        // Arrange
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);
        fixture.ImmFailsWith(new GitHubContentNotFoundException(
            new Uri("https://raw.githubusercontent.com/o%3Cg/p%26j/b%3Cr/imm.yaml"), HttpStatusCode.NotFound));
        HttpRequest request = ImmFunctionFixture.CreateRequest("?projectBranch=%3Cscript%3E&definitionsBranch=a%26b");

        // Act
        IActionResult result = await fixture.Functions.GitHubImmAllBadges(request, "<org>", "p&\"j\"");

        // Assert
        string html = result.ShouldBeOfType<ContentResult>().Content!;
        html.ShouldNotContain("<script>");
        html.ShouldNotContain("<org>");
        html.ShouldNotContain("p&\"j\"");
        html.ShouldContain("&lt;script&gt;");
        html.ShouldContain("&lt;org&gt;/p&amp;&quot;j&quot;");
        html.ShouldContain("rules: a&amp;b");
    }
}
