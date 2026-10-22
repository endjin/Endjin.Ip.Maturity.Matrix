using System.Globalization;
using Azure.Monitor.OpenTelemetry.Exporter;
using Endjin.Imm;
using Endjin.Ip.Maturity.Model.Host;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using OpenTelemetry;
using OpenTelemetry.Logs;

// It appears there isn't an official way to set the UI culture for an Azure Function.
// We want this to be consistent with the default culture for assemblies that contain
// resources to minimize startup time. (When the default assembly culture matches the
// thread UI culture, the resource manager will not probe for satellite resource
// assemblies, improving the first-load time for resources.)
CultureInfo cultureInfo = new("en-GB");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
CultureInfo.CurrentCulture = cultureInfo;
CultureInfo.CurrentUICulture = cultureInfo;

FunctionsApplicationBuilder builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// The GitHub sources cache content in a singleton, which holds on to the HttpClient it is given for
// the life of the process, so a client from IHttpClientFactory would never have its handler rotated.
// Instead, use one long-lived client whose pooled connections are replaced periodically, so that
// DNS changes for raw.githubusercontent.com are picked up.
builder.Services.AddSingleton(new HttpClient(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) }));
builder.Services.AddMemoryCache();
builder.Services.AddImmSources();

// Traces and logs are exported only when somewhere to send them is configured: Application Insights
// (APPLICATIONINSIGHTS_CONNECTION_STRING) and/or an OTLP endpoint such as the Aspire dashboard
// (OTEL_EXPORTER_OTLP_ENDPOINT). Without either, nothing listens to the ActivitySources, so the spans
// are never created and cost nothing.
TelemetryExport telemetry = TelemetryExport.FromConfiguration(builder.Configuration);
if (telemetry.IsEnabled)
{
    // UseFunctionsWorkerDefaults tells the Functions host that the worker emits its own telemetry
    // (so the host stops doing it on the worker's behalf), describes the function app as the
    // resource, and adds a span for each invocation. It adds no exporters. HttpClient emits its own
    // spans from the System.Net.Http source, so no HTTP instrumentation package is needed.
    IOpenTelemetryBuilder otel = builder.Services.AddOpenTelemetry()
        .UseFunctionsWorkerDefaults()
        .WithTracing(tracing =>
        {
            tracing.AddSource(ImmTelemetry.ActivitySourceName);
            tracing.AddSource(HostTelemetry.ActivitySourceName);
            tracing.AddSource("System.Net.Http");
            if (telemetry.TraceConnections)
            {
                // DNS lookups, socket connections, TLS handshakes and waits for a pooled connection.
                // Too noisy to have on all the time, but they show where a cold start's first fetch
                // from GitHub, or the first after the connections are recycled, spends its time.
                tracing.AddSource("Experimental.System.Net.*");
            }
        })

        // Sends ILogger output to the exporters as OpenTelemetry log records, which carry the trace
        // and span ids of the span that was current when they were written.
        .WithLogging();
    builder.Services.Configure<OpenTelemetryLoggerOptions>(options =>
    {
        options.IncludeScopes = true;
        options.IncludeFormattedMessage = true;
        options.ParseStateValues = true;
    });

    // Each exporter is added only when it is configured: UseAzureMonitorExporter fails at startup
    // when there is no connection string.
    if (telemetry.AzureMonitor)
    {
        otel.UseAzureMonitorExporter();
    }

    if (telemetry.Otlp)
    {
        otel.UseOtlpExporter();
    }
}

// Pays for JIT compilation, the YAML deserializer and the first rule set fetch in the background
// as soon as the worker starts, instead of in the first request after a cold start. It never
// delays startup. Setting IMM_DISABLE_WARMUP to 1 or true turns it off, for measuring its effect.
string? disableWarmup = builder.Configuration["IMM_DISABLE_WARMUP"];
if (!(disableWarmup == "1" || string.Equals(disableWarmup, "true", StringComparison.OrdinalIgnoreCase)))
{
    builder.Services.AddHostedService<StartupWarmup>();

    // The worker relays logs to the Functions host only when they are written during a function
    // invocation, so the warm-up's log line would never be seen. The host does capture the worker's
    // console output, so send the warm-up's log (and nothing else) to the console as a single line.
    // When telemetry is exported, the worker's logs (the warm-up's included, even though it runs
    // outside any invocation) go straight to the exporter, and relaying the line through the
    // console as well would record it twice.
    if (!telemetry.IsEnabled)
    {
        builder.Logging.AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.ColorBehavior = LoggerColorBehavior.Disabled;
        });
        builder.Logging.AddFilter<ConsoleLoggerProvider>((category, _) => category == typeof(StartupWarmup).FullName);
    }
}

builder.Build().Run();
