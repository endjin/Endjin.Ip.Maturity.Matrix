using System.Globalization;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace Endjin.Imm.Benchmarks;

public static class Program
{
    public static int Main(string[] args)
    {
        // Mirrors the Host, which pins en-GB so that formatting (and resource lookup) is
        // consistent with the assemblies' NeutralLanguage.
        CultureInfo culture = new("en-GB");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        // Every benchmark class carries [Config(typeof(ImmBenchmarkConfig))], so the global config
        // only needs a logger (for --list, --help and the switcher's own output); command-line
        // options such as --job are layered onto it. Passing a second ImmBenchmarkConfig here would
        // register every exporter twice, which BenchmarkDotNet reports as a configuration warning.
        IConfig global = ManualConfig.CreateEmpty().AddLogger(ConsoleLogger.Default);

        Summary[] summaries = [.. BenchmarkSwitcher
            .FromAssembly(typeof(Program).Assembly)
            .Run(args, global)];

        return ToExitCode(summaries);
    }

    // BenchmarkDotNet 0.15 has no public ToExitCode helper, so fail the process (for CI and smoke
    // runs) when validation failed or any benchmark did not execute successfully. Informational
    // commands such as --list and --help produce no summaries and succeed.
    private static int ToExitCode(Summary[] summaries)
    {
        bool failed = summaries.Any(s =>
            s.HasCriticalValidationErrors ||
            s.Reports.Any(r => !r.Success) ||
            s.Reports.Length != s.BenchmarksCases.Length);

        return failed ? 1 : 0;
    }
}
