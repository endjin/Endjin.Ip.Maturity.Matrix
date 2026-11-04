using BenchmarkDotNet.Attributes;
using Endjin.Imm.Contracts;
using Endjin.Imm.Domain;
using Endjin.Imm.Processing;
using Endjin.Imm.Repository;
using Endjin.Ip.Maturity.Model.Host;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using ImmFunctions = Endjin.Ip.Maturity.Model.Host.Imm;

namespace Endjin.Imm.Benchmarks;

/// <summary>
/// End-to-end work done by each Host endpoint after the HTTP cache lookup, using the 18-rule
/// snapshot and a fully populated IMM.
/// </summary>
/// <remarks>
/// <para>
/// "CacheHit" starts from already deserialised objects, as the Host does when
/// <c>HttpDeserializingCache</c> returns a completed task. "CacheMiss" adds both <c>FromYaml</c> calls
/// (but no HTTP). These evaluate with the fixed reference date and then render with the Host's own
/// <see cref="ImmBadges"/>.
/// </para>
/// <para>
/// "HostEndpoint" calls the Host's <see cref="ImmFunctions"/> function methods themselves, with sources that
/// return the already deserialised objects as completed tasks, so they also include query string
/// parsing, the response headers and the <see cref="ContentResult"/>. Like the Host, they evaluate
/// with the current date.
/// </para>
/// </remarks>
[Config(typeof(ImmBenchmarkConfig))]
[BenchmarkCategory("Pipeline")]
public class RequestPipelineBenchmarks
{
    private const string Org = "endjin";
    private const string Project = "Endjin.Ip.Maturity.Model";
    private const string RulesObjectName = IpMaturityModelRuleset.DefaultDefinitionsBranch;
    private const string ProjectObjectName = "master";

    private static readonly Guid FrameworkRuleId = Guid.Parse(SampleData.FrameworkRuleId);

    private string rulesetYaml = string.Empty;
    private string immYaml = string.Empty;
    private IRuleDefinitionRepository repository = null!;
    private IpMaturityModel imm = null!;
    private IEvaluationContext context = null!;
    private ImmFunctions functions = null!;
    private HttpRequest request = null!;

    [GlobalSetup]
    public void Setup()
    {
        this.rulesetYaml = SampleData.LoadEmbedded(SampleData.RuleSetResourceName);
        IpMaturityModelRuleset ruleset = IpMaturityModelRuleset.FromYaml(this.rulesetYaml);
        this.immYaml = SampleData.BuildImmYaml(ruleset, SampleData.ReferenceDate);

        this.repository = new RuleDefinitionRepository(ruleset);
        this.imm = IpMaturityModel.FromYaml(this.immYaml);
        this.context = new SampleData.FixedDateContext(SampleData.ReferenceDate);

        this.functions = new ImmFunctions(
            new CachedRuleDefinitionSource(this.repository),
            new CachedImmSource(this.imm),
            NullLogger<ImmFunctions>.Instance);

        // One request is reused by every invocation: the functions only read its (empty) query
        // string and overwrite the same response headers each time.
        this.request = new DefaultHttpContext().Request;

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
        IpMaturityModelRuleset ruleset = IpMaturityModelRuleset.FromYaml(this.rulesetYaml);
        IpMaturityModel freshImm = IpMaturityModel.FromYaml(this.immYaml);
        return this.TotalBadge(new RuleDefinitionRepository(ruleset), freshImm);
    }

    /// <summary>GET imm/github/{org}/{project}/rule/{ruleId} for the Framework rule.</summary>
    [Benchmark]
    public string RuleBadge_CacheHit()
    {
        var evaluationEngine = new EvaluationEngine(this.repository);
        RuleEvaluation result = evaluationEngine.Evaluate(this.imm, this.context).RuleEvaluations.First(x => x.RuleAssertion.Id == FrameworkRuleId);

        return ImmBadges.RenderRuleBadge(result);
    }

    /// <summary>GET imm/github/{org}/{project}/showbadges.</summary>
    [Benchmark]
    public string ShowBadges_CacheHit()
    {
        var evaluationEngine = new EvaluationEngine(this.repository);
        ImmEvaluation evaluationResult = evaluationEngine.Evaluate(this.imm, this.context);
        return ImmBadges.RenderShowBadgesPage(Org, Project, RulesObjectName, ProjectObjectName, evaluationResult);
    }

    /// <summary><see cref="ImmFunctions.GitHubImmTotalScore"/> with both inputs already cached.</summary>
    [Benchmark]
    public Task<IActionResult> HostEndpoint_TotalBadge() =>
        this.functions.GitHubImmTotalScore(this.request, Org, Project);

    /// <summary><see cref="ImmFunctions.GitHubImmByRule"/> for the Framework rule with both inputs already cached.</summary>
    [Benchmark]
    public Task<IActionResult> HostEndpoint_RuleBadge() =>
        this.functions.GitHubImmByRule(this.request, Org, Project, SampleData.FrameworkRuleId);

    /// <summary><see cref="ImmFunctions.GitHubImmAllBadges"/> with both inputs already cached.</summary>
    [Benchmark]
    public Task<IActionResult> HostEndpoint_ShowBadges() =>
        this.functions.GitHubImmAllBadges(this.request, Org, Project);

    private string TotalBadge(IRuleDefinitionRepository ruleSet, IpMaturityModel ruleAssertions)
    {
        var evaluationEngine = new EvaluationEngine(ruleSet);

        ImmEvaluation evaluationResult = evaluationEngine.Evaluate(ruleAssertions, this.context);

        return ImmBadges.RenderTotalBadge(evaluationResult);
    }

    // Stand-ins for the GitHub sources on a cache hit: each returns the same completed task.
    private sealed class CachedRuleDefinitionSource(IRuleDefinitionRepository repository) : IRuleDefinitionRepositorySource
    {
        private readonly Task<IRuleDefinitionRepository> result = Task.FromResult(repository);

        public Task<IRuleDefinitionRepository> GetRuleDefinitionRepositoryAsync(string name) => this.result;
    }

    private sealed class CachedImmSource(IpMaturityModel imm) : IIpMaturityModelSource
    {
        private readonly Task<IpMaturityModel> result = Task.FromResult(imm);

        public Task<IpMaturityModel> GetIpMaturityModelAsync(string gitHubOwner, string gitHubProject, string objectName) => this.result;
    }
}
