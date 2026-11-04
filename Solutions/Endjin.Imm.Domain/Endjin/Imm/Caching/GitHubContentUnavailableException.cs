namespace Endjin.Imm.Caching
{
    using System;
    using System.Net;

    /// <summary>
    /// Thrown when content could not be fetched from GitHub for a reason other than its not
    /// existing: GitHub returned an error status (such as 5xx or 429), or the request failed or
    /// timed out.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These failures are usually transient, so they are never cached: the next request for the
    /// same URL tries GitHub again.
    /// </para>
    /// </remarks>
    public class GitHubContentUnavailableException : Exception
    {
        /// <summary>
        /// Creates a <see cref="GitHubContentUnavailableException"/>.
        /// </summary>
        /// <param name="url">The URL that could not be fetched.</param>
        /// <param name="statusCode">
        /// The status code GitHub returned, or null if no response was received.
        /// </param>
        /// <param name="innerException">The failure, if the request itself failed.</param>
        public GitHubContentUnavailableException(Uri url, HttpStatusCode? statusCode, Exception? innerException = null)
            : base(
                  statusCode is HttpStatusCode status
                    ? $"GitHub returned {(int)status} ({status}) for {url}"
                    : $"The request to {url} failed: {innerException?.Message}",
                  innerException)
        {
            this.Url = url;
            this.StatusCode = statusCode;
        }

        /// <summary>
        /// Gets the URL that could not be fetched.
        /// </summary>
        public Uri Url { get; }

        /// <summary>
        /// Gets the status code GitHub returned, or null if no response was received.
        /// </summary>
        public HttpStatusCode? StatusCode { get; }
    }
}
