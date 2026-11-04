using BenchmarkDotNet.Attributes;
using Endjin.Imm.Domain;

namespace Endjin.Imm.Benchmarks;

/// <summary>
/// Loading rule sets and IMMs from YAML through the public factory methods, as the Host does on a
/// cache miss. Both factories use the same shared YamlDotNet deserializer.
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

        IpMaturityModelRuleset ruleset = IpMaturityModelRuleset.FromYaml(this.rulesetYaml);
        if (ruleset.Rules.Count != this.RuleCount)
        {
            throw new InvalidOperationException($"Scaled rule set has {ruleset.Rules.Count} rules, expected {this.RuleCount}.");
        }

        this.immYaml = SampleData.BuildImmYaml(ruleset, SampleData.ReferenceDate);
        IpMaturityModel imm = IpMaturityModel.FromYaml(this.immYaml);
        if (imm.Rules.Count != this.RuleCount)
        {
            throw new InvalidOperationException($"Generated IMM has {imm.Rules.Count} rules, expected {this.RuleCount}.");
        }
    }

    [Benchmark(Baseline = true)]
    public IpMaturityModelRuleset Ruleset_FromYaml() => IpMaturityModelRuleset.FromYaml(this.rulesetYaml);

    [Benchmark]
    public IpMaturityModel Imm_FromYaml() => IpMaturityModel.FromYaml(this.immYaml);
}
