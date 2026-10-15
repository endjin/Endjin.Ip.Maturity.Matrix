using BenchmarkDotNet.Attributes;
using Endjin.Imm.Domain;

namespace Endjin.Imm.Benchmarks;

/// <summary>
/// Loading rule sets and IMMs from YAML through the public factory methods, as the Host does on a
/// cache miss. Both factories build a new YamlDotNet deserializer on every call.
/// </summary>
[Config(typeof(ImmBenchmarkConfig))]
[BenchmarkCategory("Yaml")]
public class YamlDeserialisationBenchmarks
{
    private string rulesetYaml = string.Empty;
    private string immYaml = string.Empty;

    [Params(1, 18, 180)]
    public int RuleCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        string snapshot = SampleData.LoadEmbedded(SampleData.RuleSetResourceName);
        this.rulesetYaml = SampleData.ScaleRuleSet(snapshot, this.RuleCount);

        IpMaturityMatrixRuleset ruleset = IpMaturityMatrixRuleset.FromYaml(this.rulesetYaml);
        if (ruleset.Rules.Count != this.RuleCount)
        {
            throw new InvalidOperationException($"Scaled rule set has {ruleset.Rules.Count} rules, expected {this.RuleCount}.");
        }

        this.immYaml = SampleData.BuildImmYaml(ruleset, SampleData.ReferenceDate);
        IpMaturityMatrix imm = IpMaturityMatrix.FromYaml(this.immYaml);
        if (imm.Rules.Count != this.RuleCount)
        {
            throw new InvalidOperationException($"Generated IMM has {imm.Rules.Count} rules, expected {this.RuleCount}.");
        }
    }

    [Benchmark(Baseline = true)]
    public IpMaturityMatrixRuleset Ruleset_FromYaml() => IpMaturityMatrixRuleset.FromYaml(this.rulesetYaml);

    [Benchmark]
    public IpMaturityMatrix Imm_FromYaml() => IpMaturityMatrix.FromYaml(this.immYaml);
}
