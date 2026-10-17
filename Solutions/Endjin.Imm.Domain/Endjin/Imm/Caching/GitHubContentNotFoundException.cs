namespace Endjin.Imm.Caching
{
    using System;
    using System.Net;

    /// <summary>
    /// Thrown when GitHub reports that the content at a URL does not exist.
    /// </summary>
    /// <remarks>
    /// <para>
    /// raw.githubusercontent.com gives the same 404 response when the repository, the branch (or
    /// other object name) or the file is missing, so this cannot say which of those is wrong.
    /// </para>
    /// </remarks>
    public class GitHubContentNotFoundException : Exception
    {
        /// <summary>
        /// Creates a <see cref="GitHubContentNotFoundException"/>.
        /// </summary>
        /// <param name="url">The URL that was not found.</param>
        /// <param name="statusCode">The status code GitHub returned.</param>
        public GitHubContentNotFoundException(Uri url, HttpStatusCode statusCode)
            : base($"GitHub returned {(int)statusCode} ({statusCode}) for {url}")
        {
            this.Url = url;
            this.StatusCode = statusCode;
        }

        /// <summary>
        /// Gets the URL that was not found.
        /// </summary>
        public Uri Url { get; }

        /// <summary>
        /// Gets the status code GitHub returned: 404 Not Found or 410 Gone.
        /// </summary>
        public HttpStatusCode StatusCode { get; }
    }
}
