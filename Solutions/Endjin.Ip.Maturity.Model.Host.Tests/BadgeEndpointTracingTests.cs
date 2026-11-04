using System.Diagnostics;
using System.Net;
using Endjin.Imm.Caching;
using Endjin.Ip.Maturity.Model.Host.Tests.TestData;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace Endjin.Ip.Maturity.Model.Host.Tests;

/// <summary>
/// When telemetry is exported, each badge request is traced as a span named after its endpoint
/// (imm.badge.total, imm.badge.rule or imm.badge.showbadges), tagged with the repository and the
/// branches it asked for, with a child imm.render span around rendering the badge or page, so that
/// a trace shows where the request's time went. A badge or page that shows an error tags the span
/// with its X-Imm-Error code. The span is marked as an error only when something is wrong that we
/// want to hear about: GitHub being unavailable, or an exception that propagates. A request for an
/// IMM that does not exist is answered correctly, so it is not an error.
/// </summary>
[TestClass]
public class BadgeEndpointTracingTests
{
    private const string ImmUrl = "https://raw.githubusercontent.com/endjin/Endjin.Ip.Maturity.Model/master/imm.yaml";

    [TestMethod]
    public async Task GitHubImmTotalScore_ScoredImm_TracesTheRequestTaggedWithTheRepositoryAndDefaultBranches()
    {
        // Arrange
        using ActivityCapture capture = new(HostTelemetry.ActivitySourceName);
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);

        // Act
        await fixture.Functions.GitHubImmTotalScore(ImmFunctionFixture.CreateRequest(), ImmFunctionFixture.Org, ImmFunctionFixture.Project);

        // Assert
        Activity span = capture.Single("imm.badge.total");
        span.ParentSpanId.ShouldBe(capture.Root.SpanId);
        span.Kind.ShouldBe(ActivityKind.Internal);
        span.Status.ShouldBe(ActivityStatusCode.Unset);
        span.GetTagItem("imm.github.owner").ShouldBe(ImmFunctionFixture.Org);
        span.GetTagItem("imm.github.repo").ShouldBe(ImmFunctionFixture.Project);
        span.GetTagItem("imm.ruleset.ref").ShouldBe("main");
        span.GetTagItem("imm.github.ref").ShouldBe("master");
        span.GetTagItem("imm.error").ShouldBeNull();
    }

    [TestMethod]
    public async Task GitHubImmTotalScore_BranchesInQueryString_TagsTheSpanWithTheRequestedBranches()
    {
        // Arrange
        using ActivityCapture capture = new(HostTelemetry.ActivitySourceName);
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);
        HttpRequest request = ImmFunctionFixture.CreateRequest("?definitionsBranch=rules-v2&projectBranch=main");

        // Act
        await fixture.Functions.GitHubImmTotalScore(request, ImmFunctionFixture.Org, ImmFunctionFixture.Project);

        // Assert
        Activity span = capture.Single("imm.badge.total");
        span.GetTagItem("imm.ruleset.ref").ShouldBe("rules-v2");
        span.GetTagItem("imm.github.ref").ShouldBe("main");
    }

    [TestMethod]
    public async Task GitHubImmTotalScore_ScoredImm_TracesRenderingTheBadgeAsAChildSpan()
    {
        // Arrange
        using ActivityCapture capture = new(HostTelemetry.ActivitySourceName);
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);

        // Act
        await fixture.Functions.GitHubImmTotalScore(ImmFunctionFixture.CreateRequest(), ImmFunctionFixture.Org, ImmFunctionFixture.Project);

        // Assert
        ShouldBeRenderedWithin(capture, "imm.badge.total", "total");
    }

    [TestMethod]
    public async Task GitHubImmByRule_ScoredRule_TracesTheRequestTaggedWithTheRuleIdAndRendering()
    {
        // Arrange
        using ActivityCapture capture = new(HostTelemetry.ActivitySourceName);
        (string ruleSetYaml, string immYaml) = HostYaml.Scoring(50);
        ImmFunctionFixture fixture = new(ruleSetYaml, immYaml);

        // Act
        await fixture.Functions.GitHubImmByRule(
            ImmFunctionFixture.CreateRequest(), ImmFunctionFixture.Org, ImmFunctionFixture.Project, HostYaml.ScoredRuleId);

        // Assert
        Activity span = capture.Single("imm.badge.rule");
        span.Status.ShouldBe(ActivityStatusCode.Unset);
        span.GetTagItem("imm.github.owner").ShouldBe(ImmFunctionFixture.Org);
        span.GetTagItem("imm.github.repo").ShouldBe(ImmFunctionFixture.Project);
        span.GetTagItem("imm.ruleset.ref").ShouldBe("main");
        span.GetTagItem("imm.github.ref").ShouldBe("master");
        span.GetTagItem("imm.rule.id").ShouldBe(HostYaml.ScoredRuleId);
        ShouldBeRenderedWithin(capture, "imm.badge.rule", "rule");
    }

    [TestMethod]
    public async Task GitHubImmAllBadges_ScoredImm_TracesTheRequestAndRenderingThePage()
    {
        // Arrange
        using ActivityCapture capture = new(HostTelemetry.ActivitySourceName);
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);

        // Act
        await fixture.Functions.GitHubImmAllBadges(ImmFunctionFixture.CreateRequest(), ImmFunctionFixture.Org, ImmFunctionFixture.Project);

        // Assert
        Activity span = capture.Single("imm.badge.showbadges");
        span.Status.ShouldBe(ActivityStatusCode.Unset);
        span.GetTagItem("imm.github.owner").ShouldBe(ImmFunctionFixture.Org);
        span.GetTagItem("imm.github.repo").ShouldBe(ImmFunctionFixture.Project);
        span.GetTagItem("imm.ruleset.ref").ShouldBe("main");
        span.GetTagItem("imm.github.ref").ShouldBe("master");
        ShouldBeRenderedWithin(capture, "imm.badge.showbadges", "showbadges");
    }

    [TestMethod]
    public async Task GitHubImmTotalScore_ImmNotFound_TagsTheErrorCodeWithoutMarkingTheSpanAsAnError()
    {
        // Arrange
        using ActivityCapture capture = new(HostTelemetry.ActivitySourceName);
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);
        fixture.ImmFailsWith(new GitHubContentNotFoundException(new Uri(ImmUrl), HttpStatusCode.NotFound));

        // Act
        await fixture.Functions.GitHubImmTotalScore(ImmFunctionFixture.CreateRequest(), ImmFunctionFixture.Org, ImmFunctionFixture.Project);

        // Assert
        Activity span = capture.Single("imm.badge.total");
        span.Status.ShouldBe(ActivityStatusCode.Unset);
        span.GetTagItem("imm.error").ShouldBe("imm-not-found");
        ShouldBeRenderedWithin(capture, "imm.badge.total", "error-badge");
    }

    [TestMethod]
    public async Task GitHubImmByRule_RuleIdNotAGuid_TagsTheRawRuleIdAndTheErrorCode()
    {
        // Arrange
        using ActivityCapture capture = new(HostTelemetry.ActivitySourceName);
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);

        // Act
        await fixture.Functions.GitHubImmByRule(
            ImmFunctionFixture.CreateRequest(), ImmFunctionFixture.Org, ImmFunctionFixture.Project, "not-a-rule-id");

        // Assert
        Activity span = capture.Single("imm.badge.rule");
        span.Status.ShouldBe(ActivityStatusCode.Unset);
        span.GetTagItem("imm.rule.id").ShouldBe("not-a-rule-id");
        span.GetTagItem("imm.error").ShouldBe("invalid-rule-id");
        ShouldBeRenderedWithin(capture, "imm.badge.rule", "error-badge");
    }

    [TestMethod]
    public async Task GitHubImmTotalScore_GitHubUnavailable_MarksTheSpanAsAnErrorDescribingTheProblem()
    {
        // Arrange
        using ActivityCapture capture = new(HostTelemetry.ActivitySourceName);
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);
        fixture.ImmFailsWith(new GitHubContentUnavailableException(new Uri(ImmUrl), HttpStatusCode.ServiceUnavailable, null));

        // Act
        await fixture.Functions.GitHubImmTotalScore(ImmFunctionFixture.CreateRequest(), ImmFunctionFixture.Org, ImmFunctionFixture.Project);

        // Assert
        Activity span = capture.Single("imm.badge.total");
        span.Status.ShouldBe(ActivityStatusCode.Error);
        span.StatusDescription.ShouldBe("GitHub returned 503 (ServiceUnavailable).");
        span.GetTagItem("imm.error").ShouldBe("upstream-unavailable");
        ShouldBeRenderedWithin(capture, "imm.badge.total", "error-badge");
    }

    [TestMethod]
    public async Task GitHubImmAllBadges_ImmNotFound_TagsTheErrorCodeAndTracesRenderingTheErrorPage()
    {
        // Arrange
        using ActivityCapture capture = new(HostTelemetry.ActivitySourceName);
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);
        fixture.ImmFailsWith(new GitHubContentNotFoundException(new Uri(ImmUrl), HttpStatusCode.NotFound));

        // Act
        await fixture.Functions.GitHubImmAllBadges(ImmFunctionFixture.CreateRequest(), ImmFunctionFixture.Org, ImmFunctionFixture.Project);

        // Assert
        Activity span = capture.Single("imm.badge.showbadges");
        span.Status.ShouldBe(ActivityStatusCode.Unset);
        span.GetTagItem("imm.error").ShouldBe("imm-not-found");
        ShouldBeRenderedWithin(capture, "imm.badge.showbadges", "error-page");
    }

    [TestMethod]
    public async Task GitHubImmAllBadges_GitHubUnavailable_MarksTheSpanAsAnError()
    {
        // Arrange
        using ActivityCapture capture = new(HostTelemetry.ActivitySourceName);
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);
        fixture.ImmFailsWith(new GitHubContentUnavailableException(new Uri(ImmUrl), null, new HttpRequestException("No such host is known.")));

        // Act
        await fixture.Functions.GitHubImmAllBadges(ImmFunctionFixture.CreateRequest(), ImmFunctionFixture.Org, ImmFunctionFixture.Project);

        // Assert
        Activity span = capture.Single("imm.badge.showbadges");
        span.Status.ShouldBe(ActivityStatusCode.Error);
        span.StatusDescription.ShouldBe("GitHub could not be reached.");
        span.GetTagItem("imm.error").ShouldBe("upstream-unavailable");
        ShouldBeRenderedWithin(capture, "imm.badge.showbadges", "error-page");
    }

    [TestMethod]
    public async Task GitHubImmTotalScore_UnexpectedException_MarksTheSpanAsAnErrorAndRecordsTheException()
    {
        // Arrange
        using ActivityCapture capture = new(HostTelemetry.ActivitySourceName);
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);
        fixture.ImmFailsWith(new InvalidOperationException("A bug"));

        // Act
        await Should.ThrowAsync<InvalidOperationException>(
            () => fixture.Functions.GitHubImmTotalScore(ImmFunctionFixture.CreateRequest(), ImmFunctionFixture.Org, ImmFunctionFixture.Project));

        // Assert
        Activity span = capture.Single("imm.badge.total");
        span.Status.ShouldBe(ActivityStatusCode.Error);
        span.GetTagItem("error.type").ShouldBe(typeof(InvalidOperationException).FullName);
        span.GetTagItem("imm.error").ShouldBeNull();
        ActivityEvent exceptionEvent = span.Events.ShouldHaveSingleItem();
        exceptionEvent.Name.ShouldBe("exception");
        exceptionEvent.Tags.ShouldContain(new KeyValuePair<string, object?>("exception.message", "A bug"));
        capture.Where("imm.render").ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GitHubImmByRule_UnexpectedException_MarksTheSpanAsAnError()
    {
        // Arrange
        using ActivityCapture capture = new(HostTelemetry.ActivitySourceName);
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);
        fixture.RuleSetFailsWith(new InvalidOperationException("A bug"));

        // Act
        await Should.ThrowAsync<InvalidOperationException>(
            () => fixture.Functions.GitHubImmByRule(
                ImmFunctionFixture.CreateRequest(), ImmFunctionFixture.Org, ImmFunctionFixture.Project, HostYaml.ScoredRuleId));

        // Assert
        Activity span = capture.Single("imm.badge.rule");
        span.Status.ShouldBe(ActivityStatusCode.Error);
        span.GetTagItem("error.type").ShouldBe(typeof(InvalidOperationException).FullName);
        span.Events.ShouldHaveSingleItem().Name.ShouldBe("exception");
    }

    [TestMethod]
    public async Task GitHubImmAllBadges_UnexpectedException_MarksTheSpanAsAnError()
    {
        // Arrange
        using ActivityCapture capture = new(HostTelemetry.ActivitySourceName);
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);
        fixture.ImmFailsWith(new InvalidOperationException("A bug"));

        // Act
        await Should.ThrowAsync<InvalidOperationException>(
            () => fixture.Functions.GitHubImmAllBadges(ImmFunctionFixture.CreateRequest(), ImmFunctionFixture.Org, ImmFunctionFixture.Project));

        // Assert
        Activity span = capture.Single("imm.badge.showbadges");
        span.Status.ShouldBe(ActivityStatusCode.Error);
        span.GetTagItem("error.type").ShouldBe(typeof(InvalidOperationException).FullName);
        span.Events.ShouldHaveSingleItem().Name.ShouldBe("exception");
    }

    // The domain's spans join the endpoint's trace, so one trace shows a request's fetches, evaluation and rendering together.
    [TestMethod]
    public async Task GitHubImmTotalScore_ScoredImm_TracesEvaluatingTheImmAsAChildOfTheRequestBeforeRendering()
    {
        // Arrange
        using ActivityCapture capture = new(HostTelemetry.ActivitySourceName, Endjin.Imm.ImmTelemetry.ActivitySourceName);
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);

        // Act
        await fixture.Functions.GitHubImmTotalScore(ImmFunctionFixture.CreateRequest(), ImmFunctionFixture.Org, ImmFunctionFixture.Project);

        // Assert
        Activity request = capture.Single("imm.badge.total");
        Activity evaluate = capture.Single("imm.evaluate");
        Activity render = capture.Single("imm.render");
        evaluate.ParentSpanId.ShouldBe(request.SpanId);
        evaluate.GetTagItem("imm.rule_count").ShouldBe(2);
        evaluate.GetTagItem("imm.score").ShouldBe(3L);
        evaluate.GetTagItem("imm.max_score").ShouldBe(3L);
        (evaluate.StartTimeUtc + evaluate.Duration).ShouldBeLessThanOrEqualTo(render.StartTimeUtc);
        capture.Activities.Select(a => a.OperationName).ShouldBe(["imm.evaluate", "imm.render", "imm.badge.total"]);
    }

    private static void ShouldBeRenderedWithin(ActivityCapture capture, string endpointSpanName, string expectedKind)
    {
        Activity endpointSpan = capture.Single(endpointSpanName);
        Activity render = capture.Single("imm.render");
        render.ParentSpanId.ShouldBe(endpointSpan.SpanId);
        render.Kind.ShouldBe(ActivityKind.Internal);
        render.GetTagItem("imm.render.kind").ShouldBe(expectedKind);
    }
}
