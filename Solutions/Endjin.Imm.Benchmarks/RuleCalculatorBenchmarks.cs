using BenchmarkDotNet.Attributes;
using Endjin.Imm.Contracts;
using Endjin.Imm.Domain;
using Endjin.Imm.Processing;
using Endjin.Imm.Repository;

namespace Endjin.Imm.Benchmarks;

/// <summary>
/// One <c>Percentage</c> + <c>Score</c> calculation per rule data type (what
/// <see cref="EvaluationEngine"/> used to do for each rule), and the single <c>Evaluate</c>
/// call that replaced them.
/// </summary>
[Config(typeof(ImmBenchmarkConfig))]
[BenchmarkCategory("Evaluate")]
public class RuleCalculatorBenchmarks
{
    private const string AgeLegacyScoreYaml = """
        -
            Name: 'Date of Last IP Review'
            Id: da4ed776-0365-4d8a-a297-c4e91a14d646
            Measures:
                -
                    Score: 2
                    Description: '> 1 month'
        """;

    private const string FrameworkWildcardYaml = """
        -
            Name: 'Framework Version'
            Id: 6c0402b3-f0e3-4bd7-83fe-04bb6dca7924
            Measures:
                -
                    Framework: 'node99'
        """;

    private IEvaluationContext context = null!;

    private Case continuous;
    private Case discrete;
    private Case ageWithDate;
    private Case ageLegacyScore;
    private Case frameworkMatch;
    private Case frameworkWildcard;

    [GlobalSetup]
    public void Setup()
    {
        string snapshot = SampleData.LoadEmbedded(SampleData.RuleSetResourceName);
        this.context = new SampleData.FixedDateContext(SampleData.ReferenceDate);

        this.continuous = CreateCase(snapshot, SampleData.ContinuousRuleId, DataType.Continuous, null);
        this.discrete = CreateCase(snapshot, SampleData.DiscreteRuleId, DataType.Discrete, null);
        this.ageWithDate = CreateCase(snapshot, SampleData.AgeRuleId, DataType.Age, null);
        this.ageLegacyScore = CreateCase(snapshot, SampleData.AgeRuleId, DataType.Age, AgeLegacyScoreYaml);
        this.frameworkMatch = CreateCase(snapshot, SampleData.FrameworkRuleId, DataType.Framework, null);
        this.frameworkWildcard = CreateCase(snapshot, SampleData.FrameworkRuleId, DataType.Framework, FrameworkWildcardYaml);

        // Sanity checks: these pin down that each case exercises the intended code path.
        Expect(this.continuous, 3);        // Every non-zero Deployment measure.
        Expect(this.discrete, 1);          // The single, highest-scoring measure.
        Expect(this.ageWithDate, 2);     // 45 days old: within <P3M but not <P1M.
        Expect(this.ageLegacyScore, 2);    // No Date: falls back to the asserted Score.
        Expect(this.frameworkMatch, 3);    // First non-wildcard framework.
        Expect(this.frameworkWildcard, 1); // Unknown framework: matches '*'.

        void Expect(Case c, long score)
        {
            long actual = c.Calculator.Score(c.Assertion, this.context);
            if (actual != score)
            {
                throw new InvalidOperationException($"Expected score {score} for rule {c.Assertion.Id}, got {actual}.");
            }
        }
    }

    [Benchmark]
    public decimal Continuous() => this.Run(this.continuous);

    [Benchmark]
    public decimal Discrete() => this.Run(this.discrete);

    [Benchmark(Baseline = true)]
    public decimal Age_WithDate() => this.Run(this.ageWithDate);

    [Benchmark]
    public decimal Age_LegacyScore() => this.Run(this.ageLegacyScore);

    [Benchmark]
    public decimal Framework_Match() => this.Run(this.frameworkMatch);

    [Benchmark]
    public decimal Framework_Wildcard() => this.Run(this.frameworkWildcard);

    // The *_Evaluate variants compute the percentage and score in the single pass that
    // EvaluationEngine now uses (IRuleCalculator.Evaluate).

    [Benchmark]
    public RuleEvaluation Continuous_Evaluate() => this.RunEvaluate(this.continuous);

    [Benchmark]
    public RuleEvaluation Discrete_Evaluate() => this.RunEvaluate(this.discrete);

    [Benchmark]
    public RuleEvaluation Age_WithDate_Evaluate() => this.RunEvaluate(this.ageWithDate);

    [Benchmark]
    public RuleEvaluation Age_LegacyScore_Evaluate() => this.RunEvaluate(this.ageLegacyScore);

    [Benchmark]
    public RuleEvaluation Framework_Match_Evaluate() => this.RunEvaluate(this.frameworkMatch);

    [Benchmark]
    public RuleEvaluation Framework_Wildcard_Evaluate() => this.RunEvaluate(this.frameworkWildcard);

    private static Case CreateCase(string snapshot, string ruleId, DataType expectedType, string? immYaml)
    {
        IpMaturityMatrixRuleset ruleset = IpMaturityMatrixRuleset.FromYaml(SampleData.ExtractRule(snapshot, ruleId));
        RuleDefinition definition = ruleset.Rules.Single();
        if (definition.DataType != expectedType)
        {
            throw new InvalidOperationException($"Rule {ruleId} is {definition.DataType}, expected {expectedType}.");
        }

        RuleDefinitionRepository repository = new(ruleset);
        IRuleCalculator calculator = new RuleCalculatorFactory(repository).Create(definition.DataType);
        RuleAssertion assertion = IpMaturityMatrix
            .FromYaml(immYaml ?? SampleData.BuildImmYaml(ruleset, SampleData.ReferenceDate))
            .Rules
            .Single();

        return new Case(calculator, assertion);
    }

    private decimal Run(Case c) => c.Calculator.Percentage(c.Assertion, this.context) + c.Calculator.Score(c.Assertion, this.context);

    private RuleEvaluation RunEvaluate(Case c) => c.Calculator.Evaluate(c.Assertion, this.context);

    private readonly record struct Case(IRuleCalculator Calculator, RuleAssertion Assertion);
}
