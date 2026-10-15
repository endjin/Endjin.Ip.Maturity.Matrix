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

builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
builder.Services.AddImmSources();

builder.Build().Run();
