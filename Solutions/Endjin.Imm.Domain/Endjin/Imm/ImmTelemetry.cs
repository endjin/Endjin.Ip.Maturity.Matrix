namespace Endjin.Imm
{
    using System.Diagnostics;

    /// <summary>
    /// The OpenTelemetry instrumentation of the IMM domain: the <see cref="ActivitySource"/> its
    /// spans come from, and the names of the spans and tags, so that a host can subscribe to them
    /// and tests can look for them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Nothing here depends on the OpenTelemetry SDK: <see cref="System.Diagnostics.Activity"/>
    /// is part of the runtime, and an <see cref="ActivitySource"/> with no listener creates no
    /// activities and allocates nothing, so a host that does not export telemetry pays nothing.
    /// </para>
    /// </remarks>
    public static class ImmTelemetry
    {
        /// <summary>
        /// The name of the <see cref="ActivitySource"/> the domain's spans come from. A host that
        /// exports traces subscribes to it with <c>AddSource(ImmTelemetry.ActivitySourceName)</c>.
        /// </summary>
        public const string ActivitySourceName = "Endjin.Imm";

        internal static readonly ActivitySource Source = new(ActivitySourceName);
    }
}
