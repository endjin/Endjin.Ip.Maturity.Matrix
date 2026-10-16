using System.Net;
using System.Net.Http.Headers;
using Endjin.Imm.Caching;
using Endjin.Imm.Contracts;
using Endjin.Imm.Domain;
using Endjin.Imm.Tests.TestData;
using Shouldly;
using YamlDotNet.Core;

namespace Endjin.Imm.Tests;

/// <summary>
/// Content fetched from GitHub is cached according to the caching headers GitHub returns, so
/// that a page showing many badges does not ask GitHub for the same file again and again, while
/// changes to the file are still picked up once the cached copy is stale. Stale content is
/// revalidated with a conditional request, and an entry that nobody has asked for in 20 minutes
/// is dropped. Content marked no-cache is revalidated every time it is used, and content marked
/// no-store is not kept at all. Failures are reported by kind: content that does not exist, GitHub
/// being unavailable, and content that is not valid YAML. Only "not found" is cached, for one
/// minute; while GitHub is unavailable, content fetched earlier is served instead.
/// </summary>
[TestClass]
public class GitHubContentCachingTests
{
    private const string Owner = "endjin";
    private const string Project = "Endjin.Ip.Maturity.Model";
    private const string Branch = "master";
    private const string ImmUrl = $"https://raw.githubusercontent.com/{Owner}/{Project}/{Branch}/imm.yaml";

    private static readonly TimeSpan OneHour = TimeSpan.FromHours(1);

    [TestMethod]
    public async Task GetIpMaturityModelAsync_SecondRequestWithinMaxAge_IsServedFromTheCacheWithoutContactingGitHub()
    {
        // Arrange
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingWith(
            () => StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"v1\"", OneHour));

        // Act
        IpMaturityModel first = await GetImmAsync(fixture);
        IpMaturityModel second = await GetImmAsync(fixture);

        // Assert
        fixture.Handler.Requests.Count.ShouldBe(1);
        second.ShouldBeSameAs(first);
    }

    [TestMethod]
    public async Task GetRuleDefinitionRepositoryAsync_SecondRequestWithinMaxAge_IsServedFromTheCacheWithoutContactingGitHub()
    {
        // Arrange
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingWith(
            () => StubHttpMessageHandler.Ok(RuleSetYaml.DeploymentWithOptionalMeasure, "\"v1\"", OneHour));

        // Act
        IRuleDefinitionRepository first = await fixture.RuleDefinitionSource.GetRuleDefinitionRepositoryAsync(Branch);
        IRuleDefinitionRepository second = await fixture.RuleDefinitionSource.GetRuleDefinitionRepositoryAsync(Branch);

        // Assert
        fixture.Handler.Requests.Count.ShouldBe(1);
        second.ShouldBeSameAs(first);
    }

    // Without a max-age, content is treated as fresh for a default of one minute. This checks the start of that window
    // with the real clock.
    [TestMethod]
    [DataRow(null)]
    [DataRow("public")]
    public async Task GetIpMaturityModelAsync_ResponseWithoutMaxAge_IsCachedForTheDefaultWindow(string? cacheControl)
    {
        // Arrange
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingWith(() =>
        {
            HttpResponseMessage response = StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured);
            if (cacheControl is not null)
            {
                response.Headers.CacheControl = CacheControlHeaderValue.Parse(cacheControl);
            }

            return response;
        });

        // Act
        IpMaturityModel first = await GetImmAsync(fixture);
        IpMaturityModel second = await GetImmAsync(fixture);

        // Assert
        fixture.Handler.Requests.Count.ShouldBe(1);
        second.ShouldBeSameAs(first);
    }

    [TestMethod]
    [DataRow("no-cache")]
    [DataRow("public, no-cache")]
    [DataRow("no-cache, max-age=3600")]
    public async Task GetIpMaturityModelAsync_ResponseWithNoCache_IsRevalidatedWithETagOnEveryRequest(string cacheControl)
    {
        // Arrange
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingInSequence(
            () => StubHttpMessageHandler.WithCacheControl(
                StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"v1\""), cacheControl),
            () => StubHttpMessageHandler.WithCacheControl(StubHttpMessageHandler.NotModified("\"v1\""), cacheControl),
            () => StubHttpMessageHandler.WithCacheControl(StubHttpMessageHandler.NotModified("\"v1\""), cacheControl));

        // Act
        IpMaturityModel first = await GetImmAsync(fixture);
        IpMaturityModel second = await GetImmAsync(fixture);
        IpMaturityModel third = await GetImmAsync(fixture);

        // Assert
        fixture.Handler.Requests.Select(r => r.IfNoneMatch).ShouldBe(
        [
            [],
            ["\"v1\""],
            ["\"v1\""],
        ]);
        second.ShouldBeSameAs(first);
        third.ShouldBeSameAs(first);
    }

    // no-store forbids keeping the response, so there is no copy to revalidate: every request fetches the content afresh.
    [TestMethod]
    [DataRow("no-store")]
    [DataRow("no-store, max-age=3600")]
    [DataRow("no-cache, no-store")]
    public async Task GetIpMaturityModelAsync_ResponseWithNoStore_IsFetchedAgainUnconditionallyOnEveryRequest(string cacheControl)
    {
        // Arrange
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingWith(
            () => StubHttpMessageHandler.WithCacheControl(
                StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"v1\""), cacheControl));

        // Act
        IpMaturityModel first = await GetImmAsync(fixture);
        IpMaturityModel second = await GetImmAsync(fixture);

        // Assert
        fixture.Handler.Requests.Select(r => r.IfNoneMatch).ShouldBe(
        [
            [],
            [],
        ]);
        second.ShouldNotBeSameAs(first);
        second.Rules.Single().Measures.Single().Score.ShouldBe(1);
    }

    [TestMethod]
    public async Task GetIpMaturityModelAsync_ResponseWithNoStoreReceivedSynchronously_IsNotLeftInTheMemoryCache()
    {
        // Arrange
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingWith(
            () => StubHttpMessageHandler.WithCacheControl(
                StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"v1\""), "no-store"));

        // Act
        IpMaturityModel imm = await GetImmAsync(fixture);

        // Assert
        imm.Rules.Single().Measures.Single().Score.ShouldBe(1);
        fixture.MemoryCache.Count.ShouldBe(0);
    }

    [TestMethod]
    public async Task GetIpMaturityModelAsync_ResponseWithNoStoreReceivedAsynchronously_IsRemovedFromTheMemoryCacheAndStillReachesEveryWaitingCaller()
    {
        // Arrange
        TaskCompletionSource<HttpResponseMessage> gitHubResponse = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using ImmSourceFixture fixture = new((_, _) => gitHubResponse.Task);

        // Act
        Task<IpMaturityModel> first = GetImmAsync(fixture);
        Task<IpMaturityModel> second = GetImmAsync(fixture);
        int entriesWhileInFlight = fixture.MemoryCache.Count;
        gitHubResponse.SetResult(StubHttpMessageHandler.WithCacheControl(
            StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"v1\""), "no-store"));
        IpMaturityModel[] results = await Task.WhenAll(first, second);

        // Assert
        entriesWhileInFlight.ShouldBe(1);
        fixture.Handler.Requests.Count.ShouldBe(1);
        results[1].ShouldBeSameAs(results[0]);
        fixture.MemoryCache.Count.ShouldBe(0);
    }

    // A 304 updates only the headers it carries (RFC 9111, section 4.3.4), so one without Cache-Control must not replace a
    // stored no-cache policy with the one-minute default.
    [TestMethod]
    public async Task GetIpMaturityModelAsync_NotModifiedResponseWithoutCacheControl_KeepsTheNoCachePolicyOfTheOriginalResponse()
    {
        // Arrange
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingInSequence(
            () => StubHttpMessageHandler.WithCacheControl(
                StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"v1\""), "no-cache"),
            () => StubHttpMessageHandler.NotModified("\"v1\""),
            () => StubHttpMessageHandler.NotModified("\"v1\""));

        // Act
        IpMaturityModel first = await GetImmAsync(fixture);
        IpMaturityModel second = await GetImmAsync(fixture);
        IpMaturityModel third = await GetImmAsync(fixture);

        // Assert
        fixture.Handler.Requests.Select(r => r.IfNoneMatch).ShouldBe(
        [
            [],
            ["\"v1\""],
            ["\"v1\""],
        ]);
        second.ShouldBeSameAs(first);
        third.ShouldBeSameAs(first);
    }

    [TestMethod]
    public async Task GetIpMaturityModelAsync_NotModifiedResponseWithItsOwnCacheControl_ReplacesTheStoredPolicy()
    {
        // Arrange
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingInSequence(
            () => StubHttpMessageHandler.WithCacheControl(
                StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"v1\""), "no-cache"),
            () => StubHttpMessageHandler.NotModified("\"v1\"", OneHour));

        // Act
        IpMaturityModel first = await GetImmAsync(fixture);
        IpMaturityModel revalidated = await GetImmAsync(fixture);
        IpMaturityModel third = await GetImmAsync(fixture);

        // Assert
        fixture.Handler.Requests.Count.ShouldBe(2);
        revalidated.ShouldBeSameAs(first);
        third.ShouldBeSameAs(first);
    }

    [TestMethod]
    public async Task GetIpMaturityModelAsync_NotModifiedResponseWithoutAnETag_KeepsRevalidatingWithThePreviousETag()
    {
        // Arrange
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingInSequence(
            () => StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"v1\"", TimeSpan.Zero),
            () => StubHttpMessageHandler.NotModified(null, TimeSpan.Zero),
            () => StubHttpMessageHandler.NotModified("\"v1\"", TimeSpan.Zero));

        // Act
        IpMaturityModel first = await GetImmAsync(fixture);
        IpMaturityModel second = await GetImmAsync(fixture);
        IpMaturityModel third = await GetImmAsync(fixture);

        // Assert
        fixture.Handler.Requests.Select(r => r.IfNoneMatch).ShouldBe(
        [
            [],
            ["\"v1\""],
            ["\"v1\""],
        ]);
        second.ShouldBeSameAs(first);
        third.ShouldBeSameAs(first);
    }

    [TestMethod]
    public async Task GetIpMaturityModelAsync_AfterMaxAgeWhenContentUnchanged_RevalidatesWithETagAndReusesTheCachedImm()
    {
        // Arrange
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingInSequence(
            () => StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"v1\"", TimeSpan.Zero),
            () => StubHttpMessageHandler.NotModified("\"v1\"", TimeSpan.Zero));

        // Act
        IpMaturityModel first = await GetImmAsync(fixture);
        IpMaturityModel second = await GetImmAsync(fixture);

        // Assert
        fixture.Handler.Requests.Count.ShouldBe(2);
        fixture.Handler.Requests[0].IfNoneMatch.ShouldBeEmpty();
        fixture.Handler.Requests[1].IfNoneMatch.ShouldBe(["\"v1\""]);
        second.ShouldBeSameAs(first);
    }

    [TestMethod]
    public async Task GetIpMaturityModelAsync_NotModifiedResponse_StartsANewFreshnessWindow()
    {
        // Arrange
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingInSequence(
            () => StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"v1\"", TimeSpan.Zero),
            () => StubHttpMessageHandler.NotModified("\"v1\"", OneHour));

        // Act
        IpMaturityModel first = await GetImmAsync(fixture);
        IpMaturityModel revalidated = await GetImmAsync(fixture);
        IpMaturityModel third = await GetImmAsync(fixture);

        // Assert
        fixture.Handler.Requests.Count.ShouldBe(2);
        revalidated.ShouldBeSameAs(first);
        third.ShouldBeSameAs(first);
    }

    [TestMethod]
    public async Task GetIpMaturityModelAsync_AfterMaxAgeWhenContentChanged_ReplacesTheCachedImmAndRevalidatesWithTheNewETag()
    {
        // Arrange
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingInSequence(
            () => StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"v1\"", TimeSpan.Zero),
            () => StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsNone, "\"v2\"", TimeSpan.Zero),
            () => StubHttpMessageHandler.NotModified("\"v2\"", TimeSpan.Zero));

        // Act
        IpMaturityModel original = await GetImmAsync(fixture);
        IpMaturityModel changed = await GetImmAsync(fixture);
        IpMaturityModel revalidated = await GetImmAsync(fixture);

        // Assert
        fixture.Handler.Requests.Select(r => r.IfNoneMatch).ShouldBe(
        [
            [],
            ["\"v1\""],
            ["\"v2\""],
        ]);
        original.Rules.Single().Measures.Single().Score.ShouldBe(1);
        changed.ShouldNotBeSameAs(original);
        changed.Rules.Single().Measures.Single().Score.ShouldBe(0);
        revalidated.ShouldBeSameAs(changed);
    }

    [TestMethod]
    [DataRow(HttpStatusCode.NotFound)]
    [DataRow(HttpStatusCode.Gone)]
    public async Task GetIpMaturityModelAsync_GitHubReturnsNotFound_ThrowsContentNotFoundExceptionNamingTheUrl(HttpStatusCode statusCode)
    {
        // Arrange
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingWith(() => new HttpResponseMessage(statusCode));

        // Act
        GitHubContentNotFoundException exception = await Should.ThrowAsync<GitHubContentNotFoundException>(() => GetImmAsync(fixture));

        // Assert
        exception.Url.ShouldBe(new Uri(ImmUrl));
        exception.StatusCode.ShouldBe(statusCode);
        exception.Message.ShouldContain(ImmUrl);
    }

    [TestMethod]
    public async Task GetRuleDefinitionRepositoryAsync_GitHubReturnsNotFound_ThrowsContentNotFoundException()
    {
        // Arrange
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingWith(() => new HttpResponseMessage(HttpStatusCode.NotFound));

        // Act
        GitHubContentNotFoundException exception = await Should.ThrowAsync<GitHubContentNotFoundException>(
            () => fixture.RuleDefinitionSource.GetRuleDefinitionRepositoryAsync("does-not-exist"));

        // Assert
        exception.Url.ShouldBe(new Uri(IpMaturityModelRuleset.DefinitionsUrlForName("does-not-exist")));
    }

    [TestMethod]
    [DataRow(HttpStatusCode.InternalServerError)]
    [DataRow(HttpStatusCode.BadGateway)]
    [DataRow(HttpStatusCode.ServiceUnavailable)]
    [DataRow(HttpStatusCode.TooManyRequests)]
    [DataRow(HttpStatusCode.Forbidden)]
    public async Task GetIpMaturityModelAsync_GitHubReturnsServerError_ThrowsContentUnavailableExceptionWithTheStatusCode(HttpStatusCode statusCode)
    {
        // Arrange
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingWith(() => new HttpResponseMessage(statusCode));

        // Act
        GitHubContentUnavailableException exception = await Should.ThrowAsync<GitHubContentUnavailableException>(() => GetImmAsync(fixture));

        // Assert
        exception.Url.ShouldBe(new Uri(ImmUrl));
        exception.StatusCode.ShouldBe(statusCode);
    }

    [TestMethod]
    public async Task GetIpMaturityModelAsync_RequestTimesOut_ThrowsContentUnavailableException()
    {
        // Arrange
        // This is what HttpClient throws when its Timeout elapses.
        TaskCanceledException timeout = new("The request was canceled due to the configured HttpClient.Timeout.", new TimeoutException());
        using ImmSourceFixture fixture = new((_, _) => Task.FromException<HttpResponseMessage>(timeout));

        // Act
        GitHubContentUnavailableException exception = await Should.ThrowAsync<GitHubContentUnavailableException>(() => GetImmAsync(fixture));

        // Assert
        exception.Url.ShouldBe(new Uri(ImmUrl));
        exception.StatusCode.ShouldBeNull();
        exception.InnerException.ShouldBeSameAs(timeout);
    }

    [TestMethod]
    public async Task GetIpMaturityModelAsync_ConnectionFails_ThrowsContentUnavailableException()
    {
        // Arrange
        HttpRequestException connectionFailure = new("No such host is known.");
        using ImmSourceFixture fixture = new((_, _) => Task.FromException<HttpResponseMessage>(connectionFailure));

        // Act
        GitHubContentUnavailableException exception = await Should.ThrowAsync<GitHubContentUnavailableException>(() => GetImmAsync(fixture));

        // Assert
        exception.StatusCode.ShouldBeNull();
        exception.InnerException.ShouldBeSameAs(connectionFailure);
    }

    [TestMethod]
    [DataRow(HttpStatusCode.InternalServerError)]
    [DataRow(HttpStatusCode.ServiceUnavailable)]
    public async Task GetIpMaturityModelAsync_ServerError_IsNotCachedAndIsFetchedAgainOnTheNextRequest(HttpStatusCode statusCode)
    {
        // Arrange
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingInSequence(
            () => new HttpResponseMessage(statusCode),
            () => StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"v1\"", OneHour));

        // Act
        await Should.ThrowAsync<GitHubContentUnavailableException>(() => GetImmAsync(fixture));
        IpMaturityModel retried = await GetImmAsync(fixture);

        // Assert
        fixture.Handler.Requests.Count.ShouldBe(2);
        fixture.Handler.Requests[1].IfNoneMatch.ShouldBeEmpty();
        retried.Rules.Single().Measures.Single().Score.ShouldBe(1);
    }

    // A README showing many badges for a project without an imm.yaml would otherwise ask GitHub for it once per badge.
    [TestMethod]
    public async Task GetIpMaturityModelAsync_NotFoundRequestedAgainWithinNegativeCacheWindow_IsNotFetchedAgain()
    {
        // Arrange
        FakeSystemClock clock = new();
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingInSequence(
            clock,
            () => new HttpResponseMessage(HttpStatusCode.NotFound),
            () => StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"v1\"", OneHour));

        // Act
        await Should.ThrowAsync<GitHubContentNotFoundException>(() => GetImmAsync(fixture));
        clock.Advance(TimeSpan.FromSeconds(59));
        GitHubContentNotFoundException second = await Should.ThrowAsync<GitHubContentNotFoundException>(() => GetImmAsync(fixture));

        // Assert
        fixture.Handler.Requests.Count.ShouldBe(1);
        second.Url.ShouldBe(new Uri(ImmUrl));
    }

    [TestMethod]
    public async Task GetIpMaturityModelAsync_NotFoundRequestedAfterNegativeCacheWindow_IsFetchedAgain()
    {
        // Arrange
        FakeSystemClock clock = new();
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingInSequence(
            clock,
            () => new HttpResponseMessage(HttpStatusCode.NotFound),
            () => StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"v1\"", OneHour));

        // Act
        await Should.ThrowAsync<GitHubContentNotFoundException>(() => GetImmAsync(fixture));
        clock.Advance(TimeSpan.FromSeconds(61));
        IpMaturityModel afterWindow = await GetImmAsync(fixture);

        // Assert
        fixture.Handler.Requests.Count.ShouldBe(2);
        afterWindow.Rules.Single().Measures.Single().Score.ShouldBe(1);
    }

    [TestMethod]
    public async Task GetIpMaturityModelAsync_NotFoundWithNoStore_IsFetchedAgainOnTheNextRequest()
    {
        // Arrange
        FakeSystemClock clock = new();
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingInSequence(
            clock,
            () => StubHttpMessageHandler.WithCacheControl(new HttpResponseMessage(HttpStatusCode.NotFound), "no-store"),
            () => StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"v1\"", OneHour));

        // Act
        await Should.ThrowAsync<GitHubContentNotFoundException>(() => GetImmAsync(fixture));
        IpMaturityModel retried = await GetImmAsync(fixture);

        // Assert
        fixture.Handler.Requests.Count.ShouldBe(2);
        retried.Rules.Single().Measures.Single().Score.ShouldBe(1);
    }

    [TestMethod]
    public async Task GetIpMaturityModelAsync_ConcurrentRequestsForAMissingImm_ShareASingleRequestToGitHub()
    {
        // Arrange
        TaskCompletionSource<HttpResponseMessage> gitHubResponse = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using ImmSourceFixture fixture = new((_, _) => gitHubResponse.Task);

        // Act
        Task<IpMaturityModel> first = GetImmAsync(fixture);
        Task<IpMaturityModel> second = GetImmAsync(fixture);
        Task<IpMaturityModel> third = GetImmAsync(fixture);
        gitHubResponse.SetResult(new HttpResponseMessage(HttpStatusCode.NotFound));

        // Assert
        await Should.ThrowAsync<GitHubContentNotFoundException>(() => first);
        await Should.ThrowAsync<GitHubContentNotFoundException>(() => second);
        await Should.ThrowAsync<GitHubContentNotFoundException>(() => third);
        fixture.Handler.Requests.Count.ShouldBe(1);
    }

    // Stale-if-error: while GitHub is unavailable, the content it last sent is better than no badge.
    [TestMethod]
    public async Task GetIpMaturityModelAsync_ServerErrorAfterSuccess_ServesTheStaleImmAndTriesGitHubAgainNextTime()
    {
        // Arrange
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingInSequence(
            () => StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"v1\"", TimeSpan.Zero),
            () => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
            () => StubHttpMessageHandler.NotModified("\"v1\"", TimeSpan.Zero));

        // Act
        IpMaturityModel first = await GetImmAsync(fixture);
        IpMaturityModel duringOutage = await GetImmAsync(fixture);
        IpMaturityModel afterOutage = await GetImmAsync(fixture);

        // Assert
        fixture.Handler.Requests.Select(r => r.IfNoneMatch).ShouldBe(
        [
            [],
            ["\"v1\""],
            ["\"v1\""],
        ]);
        duringOutage.ShouldBeSameAs(first);
        afterOutage.ShouldBeSameAs(first);
    }

    [TestMethod]
    public async Task GetIpMaturityModelAsync_NotFoundAfterSuccess_ThrowsRatherThanServingTheStaleImm()
    {
        // Arrange
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingInSequence(
            () => StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"v1\"", TimeSpan.Zero),
            () => new HttpResponseMessage(HttpStatusCode.NotFound));
        await GetImmAsync(fixture);

        // Act
        Task<IpMaturityModel> afterRemoval = GetImmAsync(fixture);

        // Assert
        await Should.ThrowAsync<GitHubContentNotFoundException>(() => afterRemoval);
    }

    [TestMethod]
    public async Task GetIpMaturityModelAsync_GitHubReturnsMalformedYaml_ThrowsInvalidImmContentExceptionAndDoesNotCacheTheFailure()
    {
        // Arrange
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingInSequence(
            () => StubHttpMessageHandler.Ok("- Name: 'unterminated", "\"broken\"", OneHour),
            () => StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"fixed\"", OneHour));

        // Act
        InvalidImmContentException exception = await Should.ThrowAsync<InvalidImmContentException>(() => GetImmAsync(fixture));
        IpMaturityModel retried = await GetImmAsync(fixture);

        // Assert
        exception.Url.ShouldBe(new Uri(ImmUrl));
        YamlException yamlError = exception.InnerException.ShouldBeAssignableTo<YamlException>()!;
        exception.Line.ShouldBe(yamlError.Start.Line);
        exception.Column.ShouldBe(yamlError.Start.Column);
        exception.Message.ShouldContain($"(line {yamlError.Start.Line}, column {yamlError.Start.Column})");
        fixture.Handler.Requests.Count.ShouldBe(2);
        fixture.Handler.Requests[1].IfNoneMatch.ShouldBeEmpty();
        retried.Rules.Single().Measures.Single().Score.ShouldBe(1);
    }

    // The ETag of content that could not be deserialized must not be remembered: revalidating with it
    // would get a 304, and the cache would then serve the previous, valid IMM as if it were current,
    // hiding the broken update from the project's owner until the file changed again.
    [TestMethod]
    public async Task GetIpMaturityModelAsync_ValidImmReplacedWithMalformedYaml_KeepsReportingItAsInvalid()
    {
        // Arrange
        using ImmSourceFixture fixture = new((request, callIndex) => Task.FromResult(
            callIndex == 0
                ? StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"v1\"", TimeSpan.Zero)
                : request.Headers.IfNoneMatch.Any(e => e.Tag == "\"v2\"")
                    ? StubHttpMessageHandler.NotModified("\"v2\"", TimeSpan.Zero)
                    : StubHttpMessageHandler.Ok("- Name: 'unterminated", "\"v2\"", TimeSpan.Zero)));
        await GetImmAsync(fixture);

        // Act
        Task<IpMaturityModel> afterBrokenUpdate = GetImmAsync(fixture);
        await Should.ThrowAsync<InvalidImmContentException>(() => afterBrokenUpdate);
        Task<IpMaturityModel> nextRequest = GetImmAsync(fixture);

        // Assert
        await Should.ThrowAsync<InvalidImmContentException>(() => nextRequest);
        fixture.Handler.Requests.Select(r => r.IfNoneMatch).ShouldBe(
        [
            [],
            ["\"v1\""],
            ["\"v1\""],
        ]);
    }

    [TestMethod]
    public async Task GetIpMaturityModelAsync_ConcurrentRequestsForTheSameImm_ShareASingleRequestToGitHub()
    {
        // Arrange
        TaskCompletionSource<HttpResponseMessage> gitHubResponse = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using ImmSourceFixture fixture = new((_, _) => gitHubResponse.Task);

        // Act
        Task<IpMaturityModel> first = GetImmAsync(fixture);
        Task<IpMaturityModel> second = GetImmAsync(fixture);
        Task<IpMaturityModel> third = GetImmAsync(fixture);
        int requestsInFlight = fixture.Handler.Requests.Count;
        gitHubResponse.SetResult(StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"v1\"", OneHour));
        IpMaturityModel[] results = await Task.WhenAll(first, second, third);

        // Assert
        requestsInFlight.ShouldBe(1);
        fixture.Handler.Requests.Count.ShouldBe(1);
        results[1].ShouldBeSameAs(results[0]);
        results[2].ShouldBeSameAs(results[0]);
    }

    [TestMethod]
    public async Task GetIpMaturityModelAsync_RequestedAgainWithinTwentyMinutes_StaysInTheCache()
    {
        // Arrange
        FakeSystemClock clock = new();
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingWith(
            () => StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"v1\"", OneHour),
            clock);

        // Act
        IpMaturityModel first = await GetImmAsync(fixture);
        clock.Advance(TimeSpan.FromMinutes(19));
        IpMaturityModel second = await GetImmAsync(fixture);
        clock.Advance(TimeSpan.FromMinutes(19));
        IpMaturityModel third = await GetImmAsync(fixture);

        // Assert
        fixture.Handler.Requests.Count.ShouldBe(1);
        second.ShouldBeSameAs(first);
        third.ShouldBeSameAs(first);
    }

    [TestMethod]
    public async Task GetIpMaturityModelAsync_NotRequestedForMoreThanTwentyMinutes_IsEvictedAndFetchedAgainUnconditionally()
    {
        // Arrange
        FakeSystemClock clock = new();
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingWith(
            () => StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"v1\"", OneHour),
            clock);

        // Act
        IpMaturityModel first = await GetImmAsync(fixture);
        clock.Advance(TimeSpan.FromMinutes(21));
        IpMaturityModel afterEviction = await GetImmAsync(fixture);

        // Assert
        fixture.Handler.Requests.Count.ShouldBe(2);
        fixture.Handler.Requests[1].IfNoneMatch.ShouldBeEmpty();
        afterEviction.ShouldNotBeSameAs(first);
    }

    private static Task<IpMaturityModel> GetImmAsync(ImmSourceFixture fixture) =>
        fixture.ImmSource.GetIpMaturityModelAsync(Owner, Project, Branch);
}
