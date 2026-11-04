namespace Endjin.Ip.Maturity.Model.Host
{
    #region Using Directives

    using System.Diagnostics;

    #endregion

    /// <summary>
    /// The OpenTelemetry instrumentation of the Host: the <see cref="ActivitySource"/> that the
    /// badge endpoints' and the warm-up's spans come from, so that the Host can subscribe to it
    /// and tests can listen to it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The badge endpoints start <c>imm.badge.total</c>, <c>imm.badge.rule</c> or
    /// <c>imm.badge.showbadges</c>, with an <c>imm.render</c> child around rendering the badge or
    /// page. The warm-up starts <c>imm.warmup</c>, a trace of its own, with a child for each of its
    /// steps. The domain's spans come from <see cref="Endjin.Imm.ImmTelemetry"/>.
    /// </para>
    /// <para>
    /// When nothing listens (no exporter is configured), the source creates no activities, so the
    /// instrumentation costs next to nothing.
    /// </para>
    /// </remarks>
    public static class HostTelemetry
    {
        /// <summary>
        /// The name of the <see cref="ActivitySource"/> the Host's spans come from.
        /// </summary>
        public const string ActivitySourceName = "Endjin.Ip.Maturity.Model.Host";

        internal static readonly ActivitySource Source = new(ActivitySourceName);
    }
}
