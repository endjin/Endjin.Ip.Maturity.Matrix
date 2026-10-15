using System.Net;
using System.Net.Http.Headers;
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
/// revalidated with a conditional request, failures are never cached, and an entry that nobody
/// has asked for in 20 minutes is dropped. Content marked no-cache is revalidated every time it
/// is used, and content marked no-store is not kept at all.
/// </summary>
[TestClass]
public class GitHubContentCachingTests
{
    private const string Owner = "endjin";
    private const string Project = "Endjin.Ip.Maturity.Model";
    private const string Branch = "master";

    private static readonly TimeSpan OneHour = TimeSpan.FromHours(1);

    [TestMethod]
    public async Task GetIpMaturityMatrixAsync_SecondRequestWithinMaxAge_IsServedFromTheCacheWithoutContactingGitHub()
    {
        // Arrange
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingWith(
            () => StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"v1\"", OneHour));

        // Act
        IpMaturityMatrix first = await GetImmAsync(fixture);
        IpMaturityMatrix second = await GetImmAsync(fixture);

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

    // Without a max-age, content is treated as fresh for a default of one minute. Only the start of that window can be
    // tested without waiting, because freshness is measured with Environment.TickCount64.
    [TestMethod]
    [DataRow(null)]
    [DataRow("public")]
    public async Task GetIpMaturityMatrixAsync_ResponseWithoutMaxAge_IsCachedForTheDefaultWindow(string? cacheControl)
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
        IpMaturityMatrix first = await GetImmAsync(fixture);
        IpMaturityMatrix second = await GetImmAsync(fixture);

        // Assert
        fixture.Handler.Requests.Count.ShouldBe(1);
        second.ShouldBeSameAs(first);
    }

    [TestMethod]
    [DataRow("no-cache")]
    [DataRow("public, no-cache")]
    [DataRow("no-cache, max-age=3600")]
    public async Task GetIpMaturityMatrixAsync_ResponseWithNoCache_IsRevalidatedWithETagOnEveryRequest(string cacheControl)
    {
        // Arrange
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingInSequence(
            () => StubHttpMessageHandler.WithCacheControl(
                StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"v1\""), cacheControl),
            () => StubHttpMessageHandler.WithCacheControl(StubHttpMessageHandler.NotModified("\"v1\""), cacheControl),
            () => StubHttpMessageHandler.WithCacheControl(StubHttpMessageHandler.NotModified("\"v1\""), cacheControl));

        // Act
        IpMaturityMatrix first = await GetImmAsync(fixture);
        IpMaturityMatrix second = await GetImmAsync(fixture);
        IpMaturityMatrix third = await GetImmAsync(fixture);

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
    public async Task GetIpMaturityMatrixAsync_ResponseWithNoStore_IsFetchedAgainUnconditionallyOnEveryRequest(string cacheControl)
    {
        // Arrange
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingWith(
            () => StubHttpMessageHandler.WithCacheControl(
                StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"v1\""), cacheControl));

        // Act
        IpMaturityMatrix first = await GetImmAsync(fixture);
        IpMaturityMatrix second = await GetImmAsync(fixture);

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
    public async Task GetIpMaturityMatrixAsync_ResponseWithNoStoreReceivedSynchronously_IsNotLeftInTheMemoryCache()
    {
        // Arrange
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingWith(
            () => StubHttpMessageHandler.WithCacheControl(
                StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"v1\""), "no-store"));

        // Act
        IpMaturityMatrix imm = await GetImmAsync(fixture);

        // Assert
        imm.Rules.Single().Measures.Single().Score.ShouldBe(1);
        fixture.MemoryCache.Count.ShouldBe(0);
    }

    [TestMethod]
    public async Task GetIpMaturityMatrixAsync_ResponseWithNoStoreReceivedAsynchronously_IsRemovedFromTheMemoryCacheAndStillReachesEveryWaitingCaller()
    {
        // Arrange
        TaskCompletionSource<HttpResponseMessage> gitHubResponse = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using ImmSourceFixture fixture = new((_, _) => gitHubResponse.Task);

        // Act
        Task<IpMaturityMatrix> first = GetImmAsync(fixture);
        Task<IpMaturityMatrix> second = GetImmAsync(fixture);
        int entriesWhileInFlight = fixture.MemoryCache.Count;
        gitHubResponse.SetResult(StubHttpMessageHandler.WithCacheControl(
            StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"v1\""), "no-store"));
        IpMaturityMatrix[] results = await Task.WhenAll(first, second);

        // Assert
        entriesWhileInFlight.ShouldBe(1);
        fixture.Handler.Requests.Count.ShouldBe(1);
        results[1].ShouldBeSameAs(results[0]);
        fixture.MemoryCache.Count.ShouldBe(0);
    }

    // A 304 updates only the headers it carries (RFC 9111, section 4.3.4), so one without Cache-Control must not replace a
    // stored no-cache policy with the one-minute default.
    [TestMethod]
    public async Task GetIpMaturityMatrixAsync_NotModifiedResponseWithoutCacheControl_KeepsTheNoCachePolicyOfTheOriginalResponse()
    {
        // Arrange
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingInSequence(
            () => StubHttpMessageHandler.WithCacheControl(
                StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"v1\""), "no-cache"),
            () => StubHttpMessageHandler.NotModified("\"v1\""),
            () => StubHttpMessageHandler.NotModified("\"v1\""));

        // Act
        IpMaturityMatrix first = await GetImmAsync(fixture);
        IpMaturityMatrix second = await GetImmAsync(fixture);
        IpMaturityMatrix third = await GetImmAsync(fixture);

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
    public async Task GetIpMaturityMatrixAsync_NotModifiedResponseWithItsOwnCacheControl_ReplacesTheStoredPolicy()
    {
        // Arrange
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingInSequence(
            () => StubHttpMessageHandler.WithCacheControl(
                StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"v1\""), "no-cache"),
            () => StubHttpMessageHandler.NotModified("\"v1\"", OneHour));

        // Act
        IpMaturityMatrix first = await GetImmAsync(fixture);
        IpMaturityMatrix revalidated = await GetImmAsync(fixture);
        IpMaturityMatrix third = await GetImmAsync(fixture);

        // Assert
        fixture.Handler.Requests.Count.ShouldBe(2);
        revalidated.ShouldBeSameAs(first);
        third.ShouldBeSameAs(first);
    }

    [TestMethod]
    public async Task GetIpMaturityMatrixAsync_NotModifiedResponseWithoutAnETag_KeepsRevalidatingWithThePreviousETag()
    {
        // Arrange
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingInSequence(
            () => StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"v1\"", TimeSpan.Zero),
            () => StubHttpMessageHandler.NotModified(null, TimeSpan.Zero),
            () => StubHttpMessageHandler.NotModified("\"v1\"", TimeSpan.Zero));

        // Act
        IpMaturityMatrix first = await GetImmAsync(fixture);
        IpMaturityMatrix second = await GetImmAsync(fixture);
        IpMaturityMatrix third = await GetImmAsync(fixture);

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
    public async Task GetIpMaturityMatrixAsync_AfterMaxAgeWhenContentUnchanged_RevalidatesWithETagAndReusesTheCachedImm()
    {
        // Arrange
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingInSequence(
            () => StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"v1\"", TimeSpan.Zero),
            () => StubHttpMessageHandler.NotModified("\"v1\"", TimeSpan.Zero));

        // Act
        IpMaturityMatrix first = await GetImmAsync(fixture);
        IpMaturityMatrix second = await GetImmAsync(fixture);

        // Assert
        fixture.Handler.Requests.Count.ShouldBe(2);
        fixture.Handler.Requests[0].IfNoneMatch.ShouldBeEmpty();
        fixture.Handler.Requests[1].IfNoneMatch.ShouldBe(["\"v1\""]);
        second.ShouldBeSameAs(first);
    }

    [TestMethod]
    public async Task GetIpMaturityMatrixAsync_NotModifiedResponse_StartsANewFreshnessWindow()
    {
        // Arrange
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingInSequence(
            () => StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"v1\"", TimeSpan.Zero),
            () => StubHttpMessageHandler.NotModified("\"v1\"", OneHour));

        // Act
        IpMaturityMatrix first = await GetImmAsync(fixture);
        IpMaturityMatrix revalidated = await GetImmAsync(fixture);
        IpMaturityMatrix third = await GetImmAsync(fixture);

        // Assert
        fixture.Handler.Requests.Count.ShouldBe(2);
        revalidated.ShouldBeSameAs(first);
        third.ShouldBeSameAs(first);
    }

    [TestMethod]
    public async Task GetIpMaturityMatrixAsync_AfterMaxAgeWhenContentChanged_ReplacesTheCachedImmAndRevalidatesWithTheNewETag()
    {
        // Arrange
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingInSequence(
            () => StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"v1\"", TimeSpan.Zero),
            () => StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsNone, "\"v2\"", TimeSpan.Zero),
            () => StubHttpMessageHandler.NotModified("\"v2\"", TimeSpan.Zero));

        // Act
        IpMaturityMatrix original = await GetImmAsync(fixture);
        IpMaturityMatrix changed = await GetImmAsync(fixture);
        IpMaturityMatrix revalidated = await GetImmAsync(fixture);

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
    [DataRow(HttpStatusCode.InternalServerError)]
    [DataRow(HttpStatusCode.NotFound)]
    public async Task GetIpMaturityMatrixAsync_GitHubReturnsAnError_ThrowsWithTheStatusCodeAndDoesNotCacheTheFailure(HttpStatusCode statusCode)
    {
        // Arrange
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingInSequence(
            () => new HttpResponseMessage(statusCode),
            () => StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"v1\"", OneHour));

        // Act
        HttpRequestException exception = await Should.ThrowAsync<HttpRequestException>(() => GetImmAsync(fixture));
        IpMaturityMatrix retried = await GetImmAsync(fixture);

        // Assert
        exception.StatusCode.ShouldBe(statusCode);
        fixture.Handler.Requests.Count.ShouldBe(2);
        fixture.Handler.Requests[1].IfNoneMatch.ShouldBeEmpty();
        retried.Rules.Single().Measures.Single().Score.ShouldBe(1);
    }

    [TestMethod]
    public async Task GetIpMaturityMatrixAsync_GitHubReturnsMalformedYaml_ThrowsAndDoesNotCacheTheFailure()
    {
        // Arrange
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingInSequence(
            () => StubHttpMessageHandler.Ok("- Name: 'unterminated", "\"broken\"", OneHour),
            () => StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"fixed\"", OneHour));

        // Act
        await Should.ThrowAsync<YamlException>(() => GetImmAsync(fixture));
        IpMaturityMatrix retried = await GetImmAsync(fixture);

        // Assert
        fixture.Handler.Requests.Count.ShouldBe(2);
        fixture.Handler.Requests[1].IfNoneMatch.ShouldBeEmpty();
        retried.Rules.Single().Measures.Single().Score.ShouldBe(1);
    }

    [TestMethod]
    public async Task GetIpMaturityMatrixAsync_ConcurrentRequestsForTheSameImm_ShareASingleRequestToGitHub()
    {
        // Arrange
        TaskCompletionSource<HttpResponseMessage> gitHubResponse = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using ImmSourceFixture fixture = new((_, _) => gitHubResponse.Task);

        // Act
        Task<IpMaturityMatrix> first = GetImmAsync(fixture);
        Task<IpMaturityMatrix> second = GetImmAsync(fixture);
        Task<IpMaturityMatrix> third = GetImmAsync(fixture);
        int requestsInFlight = fixture.Handler.Requests.Count;
        gitHubResponse.SetResult(StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"v1\"", OneHour));
        IpMaturityMatrix[] results = await Task.WhenAll(first, second, third);

        // Assert
        requestsInFlight.ShouldBe(1);
        fixture.Handler.Requests.Count.ShouldBe(1);
        results[1].ShouldBeSameAs(results[0]);
        results[2].ShouldBeSameAs(results[0]);
    }

    [TestMethod]
    public async Task GetIpMaturityMatrixAsync_RequestedAgainWithinTwentyMinutes_StaysInTheCache()
    {
        // Arrange
        FakeSystemClock clock = new();
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingWith(
            () => StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"v1\"", OneHour),
            clock);

        // Act
        IpMaturityMatrix first = await GetImmAsync(fixture);
        clock.Advance(TimeSpan.FromMinutes(19));
        IpMaturityMatrix second = await GetImmAsync(fixture);
        clock.Advance(TimeSpan.FromMinutes(19));
        IpMaturityMatrix third = await GetImmAsync(fixture);

        // Assert
        fixture.Handler.Requests.Count.ShouldBe(1);
        second.ShouldBeSameAs(first);
        third.ShouldBeSameAs(first);
    }

    [TestMethod]
    public async Task GetIpMaturityMatrixAsync_NotRequestedForMoreThanTwentyMinutes_IsEvictedAndFetchedAgainUnconditionally()
    {
        // Arrange
        FakeSystemClock clock = new();
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingWith(
            () => StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, "\"v1\"", OneHour),
            clock);

        // Act
        IpMaturityMatrix first = await GetImmAsync(fixture);
        clock.Advance(TimeSpan.FromMinutes(21));
        IpMaturityMatrix afterEviction = await GetImmAsync(fixture);

        // Assert
        fixture.Handler.Requests.Count.ShouldBe(2);
        fixture.Handler.Requests[1].IfNoneMatch.ShouldBeEmpty();
        afterEviction.ShouldNotBeSameAs(first);
    }

    private static Task<IpMaturityMatrix> GetImmAsync(ImmSourceFixture fixture) =>
        fixture.ImmSource.GetIpMaturityMatrixAsync(Owner, Project, Branch);
}
