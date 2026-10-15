using BenchmarkDotNet.Attributes;
using Endjin.Imm.Contracts;
using Endjin.Imm.Domain;
using Endjin.Imm.Processing;
using Endjin.Imm.Repository;

namespace Endjin.Imm.Benchmarks;

/// <summary>
/// Evaluating a fully populated IMM against rule sets of increasing size.
/// </summary>
[Config(typeof(ImmBenchmarkConfig))]
[BenchmarkCategory("Evaluate")]
public class EvaluationBenchmarks
{
    private RuleDefinitionRepository repository = null!;
    private EvaluationEngine engine = null!;
    private IpMaturityMatrix imm = null!;
    private IEvaluationContext context = null!;
    private RuleAssertion lastAssertion = null!;

    [Params(18, 180)]
    public int RuleCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        string snapshot = SampleData.LoadEmbedded(SampleData.RuleSetResourceName);
        IpMaturityMatrixRuleset ruleset = IpMaturityMatrixRuleset.FromYaml(SampleData.ScaleRuleSet(snapshot, this.RuleCount));

        this.repository = new RuleDefinitionRepository(ruleset);
        this.engine = new EvaluationEngine(this.repository);
        this.imm = IpMaturityMatrix.FromYaml(SampleData.BuildImmYaml(ruleset, SampleData.ReferenceDate));
        this.context = new SampleData.FixedDateContext(SampleData.ReferenceDate);
        this.lastAssertion = this.imm.Rules[^1];

        ImmEvaluation check = this.engine.Evaluate(this.imm, this.context);
        if (check.RuleEvaluations.Count != this.RuleCount)
        {
            throw new InvalidOperationException($"Evaluated {check.RuleEvaluations.Count} rules, expected {this.RuleCount}.");
        }
    }

    [Benchmark(Baseline = true)]
    public ImmEvaluation Evaluate() => this.engine.Evaluate(this.imm, this.context);

    /// <summary>What the Host does per request: a new engine every time.</summary>
    [Benchmark]
    public ImmEvaluation NewEngine_Then_Evaluate() => new EvaluationEngine(this.repository).Evaluate(this.imm, this.context);

    /// <summary>Worst case of the linear <c>FirstOrDefault</c> lookup.</summary>
    [Benchmark]
    public RuleDefinition Repository_GetDefinitionFor_LastRule() => this.repository.GetDefinitionFor(this.lastAssertion);
}
