using BenchmarkDotNet.Attributes;
using Endjin.Imm.Domain;
using Endjin.Imm.Serialisation;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Endjin.Imm.Benchmarks;

/// <summary>
/// Splits the cost of loading a rule set or an IMM from YAML into the cost of parsing the text into
/// events (YamlDotNet's <see cref="Parser"/>) and the cost of building the object graph from those
/// events (the deserializer). This shows how much a hand-written deserializer could save at most.
/// </summary>
/// <remarks>
/// The <c>DeserializeFromEvents_*</c> benchmarks replay events that were parsed once in
/// <see cref="Setup"/>, so they measure the deserializer alone. <c>Full_*</c> should be close to
/// <c>ParseOnly_*</c> plus <c>DeserializeFromEvents_*</c>.
/// </remarks>
[Config(typeof(ImmBenchmarkConfig))]
[BenchmarkCategory("Yaml")]
public class YamlCostBreakdownBenchmarks
{
    private string rulesetYaml = string.Empty;
    private string immYaml = string.Empty;
    private ParsingEvent[] rulesetEvents = [];
    private ParsingEvent[] immEvents = [];

    [Params(18)]
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

        this.rulesetEvents = ParseAll(this.rulesetYaml);
        this.immEvents = ParseAll(this.immYaml);

        if (((RuleDefinition[])this.DeserializeFromEvents_RuleSet()).Length != this.RuleCount)
        {
            throw new InvalidOperationException("Replayed rule set events did not produce the expected rules.");
        }

        if (((RuleAssertion[])this.DeserializeFromEvents_Imm()).Length != this.RuleCount)
        {
            throw new InvalidOperationException("Replayed IMM events did not produce the expected rules.");
        }
    }

    [Benchmark(Baseline = true)]
    public IpMaturityModelRuleset Full_RuleSet() => IpMaturityModelRuleset.FromYaml(this.rulesetYaml);

    [Benchmark]
    public IpMaturityModel Full_Imm() => IpMaturityModel.FromYaml(this.immYaml);

    [Benchmark]
    public int ParseOnly_RuleSet() => CountEvents(this.rulesetYaml);

    [Benchmark]
    public int ParseOnly_Imm() => CountEvents(this.immYaml);

    [Benchmark]
    public object DeserializeFromEvents_RuleSet() =>
        ImmYamlDeserializer.Shared.Deserialize(new ReplayParser(this.rulesetEvents), typeof(RuleDefinition[]))!;

    [Benchmark]
    public object DeserializeFromEvents_Imm() =>
        ImmYamlDeserializer.Shared.Deserialize(new ReplayParser(this.immEvents), typeof(RuleAssertion[]))!;

    private static int CountEvents(string yaml)
    {
        var parser = new Parser(new StringReader(yaml));
        int n = 0;
        while (parser.MoveNext())
        {
            n++;
        }

        return n;
    }

    private static ParsingEvent[] ParseAll(string yaml)
    {
        var parser = new Parser(new StringReader(yaml));
        var events = new List<ParsingEvent>();
        while (parser.MoveNext())
        {
            events.Add(parser.Current!);
        }

        if (events[0] is not StreamStart || events[^1] is not StreamEnd)
        {
            throw new InvalidOperationException("Expected the events to run from StreamStart to StreamEnd.");
        }

        return [.. events];
    }

    /// <summary>
    /// An <see cref="IParser"/> that replays events parsed earlier, in the same order and with the
    /// same <see cref="Current"/> semantics as <see cref="Parser"/> (null before the first
    /// <see cref="MoveNext"/> and after the last event).
    /// </summary>
    private sealed class ReplayParser(ParsingEvent[] events) : IParser
    {
        private int index = -1;

        public ParsingEvent? Current => (uint)this.index < (uint)events.Length ? events[this.index] : null;

        public bool MoveNext() => ++this.index < events.Length;
    }
}
