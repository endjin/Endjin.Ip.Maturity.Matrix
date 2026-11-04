using System.Diagnostics;
using System.Net;
using Endjin.Imm.Caching;
using Endjin.Imm.Domain;
using Endjin.Imm.Tests.TestData;
using Shouldly;

namespace Endjin.Imm.Tests;

/// <summary>
/// Each request for content from GitHub is traced, so that the time a badge request spends
/// getting its rule set and IMM can be seen: an <c>imm.content.get</c> span for each request,
/// saying whether it was served from the cache (a hit), joined a fetch already in flight
/// (coalesced, with a link to that fetch), or had to fetch (a miss, or a revalidation of stale
/// content); an <c>imm.content.fetch</c> span for each request to GitHub, recording whether it was
/// conditional, the status GitHub returned, and whether stale content was served because GitHub
/// was unavailable; and an <c>imm.content.deserialize</c> span for reading the YAML, recording its
/// length. Failures mark the spans they escape from as errors, naming the exception's type.
/// </summary>
[TestClass]
public class GitHubContentTracingTests
{
    private const string Owner = "endjin";
    private const string Project = "Endjin.Ip.Maturity.Model";
    private const string Branch = "master";
    private const string ImmUrl = $"https://raw.githubusercontent.com/{Owner}/{Project}/{Branch}/imm.yaml";

    private static readonly TimeSpan OneHour = TimeSpan.FromHours(1);

    [TestMethod]
    public async Task GetIpMaturityModelAsync_NothingCached_TracesAMissThatFetchesAndDeserialisesTheImm()
    {
        // Arrange
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingWith(
            () => StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"v1\"", OneHour));
        using ActivityCapture capture = new(ImmTelemetry.ActivitySourceName);

        // Act
        await GetImmAsync(fixture);

        // Assert
        Activity get = capture.Single("imm.content.get");
        Activity fetch = capture.Single("imm.content.fetch");
        Activity deserialize = capture.Single("imm.content.deserialize");
        capture.Activities.Count.ShouldBe(3);

        get.ParentSpanId.ShouldBe(capture.Root.SpanId);
        get.Kind.ShouldBe(ActivityKind.Internal);
        get.GetTagItem("url.full").ShouldBe(ImmUrl);
        get.GetTagItem("imm.cache.result").ShouldBe("miss");
        get.Links.ShouldBeEmpty();
        get.Status.ShouldBe(ActivityStatusCode.Unset);

        fetch.ParentSpanId.ShouldBe(get.SpanId);
        fetch.GetTagItem("url.full").ShouldBe(ImmUrl);
        fetch.GetTagItem("imm.cache.conditional").ShouldBe(false);
        fetch.GetTagItem("http.response.status_code").ShouldBe(200);
        fetch.GetTagItem("imm.cache.not_modified").ShouldBe(false);
        fetch.GetTagItem("imm.cache.stale_if_error").ShouldBeNull();
        fetch.Status.ShouldBe(ActivityStatusCode.Unset);

        deserialize.ParentSpanId.ShouldBe(fetch.SpanId);
        deserialize.GetTagItem("imm.content.length").ShouldBe(ImmYaml.SharedEngineeringStandardsConfigured.Length);
        deserialize.Status.ShouldBe(ActivityStatusCode.Unset);
    }

    [TestMethod]
    public async Task GetRuleDefinitionRepositoryAsync_NothingCached_TracesTheLengthOfTheRuleSet()
    {
        // Arrange
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingWith(
            () => StubHttpMessageHandler.Ok(RuleSetYaml.DeploymentWithOptionalMeasure, "\"v1\"", OneHour));
        using ActivityCapture capture = new(ImmTelemetry.ActivitySourceName);

        // Act
        await fixture.RuleDefinitionSource.GetRuleDefinitionRepositoryAsync(Branch);

        // Assert
        capture.Single("imm.content.get").GetTagItem("url.full").ShouldBe(IpMaturityModelRuleset.DefinitionsUrlForName(Branch));
        capture.Single("imm.content.deserialize").GetTagItem("imm.content.length").ShouldBe(RuleSetYaml.DeploymentWithOptionalMeasure.Length);
    }

    [TestMethod]
    public async Task GetIpMaturityModelAsync_FreshImmCached_TracesAHitWithoutAFetch()
    {
        // Arrange
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingWith(
            () => StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"v1\"", OneHour));
        await GetImmAsync(fixture);
        using ActivityCapture capture = new(ImmTelemetry.ActivitySourceName);

        // Act
        await GetImmAsync(fixture);

        // Assert
        Activity get = capture.Activities.ShouldHaveSingleItem();
        get.OperationName.ShouldBe("imm.content.get");
        get.GetTagItem("imm.cache.result").ShouldBe("hit");
        get.Status.ShouldBe(ActivityStatusCode.Unset);
    }

    [TestMethod]
    public async Task GetIpMaturityModelAsync_AfterMaxAgeWhenContentUnchanged_TracesAConditionalFetchThatIsNotModifiedAndNotDeserialised()
    {
        // Arrange
        FakeSystemClock clock = new();
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingInSequence(
            clock,
            () => StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"v1\"", TimeSpan.FromMinutes(1)),
            () => StubHttpMessageHandler.NotModified("\"v1\"", TimeSpan.FromMinutes(1)));
        await GetImmAsync(fixture);
        clock.Advance(TimeSpan.FromSeconds(61));
        using ActivityCapture capture = new(ImmTelemetry.ActivitySourceName);

        // Act
        await GetImmAsync(fixture);

        // Assert
        Activity get = capture.Single("imm.content.get");
        Activity fetch = capture.Single("imm.content.fetch");
        get.GetTagItem("imm.cache.result").ShouldBe("revalidate");
        fetch.ParentSpanId.ShouldBe(get.SpanId);
        fetch.GetTagItem("imm.cache.conditional").ShouldBe(true);
        fetch.GetTagItem("http.response.status_code").ShouldBe(304);
        fetch.GetTagItem("imm.cache.not_modified").ShouldBe(true);
        fetch.Status.ShouldBe(ActivityStatusCode.Unset);
        capture.Where("imm.content.deserialize").ShouldBeEmpty();
    }

    // A README with several badges for the same project asks for the same IMM several times at once. The requests that
    // join the first one's fetch are in other traces, so they link to the fetch rather than containing it.
    [TestMethod]
    public async Task GetIpMaturityModelAsync_ConcurrentRequests_TracesOneMissThatFetchesAndACoalescedRequestLinkedToThatFetch()
    {
        // Arrange
        TaskCompletionSource<HttpResponseMessage> gitHubResponse = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using ImmSourceFixture fixture = new((_, _) => gitHubResponse.Task);
        using ActivityCapture capture = new(ImmTelemetry.ActivitySourceName);

        // Act
        Task<IpMaturityModel> first = GetImmAsync(fixture);
        Task<IpMaturityModel> second = GetImmAsync(fixture);
        gitHubResponse.SetResult(StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"v1\"", OneHour));
        await Task.WhenAll(first, second);

        // Assert
        fixture.Handler.Requests.Count.ShouldBe(1);
        Activity fetch = capture.Single("imm.content.fetch");
        IReadOnlyList<Activity> gets = capture.Where("imm.content.get");
        gets.Count.ShouldBe(2);
        Activity miss = gets.Single(get => (string?)get.GetTagItem("imm.cache.result") == "miss");
        Activity coalesced = gets.Single(get => (string?)get.GetTagItem("imm.cache.result") == "coalesced");
        fetch.ParentSpanId.ShouldBe(miss.SpanId);
        miss.Links.ShouldBeEmpty();
        ActivityLink link = coalesced.Links.ShouldHaveSingleItem();
        link.Context.TraceId.ShouldBe(fetch.TraceId);
        link.Context.SpanId.ShouldBe(fetch.SpanId);
        capture.Where("imm.content.fetch").Count.ShouldBe(1);
    }

    [TestMethod]
    public async Task GetIpMaturityModelAsync_GitHubReturnsNotFound_TracesTheFetchAndTheGetAsErrorsAndTheNextRequestAsAFailedHit()
    {
        // Arrange
        FakeSystemClock clock = new();
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingInSequence(
            clock,
            () => new HttpResponseMessage(HttpStatusCode.NotFound),
            () => StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"v1\"", OneHour));
        using ActivityCapture capture = new(ImmTelemetry.ActivitySourceName);

        // Act
        await Should.ThrowAsync<GitHubContentNotFoundException>(() => GetImmAsync(fixture));
        clock.Advance(TimeSpan.FromSeconds(59));
        await Should.ThrowAsync<GitHubContentNotFoundException>(() => GetImmAsync(fixture));

        // Assert
        string notFound = typeof(GitHubContentNotFoundException).FullName!;
        Activity fetch = capture.Single("imm.content.fetch");
        fetch.GetTagItem("http.response.status_code").ShouldBe(404);
        fetch.Status.ShouldBe(ActivityStatusCode.Error);
        fetch.GetTagItem("error.type").ShouldBe(notFound);
        capture.Where("imm.content.deserialize").ShouldBeEmpty();

        IReadOnlyList<Activity> gets = capture.Where("imm.content.get");
        gets.Select(get => get.GetTagItem("imm.cache.result")).ShouldBe(["miss", "hit"]);
        gets.ShouldAllBe(get => get.Status == ActivityStatusCode.Error);
        gets.ShouldAllBe(get => (string?)get.GetTagItem("error.type") == notFound);
    }

    [TestMethod]
    public async Task GetIpMaturityModelAsync_GitHubUnavailableWhenStaleImmHeld_TracesTheFetchAsServingStaleContentWithoutAnError()
    {
        // Arrange
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingInSequence(
            () => StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"v1\"", TimeSpan.Zero),
            () => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        await GetImmAsync(fixture);
        using ActivityCapture capture = new(ImmTelemetry.ActivitySourceName);

        // Act
        await GetImmAsync(fixture);

        // Assert
        Activity get = capture.Single("imm.content.get");
        Activity fetch = capture.Single("imm.content.fetch");
        get.GetTagItem("imm.cache.result").ShouldBe("revalidate");
        get.Status.ShouldBe(ActivityStatusCode.Unset);
        fetch.GetTagItem("imm.cache.conditional").ShouldBe(true);
        fetch.GetTagItem("http.response.status_code").ShouldBe(503);
        fetch.GetTagItem("imm.cache.stale_if_error").ShouldBe(true);
        fetch.Status.ShouldBe(ActivityStatusCode.Unset);
        fetch.GetTagItem("error.type").ShouldBeNull();
    }

    [TestMethod]
    public async Task GetIpMaturityModelAsync_GitHubReturnsMalformedYaml_TracesTheDeserialisationAsAnInvalidContentError()
    {
        // Arrange
        const string malformedYaml = "- Name: 'unterminated";
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingWith(
            () => StubHttpMessageHandler.Ok(malformedYaml, "\"broken\"", OneHour));
        using ActivityCapture capture = new(ImmTelemetry.ActivitySourceName);

        // Act
        await Should.ThrowAsync<InvalidImmContentException>(() => GetImmAsync(fixture));

        // Assert
        string invalidContent = typeof(InvalidImmContentException).FullName!;
        Activity deserialize = capture.Single("imm.content.deserialize");
        deserialize.GetTagItem("imm.content.length").ShouldBe(malformedYaml.Length);
        deserialize.Status.ShouldBe(ActivityStatusCode.Error);
        deserialize.GetTagItem("error.type").ShouldBe(invalidContent);
        capture.Single("imm.content.fetch").GetTagItem("error.type").ShouldBe(invalidContent);
        capture.Single("imm.content.get").GetTagItem("error.type").ShouldBe(invalidContent);
    }

    private static Task<IpMaturityModel> GetImmAsync(ImmSourceFixture fixture) =>
        fixture.ImmSource.GetIpMaturityModelAsync(Owner, Project, Branch);
}
