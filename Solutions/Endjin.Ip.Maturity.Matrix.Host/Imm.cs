namespace Endjin.Ip.Maturity.Matrix.Host
{
    #region Using Directives

    using Endjin.Badger;
    using Endjin.Imm.Contracts;
    using Endjin.Imm.Domain;
    using Endjin.Imm.Processing;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Extensions.Primitives;

    using System;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
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

            string svg = BadgePainter.DrawSVG(
                "IMM",
                $"{evaluationResult.TotalScore} / {evaluationResult.MaximumPossibleTotalScore}",
                GetColourSchemeForPercentage(100M * evaluationResult.TotalScore / evaluationResult.MaximumPossibleTotalScore),
                Style.Flat);
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

            return CreateUncacheResponse(request, BadgePainter.DrawSVG(WebUtility.HtmlEncode(result.RuleAssertion.Name!), $"{result.Percentage}%", GetColourSchemeForPercentage(result.Percentage), Style.Flat), "image/svg+xml");
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

            string encodedRulesObjectName = WebUtility.UrlEncode(rulesObjectName);
            string encodedProjectObjectName = WebUtility.UrlEncode(projectObjectName);

            var sb = new StringBuilder();
            using (var sw = new StringWriter(sb))
            {
                sw.WriteLine("<!DOCTYPE html>");
                sw.WriteLine("<html>");
                sw.WriteLine("<head>");
                sw.WriteLine("    <meta charset=\"utf-8\" />");
                sw.Write("    <title>IP Maturity Matrix for");
                sw.Write(org);
                sw.Write("/");
                sw.Write(org);
                sw.Write(" (");
                sw.Write(projectObjectName);
                if (rulesObjectName != "master")
                {
                    sw.Write(", rules: ");
                    sw.Write(WebUtility.HtmlEncode(rulesObjectName));
                }
                sw.WriteLine(")</title>");
                sw.WriteLine("<body>");

                sw.WriteLine("  <div>");
                sw.WriteLine("  <p>Total score</p>");

                sw.Write("    <img src=\"/api/imm/github/");
                sw.Write(org);
                sw.Write("/");
                sw.Write(project);
                sw.Write("/total");
                sw.Write("?cache=false&definitionsBranch=");
                sw.Write(encodedRulesObjectName);
                sw.Write("&projectBranch=");
                sw.Write(encodedProjectObjectName);
                sw.WriteLine("\" />");

                sw.WriteLine("  </div>");

                sw.WriteLine("  <div>");
                sw.WriteLine("  <p>Rules</p>");

                sw.WriteLine("  <table>");
                sw.WriteLine("    <tbody>");

                foreach (var evaluation in evaluationResult.RuleEvaluations)
                {
                    sw.WriteLine("      <tr>");
                    sw.WriteLine("        <td>");
                    sw.Write("          <img src=\"/api/imm/github/");
                    sw.Write(org);
                    sw.Write("/");
                    sw.Write(project);
                    sw.Write("/rule/");
                    sw.Write(evaluation.RuleAssertion.Id);
                    sw.Write("?cache=false&definitionsBranch=");
                    sw.Write(encodedRulesObjectName);
                    sw.Write("&projectBranch=");
                    sw.Write(encodedProjectObjectName);
                    sw.WriteLine("\" />");
                    sw.WriteLine("        </td>");
                    sw.WriteLine("      </tr>");
                }

                sw.WriteLine("    </tbody>");
                sw.WriteLine("  </table>");
                sw.WriteLine("  </div>");
                sw.WriteLine("</body>");
                sw.WriteLine("</html>");
            }

            //string svg = BadgePainter.DrawSVG("IMM", $"{evaluationResult.TotalScore} / {evaluationResult.MaximumPossibleTotalScore}", ColorScheme.Red, Style.Flat);
            return CreateUncacheResponse(
                request,
                sb.ToString(),
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

            string ObjectNameFromQuerystring(string name) =>
                request.Query.TryGetValue(name, out StringValues value) && !StringValues.IsNullOrEmpty(value)
                    ? value.ToString()
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

        private static string GetColourSchemeForPercentage(decimal percentage)
        {
            return percentage switch
            {
                var _ when percentage < 33 => ColorScheme.Red,
                var _ when percentage < 66 => ColorScheme.Yellow,
                var _ when percentage > 66
                        && percentage < 99 => ColorScheme.YellowGreen,
                100 => ColorScheme.Green,
                _ => ColorScheme.Red,
            };
        }
    }
}