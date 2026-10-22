namespace Endjin.Imm.Caching
{
    using Microsoft.Extensions.Caching.Memory;
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using YamlDotNet.Core;

    /// <summary>
    /// Cache that fetches data via HTTP, deserializes it, and caches the deserialized results,
    /// honouring the cache age and ETag headers typically returned by GitHub.
    /// </summary>
    /// <typeparam name="T">
    /// The deserialized type. An implementation of <see cref="IHttpDeserializer{T}"/> must be
    /// available for this type for this cache to work.
    /// </typeparam>
    /// <remarks>
    /// <para>
    /// The main purpose of this is to avoid repeatedly asking GitHub for the same resource
    /// again and again when fetching rule definitions and IMMs, by taking into account the caching
    /// headers GitHub returns. We hold onto cached content using a sliding 20 minute window. When
    /// the cached content reaches the maximum age indicated by GitHub's last response, we perform
    /// a conditional GET using the ETag previously returned by GitHub, so if the content hasn't
    /// changed since last time (and with this application, it usually won't have), GitHub can
    /// reply with 304 Not Modified and we reuse the content we already deserialized.
    /// </para>
    /// <para>
    /// A response with no Cache-Control header, or one with no max-age, is treated as fresh for
    /// one minute. A response marked <c>no-cache</c> is kept but revalidated every time it is
    /// used. A response marked <c>no-store</c> is not kept for reuse at all, so each request
    /// fetches the content again, unconditionally. (GitHub does not send <c>no-store</c> for the
    /// public content we fetch, so honouring it as the origin asks costs nothing in practice.)
    /// A 304 that does not repeat the ETag leaves the stored one in place, and a 304 with no
    /// Cache-Control header leaves the stored caching policy in place (RFC 9111, section 4.3.4).
    /// </para>
    /// <para>
    /// Failures are reported as typed exceptions. A 404 Not Found or 410 Gone becomes a
    /// <see cref="GitHubContentNotFoundException"/>, which is cached for one minute (unless the
    /// response is marked <c>no-store</c>), so that a README full of badges for a missing IMM
    /// asks GitHub only once a minute. Any other error status, and a request that fails or times
    /// out, becomes a <see cref="GitHubContentUnavailableException"/>, and content that cannot be
    /// deserialized becomes an <see cref="InvalidImmContentException"/>; neither of those is
    /// cached. When GitHub is unavailable but content fetched earlier is still held, that content
    /// is returned instead (stale-if-error), and the next request tries GitHub again.
    /// </para>
    /// <para>
    /// When something listens to <see cref="ImmTelemetry.ActivitySourceName"/>, each call to
    /// <see cref="GetAsync"/> is traced as an <c>imm.content.get</c> span, which lasts until the
    /// caller's task completes and records in <c>imm.cache.result</c> how the request was served:
    /// <c>hit</c> (fresh content, or the fresh knowledge that there is none, was reused),
    /// <c>coalesced</c> (another caller's fetch was already in flight, and was joined; the span
    /// links to that fetch's span, which belongs to the first caller's trace), <c>miss</c> (nothing
    /// was held, so a fetch started) or <c>revalidate</c> (what was held could not be reused, so a
    /// fetch started). A fetch is an <c>imm.content.fetch</c> span, a child of the
    /// <c>imm.content.get</c> span that started it, recording whether the request was conditional,
    /// GitHub's status code, whether the content was unchanged (304), and whether stale content was
    /// served because GitHub was unavailable; and reading the content is an
    /// <c>imm.content.deserialize</c> span inside the fetch. A span whose operation fails has
    /// status <see cref="ActivityStatusCode.Error"/> and an <c>error.type</c> tag naming the
    /// exception's type. With no listener none of this happens: <see cref="GetAsync"/> hands out
    /// the shared task itself, so tracing that nobody collects adds no work or allocation to a
    /// request.
    /// </para>
    /// </remarks>
    internal class HttpDeserializingCache<T>
        where T : class
    {
        private static readonly TimeSpan DefaultFreshnessLifetime = TimeSpan.FromMinutes(1);

        private readonly object sync = new();
        private readonly IMemoryCache memoryCache;
        private readonly HttpClient httpClient;
        private readonly IHttpDeserializer<T> deserializer;
        private readonly TimeProvider timeProvider;

        public HttpDeserializingCache(
            HttpClient httpClient,
            IMemoryCache memoryCache,
            IHttpDeserializer<T> deserializer,
            TimeProvider? timeProvider = null)
        {
            this.memoryCache = memoryCache;
            this.httpClient = httpClient;
            this.deserializer = deserializer;
            this.timeProvider = timeProvider ?? TimeProvider.System;
        }

        public Task<T> GetAsync(string url)
        {
            // Without a listener, hand out the shared task itself, as the cache always has. Only when
            // the span will be recorded is it worth wrapping the task in one of our own.
            return ImmTelemetry.Source.HasListeners()
                ? this.GetWithTracingAsync(url)
                : this.GetOrFetch(url, out _, out _);
        }

        /// <summary>
        /// Traces a call to <see cref="GetAsync"/> as an <c>imm.content.get</c> span.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The span is started here, inside an async method, rather than in <see cref="GetAsync"/>:
        /// an async method's changes to <see cref="Activity.Current"/> are undone when it returns
        /// to its caller, so the span is the parent of the fetch it starts but never becomes the
        /// parent of whatever the caller does next.
        /// </para>
        /// </remarks>
        private async Task<T> GetWithTracingAsync(string url)
        {
            using Activity? activity = ImmTelemetry.Source.StartActivity("imm.content.get", ActivityKind.Internal);
            activity?.SetTag("url.full", url);

            try
            {
                Task<T> result = this.GetOrFetch(url, out string cacheResult, out ActivityContext inFlightFetch);
                if (activity is not null)
                {
                    activity.SetTag("imm.cache.result", cacheResult);
                    if (inFlightFetch != default)
                    {
                        activity.AddLink(new ActivityLink(inFlightFetch));
                    }
                }

                return await result.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                activity.RecordFailure(ex);
                throw;
            }
        }

        /// <summary>
        /// Returns the task that produces the content at <paramref name="url"/>: the one already
        /// held, if it can be reused, or a new fetch.
        /// </summary>
        /// <param name="url">The URL of the content.</param>
        /// <param name="cacheResult">
        /// How the request was served, as reported in the <c>imm.cache.result</c> tag: <c>hit</c>,
        /// <c>coalesced</c>, <c>miss</c> or <c>revalidate</c>.
        /// </param>
        /// <param name="inFlightFetch">
        /// For a coalesced request, the context of the fetch's span, if it has one; otherwise the
        /// default.
        /// </param>
        private Task<T> GetOrFetch(string url, out string cacheResult, out ActivityContext inFlightFetch)
        {
            string cacheKey = $"HttpDeserializingCache:{typeof(T).FullName}:{url}";
            inFlightFetch = default;

            // Holding the lock as we call FetchAsync is a bit weird. (We're not awaiting FetchAsync,
            // which may make it OK but definitely makes it more weird.)
            lock (sync)
            {
                if (this.memoryCache.TryGetValue(cacheKey, out object? entry) && entry is not null)
                {
                    var record = (Record)entry;

                    // In cases where we get multiple requests for the same URL in quick succession
                    // (which will often happen when showing a project README.md in GitHub, which
                    // has multiple IMM badges) we want all of these to be serviced by a single
                    // request. If the cache entry for this item has a task that is not yet
                    // complete we just hand that back out, meaning all concurrent requests will
                    // end up sharing the result of the same underlying fetch.
                    // When the operation in the cache entry is complete we only return it if it
                    // completed successfully (or found that the content does not exist), and
                    // recently enough that it's sufficiently up to date according to the cache
                    // header Max Age returned by the source.
                    if (!record.Result.IsCompleted)
                    {
                        cacheResult = "coalesced";
                        inFlightFetch = record.FetchActivityContext;
                        return record.Result;
                    }
                    else if ((record.Result.IsCompletedSuccessfully || IsNotFound(record.Result)) && this.IsFresh(record))
                    {
                        cacheResult = "hit";
                        return record.Result;
                    }
                    else
                    {
                        cacheResult = "revalidate";
                        record.Result = FetchAsync(url, cacheKey, record, record.ETag);
                        return record.Result;
                    }
                }
                else
                {
                    cacheResult = "miss";
                    var record = new Record();
                    record.Result = FetchAsync(url, cacheKey, record);
                    using (ICacheEntry ce = this.memoryCache.CreateEntry(cacheKey))
                    {
                        ce.Value = record;
                        ce.SlidingExpiration = TimeSpan.FromMinutes(20);
                    }

                    // If the fetch completed synchronously, its attempt to drop a no-store record ran
                    // before the record was added, so drop it now.
                    if (record.Result.IsCompleted && record.MustNotRetain)
                    {
                        this.memoryCache.Remove(cacheKey);
                    }

                    return record.Result;
                }
            }
        }

        private static bool IsNotFound(Task<T> result) =>
            result.IsFaulted && result.Exception?.InnerException is GitHubContentNotFoundException;

        /// <summary>
        /// Fetches the content, traced as an <c>imm.content.fetch</c> span.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <see cref="GetOrFetch"/> calls this while holding the lock, and the span starts before
        /// the first <c>await</c>, so it is in place, with its context stored on the record, before
        /// any other caller can find the record and join this fetch. The span starts as a child of
        /// the <c>imm.content.get</c> span of the caller that started the fetch.
        /// </para>
        /// </remarks>
        private async Task<T> FetchAsync(string url, string cacheKey, Record record, EntityTagHeaderValue? eTag = null)
        {
            using Activity? activity = ImmTelemetry.Source.StartActivity("imm.content.fetch", ActivityKind.Internal);
            record.FetchActivityContext = activity?.Context ?? default;
            activity?.SetTag("url.full", url);

            try
            {
                return await this.FetchAndDeserializeAsync(url, cacheKey, record, eTag, activity).ConfigureAwait(false);
            }
            catch (GitHubContentUnavailableException) when (record.LastResult is not null)
            {
                // Stale-if-error: GitHub could not be reached, but we still hold what it last sent,
                // which is better than no badge. The record's freshness is left as it was (stale), so
                // the next request tries GitHub again.
                activity?.SetTag("imm.cache.stale_if_error", true);
                return record.LastResult;
            }
            catch (Exception ex)
            {
                // Including GitHubContentNotFoundException: whatever the client asked for is missing,
                // which is an error from the point of view of the client that asked GitHub for it.
                activity.RecordFailure(ex);
                throw;
            }
        }

        private async Task<T> FetchAndDeserializeAsync(
            string url, string cacheKey, Record record, EntityTagHeaderValue? eTag, Activity? activity)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            Uri requestUri = request.RequestUri!;
            bool conditional = false;
            if (eTag != null && record.LastResult != null)
            {
                request.Headers.IfNoneMatch.Add(eTag);
                conditional = true;
            }

            activity?.SetTag("imm.cache.conditional", conditional);

            using HttpResponseMessage response = await this.SendAsync(request).ConfigureAwait(false);
            bool contentNotChanged = response.StatusCode == HttpStatusCode.NotModified;
            if (activity is not null)
            {
                activity.SetTag("http.response.status_code", (int)response.StatusCode);
                activity.SetTag("imm.cache.not_modified", contentNotChanged);
            }

            // 304 Not Modified is not a success status, but it is what we hope for when revalidating.
            if (!contentNotChanged && !response.IsSuccessStatusCode)
            {
                if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
                {
                    // Remember that the content does not exist for a short while, unless the origin
                    // forbids storing the response. (GetAsync only reuses a faulted task when it
                    // holds this exception and the record is fresh.)
                    if (response.Headers.CacheControl?.NoStore != true)
                    {
                        lock (sync)
                        {
                            record.ExpirationTimestamp = this.ExpirationFor(DefaultFreshnessLifetime);
                        }
                    }

                    throw new GitHubContentNotFoundException(requestUri, response.StatusCode);
                }

                throw new GitHubContentUnavailableException(requestUri, response.StatusCode);
            }

            // A 304 without a Cache-Control header leaves the caching policy we already hold in place;
            // any other response, including a 304 that supplies one, replaces it.
            CacheControlHeaderValue? cacheControl = contentNotChanged && response.Headers.CacheControl is null
                ? record.CacheControl
                : response.Headers.CacheControl;
            bool mayStore = cacheControl?.NoStore != true;

            // Deserialize before the record takes this response's ETag and freshness. If the content
            // is invalid, the record must keep describing the content it last held successfully:
            // otherwise the next request would revalidate with the invalid content's ETag, get a 304,
            // and serve the previous content as if it were current, hiding the broken update.
            T result = contentNotChanged && record.LastResult != null
                ? record.LastResult
                : await this.DeserializeAsync(requestUri, response).ConfigureAwait(false);

            // We want to update our expiration time whether we ended up fetching new content, or
            // we're just reusing the old content.
            lock (sync)
            {
                record.CacheControl = cacheControl;
                record.ExpirationTimestamp = this.ExpirationFor(GetFreshnessLifetime(cacheControl));

                // A 304 need not repeat the ETag, in which case the one we revalidated with still
                // identifies the content we hold.
                if (!contentNotChanged || response.Headers.ETag != null)
                {
                    record.ETag = response.Headers.ETag;
                }

                if (!mayStore)
                {
                    record.ETag = null;
                }
            }

            // Without a last result, the next fetch will be unconditional.
            record.LastResult = mayStore ? result : null;

            if (!mayStore)
            {
                // The record's task would still hold the content once it completes, so no-store also
                // means removing the record from the memory cache. Callers already holding the task still
                // get the result. Only this record is removed, never one that has since replaced it.
                lock (sync)
                {
                    record.MustNotRetain = true;
                    if (this.memoryCache.TryGetValue(cacheKey, out object? entry) && ReferenceEquals(entry, record))
                    {
                        this.memoryCache.Remove(cacheKey);
                    }
                }
            }

            return result;
        }

        private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            try
            {
                return await this.httpClient.SendAsync(request).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                throw new GitHubContentUnavailableException(request.RequestUri!, null, ex);
            }
            catch (TaskCanceledException ex)
            {
                // We never pass a cancellation token, so this can only be the HttpClient's timeout.
                throw new GitHubContentUnavailableException(request.RequestUri!, null, ex);
            }
        }

        /// <summary>
        /// Reads and deserialises the response's content, traced as an
        /// <c>imm.content.deserialize</c> span. (The <see cref="IHttpDeserializer{T}"/> adds the
        /// length of the content it read to the span, since only it sees the text.)
        /// </summary>
        private async Task<T> DeserializeAsync(Uri requestUri, HttpResponseMessage response)
        {
            using Activity? activity = ImmTelemetry.Source.StartActivity("imm.content.deserialize", ActivityKind.Internal);
            try
            {
                return await this.deserializer.DeserializeAsync(response).ConfigureAwait(false);
            }
            catch (YamlException ex)
            {
                throw Failed(activity, new InvalidImmContentException(requestUri, ex));
            }
            catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
            {
                // YamlDotNet reports most failures as a YamlException, but a value it converts
                // after the document has been read (an alias to a node that was still being
                // deserialized, such as the whole document aliased into a rule's measures) fails
                // with the bare conversion exception. That is still content we cannot read.
                throw Failed(activity, new InvalidImmContentException(requestUri, ex));
            }
            catch (HttpRequestException ex)
            {
                // The connection failed while the body was being read.
                throw Failed(activity, new GitHubContentUnavailableException(requestUri, null, ex));
            }
            catch (Exception ex)
            {
                activity.RecordFailure(ex);
                throw;
            }
        }

        /// <summary>
        /// Records on the span the exception a failure is reported as, and returns it to be thrown.
        /// </summary>
        private static Exception Failed(Activity? activity, Exception exception)
        {
            activity.RecordFailure(exception);
            return exception;
        }

        private bool IsFresh(Record record) => record.ExpirationTimestamp > this.timeProvider.GetTimestamp();

        private long ExpirationFor(TimeSpan freshnessLifetime) =>
            this.timeProvider.GetTimestamp() + (long)(freshnessLifetime.TotalSeconds * this.timeProvider.TimestampFrequency);

        private static TimeSpan GetFreshnessLifetime(CacheControlHeaderValue? cacheControl) =>
            cacheControl switch
            {
                null => DefaultFreshnessLifetime,
                { NoCache: true } or { NoStore: true } => TimeSpan.Zero,
                { MaxAge: TimeSpan maxAge } => maxAge,
                _ => DefaultFreshnessLifetime,
            };

        private class Record
        {
            private Task<T>? result;

            public EntityTagHeaderValue? ETag { get; set; }

            public CacheControlHeaderValue? CacheControl { get; set; }

            public bool MustNotRetain { get; set; }

            /// <summary>
            /// Gets or sets the context of the span tracing the most recent fetch (the default if
            /// it was not traced), so that callers who join that fetch while it is in flight can
            /// link to it.
            /// </summary>
            public ActivityContext FetchActivityContext { get; set; }

            /// <summary>
            /// Gets or sets the <see cref="TimeProvider.GetTimestamp"/> value after which the
            /// content (or the knowledge that there is none) is stale.
            /// </summary>
            public long ExpirationTimestamp { get; set; }

            public Task<T> Result
            {
                get => this.result ?? throw new InvalidOperationException("Property not set yet");
                set => this.result = value;
            }

            public T? LastResult { get; set; }
        }
    }
}
