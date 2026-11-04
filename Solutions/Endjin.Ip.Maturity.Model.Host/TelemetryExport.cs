namespace Endjin.Ip.Maturity.Model.Host
{
    #region Using Directives

    using Microsoft.Extensions.Configuration;

    using System;

    #endregion

    /// <summary>
    /// Where, if anywhere, the Host exports its traces and logs, as decided by its configuration
    /// (app settings in Azure, <c>local.settings.json</c> or environment variables locally).
    /// </summary>
    /// <param name="AzureMonitor">
    /// Whether to export to Application Insights, which is the case when
    /// <c>APPLICATIONINSIGHTS_CONNECTION_STRING</c> is set.
    /// </param>
    /// <param name="Otlp">
    /// Whether to export over OTLP (for example to the Aspire dashboard), which is the case when
    /// <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is set.
    /// </param>
    /// <param name="TraceConnections">
    /// Whether to also trace DNS lookups, socket connections, TLS handshakes and waits for a pooled
    /// connection, which is the case when <c>IMM_TRACE_CONNECTIONS</c> is <c>1</c> or <c>true</c>.
    /// These spans are noisy, so they are for diagnosing cold starts and connection recycling.
    /// </param>
    public sealed record TelemetryExport(bool AzureMonitor, bool Otlp, bool TraceConnections)
    {
        /// <summary>
        /// The setting that holds the Application Insights connection string.
        /// </summary>
        public const string AzureMonitorConnectionStringSetting = "APPLICATIONINSIGHTS_CONNECTION_STRING";

        /// <summary>
        /// The setting that holds the OTLP endpoint.
        /// </summary>
        public const string OtlpEndpointSetting = "OTEL_EXPORTER_OTLP_ENDPOINT";

        /// <summary>
        /// The setting that turns on tracing of connection setup.
        /// </summary>
        public const string TraceConnectionsSetting = "IMM_TRACE_CONNECTIONS";

        /// <summary>
        /// Gets a value indicating whether telemetry is exported anywhere.
        /// </summary>
        public bool IsEnabled => this.AzureMonitor || this.Otlp;

        /// <summary>
        /// Works out where to export telemetry from the Host's configuration.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <returns>Where to export telemetry.</returns>
        public static TelemetryExport FromConfiguration(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            string? traceConnections = configuration[TraceConnectionsSetting];
            return new TelemetryExport(
                AzureMonitor: !string.IsNullOrWhiteSpace(configuration[AzureMonitorConnectionStringSetting]),
                Otlp: !string.IsNullOrWhiteSpace(configuration[OtlpEndpointSetting]),
                TraceConnections: traceConnections == "1" || string.Equals(traceConnections, "true", StringComparison.OrdinalIgnoreCase));
        }
    }
}
