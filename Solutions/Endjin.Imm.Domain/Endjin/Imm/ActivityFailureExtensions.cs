namespace Endjin.Imm
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Records failures on the domain's spans in the form OpenTelemetry's semantic conventions
    /// describe.
    /// </summary>
    internal static class ActivityFailureExtensions
    {
        /// <summary>
        /// Marks a span as failed: its status becomes <see cref="ActivityStatusCode.Error"/>, with
        /// the exception's message as the description, and its <c>error.type</c> tag names the
        /// exception's type.
        /// </summary>
        /// <param name="activity">The span, or null when nobody is listening.</param>
        /// <param name="exception">The exception that ended the operation.</param>
        /// <remarks>
        /// <para>
        /// The exception itself (and its stack trace) is not recorded as an event: the host logs
        /// it, and the log record carries the trace and span ids, so the two can be correlated
        /// without sending the stack trace twice.
        /// </para>
        /// </remarks>
        public static void RecordFailure(this Activity? activity, Exception exception)
        {
            if (activity is not null)
            {
                activity.SetStatus(ActivityStatusCode.Error, exception.Message);
                activity.SetTag("error.type", exception.GetType().FullName);
            }
        }
    }
}
