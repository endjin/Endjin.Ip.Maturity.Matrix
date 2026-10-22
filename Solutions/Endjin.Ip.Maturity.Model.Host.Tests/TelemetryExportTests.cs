using Microsoft.Extensions.Configuration;
using Shouldly;

namespace Endjin.Ip.Maturity.Model.Host.Tests;

/// <summary>
/// The Host exports traces and logs only when it is told where to send them: to Application
/// Insights when APPLICATIONINSIGHTS_CONNECTION_STRING is set, over OTLP (for example to the Aspire
/// dashboard) when OTEL_EXPORTER_OTLP_ENDPOINT is set, or both. Setting IMM_TRACE_CONNECTIONS to 1
/// or true (in any case) also traces DNS lookups, connections and TLS handshakes.
/// </summary>
[TestClass]
public class TelemetryExportTests
{
    private const string ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://example.invalid/";

    private const string OtlpEndpoint = "http://localhost:4317";

    [TestMethod]
    public void FromConfiguration_NothingConfigured_ExportsNothing()
    {
        // Arrange
        IConfiguration configuration = Configuration();

        // Act
        TelemetryExport export = TelemetryExport.FromConfiguration(configuration);

        // Assert
        export.IsEnabled.ShouldBeFalse();
        export.AzureMonitor.ShouldBeFalse();
        export.Otlp.ShouldBeFalse();
        export.TraceConnections.ShouldBeFalse();
    }

    [TestMethod]
    public void FromConfiguration_ApplicationInsightsConnectionString_ExportsToAzureMonitorOnly()
    {
        // Arrange
        IConfiguration configuration = Configuration(("APPLICATIONINSIGHTS_CONNECTION_STRING", ConnectionString));

        // Act
        TelemetryExport export = TelemetryExport.FromConfiguration(configuration);

        // Assert
        export.IsEnabled.ShouldBeTrue();
        export.AzureMonitor.ShouldBeTrue();
        export.Otlp.ShouldBeFalse();
    }

    [TestMethod]
    public void FromConfiguration_OtlpEndpoint_ExportsOverOtlpOnly()
    {
        // Arrange
        IConfiguration configuration = Configuration(("OTEL_EXPORTER_OTLP_ENDPOINT", OtlpEndpoint));

        // Act
        TelemetryExport export = TelemetryExport.FromConfiguration(configuration);

        // Assert
        export.IsEnabled.ShouldBeTrue();
        export.AzureMonitor.ShouldBeFalse();
        export.Otlp.ShouldBeTrue();
    }

    [TestMethod]
    public void FromConfiguration_BothConfigured_ExportsToBoth()
    {
        // Arrange
        IConfiguration configuration = Configuration(
            ("APPLICATIONINSIGHTS_CONNECTION_STRING", ConnectionString),
            ("OTEL_EXPORTER_OTLP_ENDPOINT", OtlpEndpoint));

        // Act
        TelemetryExport export = TelemetryExport.FromConfiguration(configuration);

        // Assert
        export.IsEnabled.ShouldBeTrue();
        export.AzureMonitor.ShouldBeTrue();
        export.Otlp.ShouldBeTrue();
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(" ")]
    public void FromConfiguration_BlankSettings_ExportsNothing(string value)
    {
        // Arrange
        IConfiguration configuration = Configuration(
            ("APPLICATIONINSIGHTS_CONNECTION_STRING", value),
            ("OTEL_EXPORTER_OTLP_ENDPOINT", value));

        // Act
        TelemetryExport export = TelemetryExport.FromConfiguration(configuration);

        // Assert
        export.IsEnabled.ShouldBeFalse();
    }

    [TestMethod]
    [DataRow("1", true)]
    [DataRow("true", true)]
    [DataRow("TRUE", true)]
    [DataRow("True", true)]
    [DataRow("0", false)]
    [DataRow("false", false)]
    [DataRow("yes", false)]
    [DataRow(null, false)]
    public void FromConfiguration_TraceConnectionsSetting_TracesConnectionsOnlyWhenOneOrTrue(string? value, bool expected)
    {
        // Arrange
        IConfiguration configuration = value is null
            ? Configuration(("OTEL_EXPORTER_OTLP_ENDPOINT", OtlpEndpoint))
            : Configuration(("OTEL_EXPORTER_OTLP_ENDPOINT", OtlpEndpoint), ("IMM_TRACE_CONNECTIONS", value));

        // Act
        TelemetryExport export = TelemetryExport.FromConfiguration(configuration);

        // Assert
        export.TraceConnections.ShouldBe(expected);
    }

    private static IConfiguration Configuration(params (string Key, string Value)[] settings) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(settings.Select(s => new KeyValuePair<string, string?>(s.Key, s.Value)))
            .Build();
}
