using System.Globalization;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

builder.Build().Run();
