using BenchmarkDotNet.Attributes;
using Endjin.Imm.Domain;
using Endjin.Imm.Serialisation;
using YamlDotNet.Serialization;

namespace Endjin.Imm.Benchmarks;

/// <summary>
/// Separates the cost of building the YamlDotNet deserializer (done on every <c>FromYaml</c> call)
/// from the cost of actually parsing the 18-rule snapshot.
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

    /// <summary>What the Host does today: build a deserializer, then parse.</summary>
    [Benchmark(Baseline = true)]
    public IpMaturityMatrixRuleset Ruleset_FromYaml() => IpMaturityMatrixRuleset.FromYaml(this.snapshot);

    /// <summary>The construction cost alone.</summary>
    [Benchmark]
    public IDeserializer Create() => ImmYamlDeserializer.Create();

    /// <summary>Construction plus a trivial parse: approximately the fixed per-call overhead.</summary>
    [Benchmark]
    public IpMaturityMatrixRuleset FromYaml_EmptyDocument() => IpMaturityMatrixRuleset.FromYaml("[]");

    /// <summary>
    /// The parse cost alone, reusing one deserializer.
    /// </summary>
    /// <remarks>
    /// Before adopting a shared deserializer in production, confirm that YamlDotNet's
    /// <see cref="IDeserializer"/> (as configured by <c>ImmYamlDeserializer</c>, including the custom
    /// node deserializers and the type-discriminating deserializer) is safe to use concurrently.
    /// This benchmark is single-threaded and says nothing about thread safety.
    /// </remarks>
    [Benchmark]
    public RuleDefinition[] Deserialize_WithPrebuiltDeserializer()
    {
        using StringReader reader = new(this.snapshot);
        return this.prebuilt.Deserialize<RuleDefinition[]>(reader);
    }
}
