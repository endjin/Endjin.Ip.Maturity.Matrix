namespace Endjin.Imm.Caching
{
    using System;
    using YamlDotNet.Core;

    /// <summary>
    /// Thrown when content fetched from GitHub (a project's imm.yaml, or a rule set) is not valid
    /// YAML, or does not have the shape of the document it should be.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="Exception.InnerException"/> is the deserialization failure. This is never
    /// cached, so a fix is picked up as soon as GitHub serves it.
    /// </para>
    /// </remarks>
    public class InvalidImmContentException : Exception
    {
        /// <summary>
        /// Creates an <see cref="InvalidImmContentException"/>.
        /// </summary>
        /// <param name="url">The URL of the invalid content.</param>
        /// <param name="innerException">
        /// The deserialization failure. When this is a YamlDotNet <see cref="YamlException"/>, its
        /// position becomes <see cref="Line"/> and <see cref="Column"/>.
        /// </param>
        public InvalidImmContentException(Uri url, Exception innerException)
            : base(BuildMessage(url, innerException), innerException)
        {
            this.Url = url;
            if (innerException is YamlException yamlException)
            {
                this.Line = yamlException.Start.Line;
                this.Column = yamlException.Start.Column;
            }
        }

        /// <summary>
        /// Gets the URL of the invalid content.
        /// </summary>
        public Uri Url { get; }

        /// <summary>
        /// Gets the (1-based) line at which the problem was found, if known.
        /// </summary>
        public long? Line { get; }

        /// <summary>
        /// Gets the (1-based) column at which the problem was found, if known.
        /// </summary>
        public long? Column { get; }

        /// <summary>
        /// Gets a description of the problem, including its position when that is known.
        /// </summary>
        public string Problem => DescribeProblem(this.InnerException!);

        private static string BuildMessage(Uri url, Exception innerException) =>
            $"The content at {url} is not valid: {DescribeProblem(innerException)}";

        private static string DescribeProblem(Exception innerException) =>
            innerException is YamlException yamlException
                ? $"{yamlException.Message} (line {yamlException.Start.Line}, column {yamlException.Start.Column})"
                : innerException.Message;
    }
}
