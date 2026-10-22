using System.Net;
using System.Net.Http.Headers;
using System.Text;
using BenchmarkDotNet.Attributes;
using Endjin.Imm.Caching;
using Endjin.Imm.Contracts;
using Endjin.Imm.Domain;
using Endjin.Imm.Serialisation;
using Microsoft.Extensions.Caching.Memory;

namespace Endjin.Imm.Benchmarks;

/// <summary>
/// The per-request cost of a warm <see cref="HttpDeserializingCache{T}"/> hit: building the cache
/// key, taking the lock, the <see cref="IMemoryCache"/> lookup and handing back the completed task.
/// </summary>
/// <remarks>
/// <para>
/// The HTTP layer is a stub that serves the embedded YAML with <c>Cache-Control: max-age=86400</c>
/// and an ETag, so once primed the cache record stays fresh for a day and every call is a pure hit
/// with no revalidation request. Setup verifies this by counting stub calls.
/// </para>
/// </remarks>
[Config(typeof(ImmBenchmarkConfig))]
[BenchmarkCategory("Cache")]
public class HttpDeserializingCacheBenchmarks
{
    private const string ImmUrl = "https://raw.githubusercontent.com/endjin/Endjin.Ip.Maturity.Model/master/imm.yaml";

    private static readonly string RuleSetUrl = IpMaturityModelRuleset.RuleSetDefinitionsUrl;

    private MemoryCache memoryCache = null!;
    private HttpClient httpClient = null!;
    private StubHandler handler = null!;
    private HttpDeserializingCache<IpMaturityModel> immCache = null!;
    private HttpDeserializingCache<IRuleDefinitionRepository> ruleSetCache = null!;

    [GlobalSetup]
    public async Task Setup()
    {
        this.handler = new StubHandler(new Dictionary<string, string>
        {
            [ImmUrl] = SampleData.LoadEmbedded(SampleData.DefaultImmResourceName),
            [RuleSetUrl] = SampleData.LoadEmbedded(SampleData.RuleSetResourceName),
        });
        this.httpClient = new HttpClient(this.handler);
        this.memoryCache = new MemoryCache(new MemoryCacheOptions());

        this.immCache = new HttpDeserializingCache<IpMaturityModel>(this.httpClient, this.memoryCache, new IpMaturityModelHttpDeserializer());
        this.ruleSetCache = new HttpDeserializingCache<IRuleDefinitionRepository>(this.httpClient, this.memoryCache, new RuleDefinitionRepositoryHttpDeserializer());

        // Prime both entries.
        IpMaturityModel imm = await this.immCache.GetAsync(ImmUrl).ConfigureAwait(false);
        IRuleDefinitionRepository rules = await this.ruleSetCache.GetAsync(RuleSetUrl).ConfigureAwait(false);
        if (imm.Rules.Count == 0 || rules.GetAll().Count == 0)
        {
            throw new InvalidOperationException("Priming the cache produced empty results.");
        }

        int primed = this.handler.CallCount;

        // Confirm that subsequent calls are hits that complete synchronously without touching HTTP.
        for (int i = 0; i < 100; i++)
        {
            Task<IpMaturityModel> immTask = this.immCache.GetAsync(ImmUrl);
            Task<IRuleDefinitionRepository> rulesTask = this.ruleSetCache.GetAsync(RuleSetUrl);
            if (!immTask.IsCompletedSuccessfully || !rulesTask.IsCompletedSuccessfully)
            {
                throw new InvalidOperationException("A primed cache lookup did not return a completed task.");
            }
        }

        if (this.handler.CallCount != primed || primed != 2)
        {
            throw new InvalidOperationException(
                $"Expected exactly 2 HTTP calls (priming only), but the stub was called {this.handler.CallCount} times.");
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        this.httpClient.Dispose();
        this.memoryCache.Dispose();
    }

    [Benchmark(Baseline = true)]
    public Task<IpMaturityModel> GetAsync_CacheHit_Imm() => this.immCache.GetAsync(ImmUrl);

    [Benchmark]
    public Task<IRuleDefinitionRepository> GetAsync_CacheHit_RuleSet() => this.ruleSetCache.GetAsync(RuleSetUrl);

    private sealed class StubHandler(IReadOnlyDictionary<string, string> content) : HttpMessageHandler
    {
        private int callCount;

        public int CallCount => Volatile.Read(ref this.callCount);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref this.callCount);

            string url = request.RequestUri!.AbsoluteUri;
            if (!content.TryGetValue(url, out string? body))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request });
            }

            HttpResponseMessage response = new(HttpStatusCode.OK)
            {
                RequestMessage = request,
                Content = new StringContent(body, Encoding.UTF8, "text/plain"),
            };
            response.Headers.CacheControl = new CacheControlHeaderValue { MaxAge = TimeSpan.FromSeconds(86400) };
            response.Headers.ETag = new EntityTagHeaderValue("\"benchmark-v1\"");
            return Task.FromResult(response);
        }
    }
}
