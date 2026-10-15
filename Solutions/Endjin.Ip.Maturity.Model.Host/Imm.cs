namespace Endjin.Ip.Maturity.Model.Host
{
    #region Using Directives

    using Endjin.Imm.Contracts;
    using Endjin.Imm.Domain;
    using Endjin.Imm.Processing;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Extensions.Primitives;

    using System;
    using System.Linq;
    using System.Threading.Tasks;

    #endregion

    public class Imm
    {
        private readonly IRuleDefinitionRepositorySource ruleDefinitionRepositorySource;
        private readonly IIpMaturityMatrixSource immSource;

        public Imm(
            IRuleDefinitionRepositorySource ruleDefinitionRepositorySource,
            IIpMaturityMatrixSource immSource)
        {
            this.ruleDefinitionRepositorySource = ruleDefinitionRepositorySource;
            this.immSource = immSource;
        }

        [Function(nameof(GitHubImmTotalScore))]
        public async Task<IActionResult> GitHubImmTotalScore(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "imm/github/{org}/{project}/total")]HttpRequest request,
            string org,
            string project)
        {
            (string rulesObjectName, string projectObjectName) = GetGitHubBranchOrObjectNames(request);
            (IRuleDefinitionRepository ruleSet, IpMaturityMatrix ruleAssertions) =
                await this.GetImmRulesFromGitHubAsync(org, project, rulesObjectName, projectObjectName).ConfigureAwait(false);
            var evaluationEngine = new EvaluationEngine(ruleSet);

            ImmEvaluation evaluationResult = evaluationEngine.Evaluate(ruleAssertions);

            string svg = ImmBadges.RenderTotalBadge(evaluationResult);
            return CreateUncacheResponse(
                request,
                svg,
                "image/svg+xml");
        }

        [Function(nameof(GitHubImmByRule))]
        public async Task<IActionResult> GitHubImmByRule(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "imm/github/{org}/{project}/rule/{ruleId}")]HttpRequest request,
            string org,
            string project,
            string ruleId)
        {
            var ruleIdAsGuid = Guid.Parse(ruleId);
            (string rulesObjectName, string projectObjectName) = GetGitHubBranchOrObjectNames(request);
            (IRuleDefinitionRepository ruleDefinitions, IpMaturityMatrix imm) =
                await this.GetImmRulesFromGitHubAsync(org, project, rulesObjectName, projectObjectName).ConfigureAwait(false);
            var evaluationEngine = new EvaluationEngine(ruleDefinitions);
            RuleEvaluation result = evaluationEngine.Evaluate(imm).RuleEvaluations.First(x => x.RuleAssertion.Id == ruleIdAsGuid);

            string? definitionName = ruleDefinitions.GetDefinitionFor(result.RuleAssertion).Name;
            return CreateUncacheResponse(request, ImmBadges.RenderRuleBadge(result, definitionName), "image/svg+xml");
        }

        [Function(nameof(GitHubImmAllBadges))]
        public async Task<IActionResult> GitHubImmAllBadges(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "imm/github/{org}/{project}/showbadges")]HttpRequest request,
            string org,
            string project)
        {
            (string rulesObjectName, string projectObjectName) = GetGitHubBranchOrObjectNames(request);
            (IRuleDefinitionRepository ruleSet, IpMaturityMatrix ruleAssertions) =
                await this.GetImmRulesFromGitHubAsync(org, project, rulesObjectName, projectObjectName).ConfigureAwait(false);
            var evaluationEngine = new EvaluationEngine(ruleSet);

            ImmEvaluation evaluationResult = evaluationEngine.Evaluate(ruleAssertions);

            string html = ImmBadges.RenderShowBadgesPage(org, project, rulesObjectName, projectObjectName, evaluationResult);

            return CreateUncacheResponse(
                request,
                html,
                "text/html");
        }

        private static ContentResult CreateUncacheResponse(HttpRequest request, string body, string mediaType)
        {
            IHeaderDictionary headers = request.HttpContext.Response.Headers;
            headers.CacheControl = "no-cache, no-store, must-revalidate";
            headers.Pragma = "no-cache";
            headers.Expires = "-1";
            headers.ETag = $"\"{Guid.NewGuid()}\"";

            return new ContentResult
            {
                Content = body,
                ContentType = mediaType,
                StatusCode = StatusCodes.Status200OK,
            };
        }

        private static (string RuleDefinitionsObjectName, string ProjectObjectName) GetGitHubBranchOrObjectNames(
            HttpRequest request)
        {
            string definitionsObject = ObjectNameFromQuerystring("definitionsBranch");
            string projectObject = ObjectNameFromQuerystring("projectBranch");
            return (definitionsObject, projectObject);

            // A repeated parameter uses its first value: StringValues.ToString() would join them all with
            // commas, naming a branch that does not exist.
            string ObjectNameFromQuerystring(string name) =>
                request.Query.TryGetValue(name, out StringValues value) && !string.IsNullOrEmpty(value[0])
                    ? value[0]!
                    : "master";
        }

        private async Task<(IRuleDefinitionRepository RuleSet, IpMaturityMatrix Rules)> GetImmRulesFromGitHubAsync(
            string org,
            string project,
            string ruleDefinitionsObjectName,
            string projectObjectName)
        {
            Task<IRuleDefinitionRepository> rulesetTask = this.ruleDefinitionRepositorySource.GetRuleDefinitionRepositoryAsync(ruleDefinitionsObjectName);
            Task<IpMaturityMatrix> immTask = this.immSource.GetIpMaturityMatrixAsync(org, project, projectObjectName);

            await Task.WhenAll(new Task[] { rulesetTask, immTask }).ConfigureAwait(false);

            return (rulesetTask.Result, immTask.Result);
        }
    }
}