using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;

namespace Endjin.Imm.Tests.TestData;

/// <summary>
/// Stands in for GitHub at the HTTP boundary: records each request and answers it with a
/// response chosen by the test, so that no test touches the network.
/// </summary>
/// <param name="respond">
/// Produces the response for a request, given the request and its zero-based position in the
/// sequence of requests the handler has received.
/// </param>
internal sealed class StubHttpMessageHandler(Func<HttpRequestMessage, int, Task<HttpResponseMessage>> respond)
    : HttpMessageHandler
{
    private readonly ConcurrentQueue<CapturedRequest> requests = new();
    private int requestCount;

    /// <summary>
    /// Gets the requests received so far, in the order they arrived. A request is recorded as
    /// soon as it arrives, before its response is produced, so in-flight requests are included.
    /// </summary>
    public IReadOnlyList<CapturedRequest> Requests => [.. this.requests];

    /// <summary>
    /// Creates a 200 OK response carrying the YAML body and optional caching headers.
    /// </summary>
    public static HttpResponseMessage Ok(string yaml, string? eTag = null, TimeSpan? maxAge = null)
    {
        HttpResponseMessage response = new(HttpStatusCode.OK)
        {
            Content = new StringContent(yaml),
        };
        SetCachingHeaders(response, eTag, maxAge);
        return response;
    }

    /// <summary>
    /// Creates a 304 Not Modified response, as GitHub returns for a conditional request whose
    /// ETag still matches.
    /// </summary>
    public static HttpResponseMessage NotModified(string eTag, TimeSpan? maxAge = null)
    {
        HttpResponseMessage response = new(HttpStatusCode.NotModified);
        SetCachingHeaders(response, eTag, maxAge);
        return response;
    }

    /// <inheritdoc/>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        int callIndex = Interlocked.Increment(ref this.requestCount) - 1;
        this.requests.Enqueue(new CapturedRequest(
            request.RequestUri!,
            [.. request.Headers.IfNoneMatch.Select(tag => tag.Tag)]));

        return respond(request, callIndex);
    }

    private static void SetCachingHeaders(HttpResponseMessage response, string? eTag, TimeSpan? maxAge)
    {
        if (eTag is not null)
        {
            response.Headers.ETag = new EntityTagHeaderValue(eTag);
        }

        if (maxAge is TimeSpan age)
        {
            response.Headers.CacheControl = new CacheControlHeaderValue { MaxAge = age };
        }
    }
}

/// <summary>
/// A request received by <see cref="StubHttpMessageHandler"/>.
/// </summary>
/// <param name="Uri">The requested URI.</param>
/// <param name="IfNoneMatch">The quoted ETags sent in the If-None-Match header, if any.</param>
internal sealed record CapturedRequest(Uri Uri, IReadOnlyList<string> IfNoneMatch);
