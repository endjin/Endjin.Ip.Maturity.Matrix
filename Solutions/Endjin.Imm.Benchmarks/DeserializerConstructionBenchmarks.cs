using BenchmarkDotNet.Attributes;
using Endjin.Imm.Domain;
using Endjin.Imm.Serialisation;
using YamlDotNet.Serialization;

namespace Endjin.Imm.Benchmarks;

/// <summary>
/// Separates the cost of building the YamlDotNet deserializer from the cost of actually parsing
/// the 18-rule snapshot. The <c>FromYaml</c> factories share one deserializer
/// (<c>ImmYamlDeserializer.Shared</c>), so they pay only the parse cost; building one is measured
/// here to show what that sharing saves.
/// </summary>
[Config(typeof(ImmBenchmarkConfig))]
[BenchmarkCategory("Yaml")]
public class DeserializerConstructionBenchmarks
{
    private string snapshot = string.Empty;
    private IDeserializer prebuilt = null!;

    [GlobalSetup]
    public void Setup()
    {
        this.snapshot = SampleData.LoadEmbedded(SampleData.RuleSetResourceName);
        this.prebuilt = ImmYamlDeserializer.Create();
    }

    /// <summary>What the Host does: parse with the shared deserializer.</summary>
    [Benchmark(Baseline = true)]
    public IpMaturityMatrixRuleset Ruleset_FromYaml() => IpMaturityMatrixRuleset.FromYaml(this.snapshot);

    /// <summary>The construction cost alone, which the shared deserializer avoids on every call.</summary>
    [Benchmark]
    public IDeserializer Create() => ImmYamlDeserializer.Create();

    /// <summary>A trivial parse: approximately the fixed per-call overhead.</summary>
    [Benchmark]
    public IpMaturityMatrixRuleset FromYaml_EmptyDocument() => IpMaturityMatrixRuleset.FromYaml("[]");

    /// <summary>
    /// The parse cost alone, reusing one deserializer created for this benchmark.
    /// </summary>
    /// <remarks>
    /// This should match <see cref="Ruleset_FromYaml"/>, which reuses the shared instance. That
    /// sharing it across threads is safe is covered by <c>YamlDeserializerSharingTests</c>; this
    /// benchmark is single-threaded.
    /// </remarks>
    [Benchmark]
    public RuleDefinition[] Deserialize_WithPrebuiltDeserializer()
    {
        using StringReader reader = new(this.snapshot);
        return this.prebuilt.Deserialize<RuleDefinition[]>(reader);
    }
}
