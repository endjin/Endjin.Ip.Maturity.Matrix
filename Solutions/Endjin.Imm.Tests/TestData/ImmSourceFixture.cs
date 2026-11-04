using Endjin.Imm.Contracts;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;

namespace Endjin.Imm.Tests.TestData;

/// <summary>
/// Builds the GitHub sources through the same public registration the host uses
/// (<see cref="ImmServiceCollectionExtensions.AddImmSources"/>), with GitHub replaced by a
/// <see cref="StubHttpMessageHandler"/> and a memory cache of the test's own.
/// </summary>
internal sealed class ImmSourceFixture : IDisposable
{
    private readonly ServiceProvider serviceProvider;

    /// <summary>
    /// Creates the fixture.
    /// </summary>
    /// <param name="respond">Produces GitHub's response to each request.</param>
    /// <param name="clock">
    /// The clock that drives the memory cache's sliding expiration and the freshness of cached
    /// content. Defaults to the system clock.
    /// </param>
    public ImmSourceFixture(
        Func<HttpRequestMessage, int, Task<HttpResponseMessage>> respond,
        FakeSystemClock? clock = null)
    {
        this.Handler = new StubHttpMessageHandler(respond);

        ServiceCollection services = new();
        services.AddSingleton(new HttpClient(this.Handler));
        this.MemoryCache = new MemoryCache(new MemoryCacheOptions { Clock = (ISystemClock?)clock ?? new SystemClock() });
        services.AddSingleton<IMemoryCache>(this.MemoryCache);
        if (clock is not null)
        {
            services.AddSingleton<TimeProvider>(clock);
        }

        services.AddImmSources();

        this.serviceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Gets the stub that stands in for GitHub.
    /// </summary>
    public StubHttpMessageHandler Handler { get; }

    /// <summary>
    /// Gets the memory cache the sources store their content in.
    /// </summary>
    public MemoryCache MemoryCache { get; }

    /// <summary>
    /// Gets the source of project IMMs.
    /// </summary>
    public IIpMaturityModelSource ImmSource => this.serviceProvider.GetRequiredService<IIpMaturityModelSource>();

    /// <summary>
    /// Gets the source of rule definitions.
    /// </summary>
    public IRuleDefinitionRepositorySource RuleDefinitionSource => this.serviceProvider.GetRequiredService<IRuleDefinitionRepositorySource>();

    /// <summary>
    /// Creates a fixture in which GitHub answers every request with the same response.
    /// </summary>
    public static ImmSourceFixture RespondingWith(Func<HttpResponseMessage> response, FakeSystemClock? clock = null) =>
        new((_, _) => Task.FromResult(response()), clock);

    /// <summary>
    /// Creates a fixture in which GitHub answers the requests in turn with the given responses.
    /// </summary>
    public static ImmSourceFixture RespondingInSequence(params Func<HttpResponseMessage>[] responses) =>
        new((_, callIndex) => Task.FromResult(responses[callIndex]()));

    /// <summary>
    /// Creates a fixture in which GitHub answers the requests in turn with the given responses,
    /// and in which time moves only when the test advances <paramref name="clock"/>.
    /// </summary>
    public static ImmSourceFixture RespondingInSequence(FakeSystemClock clock, params Func<HttpResponseMessage>[] responses) =>
        new((_, callIndex) => Task.FromResult(responses[callIndex]()), clock);

    /// <inheritdoc/>
    public void Dispose() => this.serviceProvider.Dispose();
}
