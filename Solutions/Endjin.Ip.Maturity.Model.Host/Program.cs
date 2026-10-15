using System.Globalization;
using Endjin.Ip.Maturity.Model.Host;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

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
    builder.Logging.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.ColorBehavior = LoggerColorBehavior.Disabled;
    });
    builder.Logging.AddFilter<ConsoleLoggerProvider>((category, _) => category == typeof(StartupWarmup).FullName);
}

builder.Build().Run();
