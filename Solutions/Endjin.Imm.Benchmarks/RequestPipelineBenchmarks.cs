using System.Net;
using BenchmarkDotNet.Attributes;
using Endjin.Badger;
using Endjin.Imm.Contracts;
using Endjin.Imm.Domain;
using Endjin.Imm.Processing;
using Endjin.Imm.Repository;

namespace Endjin.Imm.Benchmarks;

/// <summary>
/// End-to-end work done by each Host endpoint after the HTTP cache lookup, using the 18-rule
/// snapshot and a fully populated IMM.
/// </summary>
/// <remarks>
/// <para>
/// "CacheHit" starts from already deserialised objects, as the Host does when
/// <c>HttpDeserializingCache</c> returns a completed task. "CacheMiss" adds both <c>FromYaml</c> calls
/// (but no HTTP). The Host evaluates with the current date; these use the fixed reference date.
/// </para>
/// </remarks>
[Config(typeof(ImmBenchmarkConfig))]
[BenchmarkCategory("Pipeline")]
public class RequestPipelineBenchmarks
{
    private const string Org = "endjin";
    private const string Project = "Endjin.Ip.Maturity.Model";
    private const string RulesObjectName = "master";
    private const string ProjectObjectName = "master";

    private static readonly Guid FrameworkRuleId = Guid.Parse(SampleData.FrameworkRuleId);

    private string rulesetYaml = string.Empty;
    private string immYaml = string.Empty;
    private IRuleDefinitionRepository repository = null!;
    private IpMaturityMatrix imm = null!;
    private IEvaluationContext context = null!;

    [GlobalSetup]
    public void Setup()
    {
        this.rulesetYaml = SampleData.LoadEmbedded(SampleData.RuleSetResourceName);
        IpMaturityMatrixRuleset ruleset = IpMaturityMatrixRuleset.FromYaml(this.rulesetYaml);
        this.immYaml = SampleData.BuildImmYaml(ruleset, SampleData.ReferenceDate);

        this.repository = new RuleDefinitionRepository(ruleset);
        this.imm = IpMaturityMatrix.FromYaml(this.immYaml);
        this.context = new SampleData.FixedDateContext(SampleData.ReferenceDate);

        // Warm up the font collection used by BadgePainter.
        _ = this.TotalBadge_CacheHit();
    }

    /// <summary>GET imm/github/{org}/{project}/total with both inputs already cached.</summary>
    [Benchmark(Baseline = true)]
    public string TotalBadge_CacheHit() => this.TotalBadge(this.repository, this.imm);

    /// <summary>GET imm/github/{org}/{project}/total when both inputs must be deserialised.</summary>
    [Benchmark]
    public string TotalBadge_CacheMiss()
    {
        IpMaturityMatrixRuleset ruleset = IpMaturityMatrixRuleset.FromYaml(this.rulesetYaml);
        IpMaturityMatrix freshImm = IpMaturityMatrix.FromYaml(this.immYaml);
        return this.TotalBadge(new RuleDefinitionRepository(ruleset), freshImm);
    }

    /// <summary>GET imm/github/{org}/{project}/rule/{ruleId} for the Framework rule.</summary>
    [Benchmark]
    public string RuleBadge_CacheHit()
    {
        var evaluationEngine = new EvaluationEngine(this.repository);
        RuleEvaluation result = evaluationEngine.Evaluate(this.imm, this.context).RuleEvaluations.First(x => x.RuleAssertion.Id == FrameworkRuleId);

        return BadgePainter.DrawSVG(WebUtility.HtmlEncode(result.RuleAssertion.Name!), $"{result.Percentage}%", BadgeColour.ForPercentage(result.Percentage), Style.Flat);
    }

    /// <summary>GET imm/github/{org}/{project}/showbadges.</summary>
    [Benchmark]
    public string ShowBadges_CacheHit()
    {
        var evaluationEngine = new EvaluationEngine(this.repository);
        ImmEvaluation evaluationResult = evaluationEngine.Evaluate(this.imm, this.context);
        return ShowBadgesHtml.Render(Org, Project, RulesObjectName, ProjectObjectName, evaluationResult);
    }

    private string TotalBadge(IRuleDefinitionRepository ruleSet, IpMaturityMatrix ruleAssertions)
    {
        var evaluationEngine = new EvaluationEngine(ruleSet);

        ImmEvaluation evaluationResult = evaluationEngine.Evaluate(ruleAssertions, this.context);

        return BadgePainter.DrawSVG(
            "IMM",
            $"{evaluationResult.TotalScore} / {evaluationResult.MaximumPossibleTotalScore}",
            BadgeColour.ForPercentage(TotalScore.Percentage(evaluationResult)),
            Style.Flat);
    }
}
