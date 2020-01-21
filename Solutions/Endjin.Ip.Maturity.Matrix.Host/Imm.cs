namespace Endjin.Ip.Maturity.Matrix.Host
{
    #region Using Directives

    using Endjin.Badger;
    using Endjin.Imm.Domain;
    using Endjin.Imm.Processing;

    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Extensions.Caching.Memory;

    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;

    #endregion

    public class Imm
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly MemoryCacheEntryOptions cacheEntryOptions = new MemoryCacheEntryOptions();
        private readonly IMemoryCache cache;

        public Imm(IMemoryCache cache, IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
            this.cache = cache;
            this.cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(5));
        }

        [FunctionName(nameof(GitHubImmTotalScore))]
        public async Task<HttpResponseMessage> GitHubImmTotalScore([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "imm/github/{org}/{project}/total")]HttpRequestMessage request, string org, string project)
        {
            (IpMaturityMatrixRuleset ruleSet, IpMaturityMatrix ruleAssertions) = await this.GetImmRulesFromGitHubAsync(org, project).ConfigureAwait(false);
            var evaluationEngine = new EvaluationEngine(ruleSet);

            ImmEvaluation evaluationResult = evaluationEngine.Evaluate(ruleAssertions);

            string svg = BadgePainter.DrawSVG("IMM", $"{evaluationResult.TotalScore} / {evaluationResult.MaximumPossibleTotalScore}", ColorScheme.Red, Style.Flat);
            return this.CreateUncacheResponse(
                request,
                new ByteArrayContent(Encoding.ASCII.GetBytes(svg)),
                "image/svg+xml");
        }

        [FunctionName(nameof(GitHubImmByRule))]
        public async Task<HttpResponseMessage> GitHubImmByRule([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "imm/github/{org}/{project}/rule/{ruleId}")]HttpRequestMessage request, string org, string project, string ruleId)
        {
            var ruleIdAsGuid = Guid.Parse(ruleId);
            var results = await this.GetImmRulesFromGitHubAsync(org, project).ConfigureAwait(false);
            var evaluationEngine = new EvaluationEngine(results.RuleSet);
            var result = evaluationEngine.Evaluate(results.Rules).RuleEvaluations.First(x => x.RuleAssertion.Id == ruleIdAsGuid);

            return this.CreateUncacheResponse(request, new ByteArrayContent(Encoding.ASCII.GetBytes(BadgePainter.DrawSVG(WebUtility.HtmlEncode(result.RuleAssertion.Name!), $"{result.Percentage}%", GetColourSchemeForPercentage(result.Percentage), Style.Flat))), "image/svg+xml");
        }

        private HttpResponseMessage CreateUncacheResponse(HttpRequestMessage req, HttpContent content, string mediaType)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);

            response.Content = content;
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
            response.Content.Headers.TryAddWithoutValidation("expires", "-1");
            response.Headers.ETag = new EntityTagHeaderValue($"\"{Guid.NewGuid().ToString()}\"");
            response.Headers.Pragma.Add(new NameValueHeaderValue("no-cache"));
            response.Headers.CacheControl = new CacheControlHeaderValue()
            {
                NoCache = true,
                NoStore = true,
                Public = false,
                MustRevalidate = true,
            };

            return response;
        }

        private async Task<(IpMaturityMatrixRuleset RuleSet, IpMaturityMatrix Rules)> GetImmRulesFromGitHubAsync(string org, string project)
        {
            string cacheKey = $"{org}/{project}";

            if (!this.cache.TryGetValue(cacheKey, out (IpMaturityMatrixRuleset RuleSet, IpMaturityMatrix Rules) results))
            {
                HttpClient client = this.httpClientFactory.CreateClient();

                var rulesetTask = client.GetAsync(IpMaturityMatrixRuleset.RuleSetDefinitionsUrl);
                var scoreTask = client.GetAsync($"https://raw.githubusercontent.com/{org}/{project}/master/imm.yaml");

                await Task.WhenAll(new[] { rulesetTask, scoreTask }).ConfigureAwait(false);

                rulesetTask.Result.EnsureSuccessStatusCode();
                scoreTask.Result.EnsureSuccessStatusCode();

                var rulesetContentTask = rulesetTask.Result.Content.ReadAsStringAsync();
                var scoreContentTask = scoreTask.Result.Content.ReadAsStringAsync();

                await Task.WhenAll(new[] { rulesetContentTask, scoreContentTask }).ConfigureAwait(false);

                results = (IpMaturityMatrixRuleset.FromYaml(rulesetContentTask.Result), IpMaturityMatrix.FromYaml(scoreContentTask.Result));

                this.cache.Set(cacheKey, results, cacheEntryOptions);
            }

            return results;
        }

        private string GetColourSchemeForPercentage(decimal percentage)
        {
            return percentage switch
            {
                var _ when percentage < 33 => ColorScheme.Red,
                var _ when percentage < 66 => ColorScheme.Yellow,
                var _ when percentage > 66
                        && percentage < 99 => ColorScheme.YellowGreen,
                100                        => ColorScheme.Green,
                _                          => ColorScheme.Red,
            };
        }
    }
}