namespace Endjin.Imm.Caching
{
    using Microsoft.Extensions.Caching.Memory;
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;

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
    /// </remarks>
    internal class HttpDeserializingCache<T>
        where T : class
    {
        private static readonly TimeSpan DefaultFreshnessLifetime = TimeSpan.FromMinutes(1);

        private readonly object sync = new();
        private readonly IMemoryCache memoryCache;
        private readonly HttpClient httpClient;
        private readonly IHttpDeserializer<T> deserializer;

        public HttpDeserializingCache(
            HttpClient httpClient,
            IMemoryCache memoryCache,
            IHttpDeserializer<T> deserializer)
        {
            this.memoryCache = memoryCache;
            this.httpClient = httpClient;
            this.deserializer = deserializer;
        }

        public Task<T> GetAsync(string url)
        {
            string cacheKey = $"HttpDeserializingCache:{typeof(T).FullName}:{url}";

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
                    // completed successfully, and recently enough that it's sufficiently up to
                    // date according to the cache header Max Age returned by the source.
                    if (!record.Result.IsCompleted ||
                        (record.Result.IsCompletedSuccessfully && record.ExpirationTicks > Environment.TickCount64))
                    {
                        return record.Result;
                    }
                    else
                    {
                        record.Result = FetchAsync(url, cacheKey, record, record.ETag);
                        return record.Result;
                    }
                }
                else
                {
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

        private async Task<T> FetchAsync(string url, string cacheKey, Record record, EntityTagHeaderValue? eTag = null)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (eTag != null && record.LastResult != null)
            {
                request.Headers.IfNoneMatch.Add(eTag);
            }
            using HttpResponseMessage response = await this.httpClient.SendAsync(request).ConfigureAwait(false);
            bool contentNotChanged = response.StatusCode == HttpStatusCode.NotModified;

            // Apparently EnsureSuccessStatusCode considers 304 Not Modified not to be success, so
            // we need to make sure skip the success test in that case.
            if (!contentNotChanged)
            {
                response.EnsureSuccessStatusCode();
            }

            // A 304 without a Cache-Control header leaves the caching policy we already hold in place;
            // any other response, including a 304 that supplies one, replaces it.
            CacheControlHeaderValue? cacheControl = contentNotChanged && response.Headers.CacheControl is null
                ? record.CacheControl
                : response.Headers.CacheControl;
            bool mayStore = cacheControl?.NoStore != true;

            // We want to update our expiration time whether we ended up fetching new content, or
            // we're just reusing the old content.
            lock (sync)
            {
                record.CacheControl = cacheControl;
                record.ExpirationTicks = Environment.TickCount64 +
                    (long)GetFreshnessLifetime(cacheControl).TotalMilliseconds;

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

            T result = contentNotChanged && record.LastResult != null
                ? record.LastResult
                : await this.deserializer.DeserializeAsync(response).ConfigureAwait(false);

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

            public long ExpirationTicks { get; set; }

            public Task<T> Result
            {
                get => this.result ?? throw new InvalidOperationException("Property not set yet");
                set => this.result = value;
            }

            public T? LastResult { get; set; }
        }
    }
}
