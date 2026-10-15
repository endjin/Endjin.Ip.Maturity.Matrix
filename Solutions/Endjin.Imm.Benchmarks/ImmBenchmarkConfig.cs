using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Validators;

namespace Endjin.Imm.Benchmarks;

/// <summary>
/// Shared configuration for every benchmark in this project.
/// </summary>
/// <remarks>
/// <para>
/// No job is added, so <c>Job.Default</c> applies unless one is chosen on the command line
/// (e.g. <c>--job dry</c> for a smoke test or <c>--job short</c> for a quick look).
/// </para>
/// <para>
/// The default artifacts path (<c>./BenchmarkDotNet.Artifacts</c>, relative to the working directory,
/// which the project file sets to the project directory) is kept deliberately: it is gitignored, and
/// results worth keeping are copied into <c>Baselines/</c> by hand (also gitignored, so baselines stay local).
/// </para>
/// </remarks>
internal sealed class ImmBenchmarkConfig : ManualConfig
{
    public ImmBenchmarkConfig()
    {
        this.AddDiagnoser(MemoryDiagnoser.Default);

        this.AddExporter(MarkdownExporter.GitHub, JsonExporter.FullCompressed, CsvExporter.Default);
        this.AddLogger(ConsoleLogger.Default);

        this.AddColumnProvider(DefaultColumnProviders.Instance);
        this.AddColumn(StatisticColumn.Median, StatisticColumn.P95);

        this.AddValidator(JitOptimizationsValidator.FailOnError);

        this.WithOrderer(new DefaultOrderer(SummaryOrderPolicy.Declared, MethodOrderPolicy.Declared));
        this.WithSummaryStyle(SummaryStyle.Default.WithRatioStyle(RatioStyle.Trend));

        // The generated benchmark project references the Functions Host (Azure.Functions.Sdk), whose build
        // regularly takes longer than BenchmarkDotNet's 120 s default.
        this.WithBuildTimeout(TimeSpan.FromMinutes(10));
    }
}
