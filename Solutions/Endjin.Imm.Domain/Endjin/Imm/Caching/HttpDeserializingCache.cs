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
    /// changed since last time (and it with this application, it usually won't have), 
    /// </para>
    /// </remarks>
    internal class HttpDeserializingCache<T>
        where T : class
    {
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
                if (this.memoryCache.TryGetValue(cacheKey, out object entry))
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
                        record.Result = FetchAsync(url, record, record.ETag);
                        return record.Result;
                    }
                }
                else
                {
                    var record = new Record();
                    record.Result = FetchAsync(url, record);
                    using (ICacheEntry ce = this.memoryCache.CreateEntry(cacheKey))
                    {
                        ce.Value = record;
                        ce.SlidingExpiration = TimeSpan.FromMinutes(20);
                    }
                    return record.Result;
                }
            }
        }

        private async Task<T> FetchAsync(string url, Record record, EntityTagHeaderValue? eTag = null)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (eTag != null && record.LastResult != null)
            {
                request.Headers.IfNoneMatch.Add(eTag);
            }
            HttpResponseMessage response = await this.httpClient.SendAsync(request).ConfigureAwait(false);
            bool contentNotChanged = response.StatusCode == HttpStatusCode.NotModified;

            // Apparently EnsureSuccessStatusCode considers 304 Not Modified not to be success, so
            // we need to make sure skip the success test in that case.
            if (!contentNotChanged)
            {
                response.EnsureSuccessStatusCode();
            }

            // We want to update our expiration time whether we ended up fetching new content, or
            // we're just reusing the old content.
            lock (sync)
            {
                record.ExpirationTicks = Environment.TickCount64 +
                    (int)(response.Headers.CacheControl?.MaxAge?.TotalMilliseconds ?? 60_000.0);
                record.ETag = response.Headers.ETag;
            }

            if (contentNotChanged && record.LastResult != null)
            {
                return record.LastResult;
            }
            T result = await this.deserializer.DeserializeAsync(response).ConfigureAwait(false);
            record.LastResult = result;
            return result;
        }

        private class Record
        {
            private Task<T>? result;

            public EntityTagHeaderValue? ETag { get; set; }

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
