[assembly: Microsoft.Azure.Functions.Extensions.DependencyInjection.FunctionsStartup(typeof(Endjin.Ip.Maturity.Matrix.Host.Startup))]

namespace Endjin.Ip.Maturity.Matrix.Host
{
    using Microsoft.Azure.Functions.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection;
    using System.Globalization;
    using System.Threading;

    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // It appears there isn't an official way to set the UI culture for an Azure Function.
            // We want this to be consistent with the default culture for assemblies that contain
            // resources to minimize startup time. (When the default assembly culture matches the
            // thread UI culture, the resource manager will not probe for satellite resource
            // assemblies, improving the first-load time for resources.)
            var cultureInfo = new CultureInfo("en-GB");
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;


            builder.Services.AddHttpClient();
            builder.Services.AddMemoryCache();
            builder.Services.AddImmSources();
        }
    }
}