namespace Endjin.Ip.Maturity.Model.Host
{
    #region Using Directives

    using Endjin.Imm.Caching;
    using Endjin.Imm.Contracts;
    using Endjin.Imm.Domain;
    using Endjin.Imm.Processing;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Primitives;

    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading.Tasks;

    #endregion

    /// <summary>
    /// The badge endpoints.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When a badge cannot show a score because of the request or the content it names (the IMM
    /// or rule set does not exist or is not valid, GitHub is unavailable, or the rule id is not
    /// one the IMM scores), the badge endpoints return a light grey badge saying why, with status
    /// 200 so that image proxies show it, and the showbadges endpoint returns a short HTML page
    /// with an error status. Both set the <c>X-Imm-Error</c> header to a code identifying the
    /// problem. Any other exception is a bug, and propagates.
    /// </para>
    /// <para>
    /// Each endpoint traces its work as a span from <see cref="HostTelemetry.ActivitySourceName"/>
    /// (<c>imm.badge.total</c>, <c>imm.badge.rule</c> or <c>imm.badge.showbadges</c>) tagged with
    /// the repository and branches requested, with an <c>imm.render</c> child around rendering the
    /// badge or page. When a badge or page shows an error, the span's <c>imm.error</c> tag holds
    /// the <c>X-Imm-Error</c> code; its status is Error only when the problem is logged as a
    /// warning (GitHub is unavailable), or when an exception propagates.
    /// </para>
    /// </remarks>
    public partial class Imm
    {
        /// <summary>
        /// The name of the response header that identifies why a badge or page shows an error.
        /// </summary>
        public const string ErrorHeaderName = "X-Imm-Error";

        private const string SvgMediaType = "image/svg+xml";
        private const string TotalSubject = "IMM";
        private const string UnnamedRuleSubject = "IMM rule";

        private const string TotalActivityName = "imm.badge.total";
        private const string RuleActivityName = "imm.badge.rule";
        private const string ShowBadgesActivityName = "imm.badge.showbadges";
        private const string RenderActivityName = "imm.render";

        private readonly IRuleDefinitionRepositorySource ruleDefinitionRepositorySource;
        private readonly IIpMaturityModelSource immSource;
        private readonly ILogger<Imm> logger;

        public Imm(
            IRuleDefinitionRepositorySource ruleDefinitionRepositorySource,
            IIpMaturityModelSource immSource,
            ILogger<Imm> logger)
        {
            this.ruleDefinitionRepositorySource = ruleDefinitionRepositorySource;
            this.immSource = immSource;
            this.logger = logger;
        }

        [Function(nameof(GitHubImmTotalScore))]
        public async Task<IActionResult> GitHubImmTotalScore(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "imm/github/{org}/{project}/total")]HttpRequest request,
            string org,
            string project)
        {
            (string rulesObjectName, string projectObjectName) = GetGitHubBranchOrObjectNames(request);
            using Activity? activity = StartBadgeActivity(TotalActivityName, org, project, rulesObjectName, projectObjectName);
            try
            {
                (Task<IRuleDefinitionRepository> rulesetTask, Task<IpMaturityModel> immTask) =
                    this.FetchFromGitHub(org, project, rulesObjectName, projectObjectName);
                try
                {
                    await Task.WhenAll(rulesetTask, immTask).ConfigureAwait(false);
                    var evaluationEngine = new EvaluationEngine(rulesetTask.Result);

                    ImmEvaluation evaluationResult = evaluationEngine.Evaluate(immTask.Result);

                    string svg;
                    using (StartRenderActivity("total"))
                    {
                        svg = ImmBadges.RenderTotalBadge(evaluationResult);
                    }

                    return CreateUncacheResponse(
                        request,
                        svg,
                        SvgMediaType);
                }
                catch (Exception ex) when (TryDescribeFailure(ex, rulesetTask, org, project, rulesObjectName, projectObjectName, out ImmFailure? failure))
                {
                    return this.CreateErrorBadgeResponse(activity, request, org, project, TotalSubject, failure, ex);
                }
            }
            catch (Exception ex)
            {
                RecordUnexpectedException(activity, ex);
                throw;
            }
        }

        [Function(nameof(GitHubImmByRule))]
        public async Task<IActionResult> GitHubImmByRule(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "imm/github/{org}/{project}/rule/{ruleId}")]HttpRequest request,
            string org,
            string project,
            string ruleId)
        {
            (string rulesObjectName, string projectObjectName) = GetGitHubBranchOrObjectNames(request);
            using Activity? activity = StartBadgeActivity(RuleActivityName, org, project, rulesObjectName, projectObjectName);
            activity?.SetTag("imm.rule.id", ruleId);
            try
            {
                // Not a {ruleId:guid} route constraint, which would make the request a 404 with no badge.
                if (!Guid.TryParse(ruleId, out Guid ruleIdAsGuid))
                {
                    return this.CreateErrorBadgeResponse(activity, request, org, project, UnnamedRuleSubject, ImmFailure.InvalidRuleId(ruleId), null);
                }

                (Task<IRuleDefinitionRepository> rulesetTask, Task<IpMaturityModel> immTask) =
                    this.FetchFromGitHub(org, project, rulesObjectName, projectObjectName);
                try
                {
                    await Task.WhenAll(rulesetTask, immTask).ConfigureAwait(false);
                    IRuleDefinitionRepository ruleDefinitions = rulesetTask.Result;
                    IpMaturityModel imm = immTask.Result;
                    var evaluationEngine = new EvaluationEngine(ruleDefinitions);
                    RuleEvaluation? result = evaluationEngine.Evaluate(imm).RuleEvaluations.FirstOrDefault(x => x.RuleAssertion.Id == ruleIdAsGuid);

                    if (result is null)
                    {
                        // The evaluation leaves out rules the IMM opts out of.
                        ImmFailure failure = imm.Rules.Any(r => r is not null && r.Id == ruleIdAsGuid && r.OptOut)
                            ? ImmFailure.RuleOptedOut(ruleIdAsGuid)
                            : ImmFailure.RuleNotFound(ruleIdAsGuid);
                        return this.CreateErrorBadgeResponse(
                            activity, request, org, project, RuleSubject(rulesetTask, immTask, ruleIdAsGuid), failure, null);
                    }

                    string? definitionName = ruleDefinitions.GetDefinitionFor(result.RuleAssertion).Name;
                    string svg;
                    using (StartRenderActivity("rule"))
                    {
                        svg = ImmBadges.RenderRuleBadge(result, definitionName);
                    }

                    return CreateUncacheResponse(request, svg, SvgMediaType);
                }
                catch (Exception ex) when (TryDescribeFailure(ex, rulesetTask, org, project, rulesObjectName, projectObjectName, out ImmFailure? failure))
                {
                    return this.CreateErrorBadgeResponse(
                        activity, request, org, project, RuleSubject(rulesetTask, immTask, ruleIdAsGuid), failure, ex);
                }
            }
            catch (Exception ex)
            {
                RecordUnexpectedException(activity, ex);
                throw;
            }
        }

        [Function(nameof(GitHubImmAllBadges))]
        public async Task<IActionResult> GitHubImmAllBadges(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "imm/github/{org}/{project}/showbadges")]HttpRequest request,
            string org,
            string project)
        {
            (string rulesObjectName, string projectObjectName) = GetGitHubBranchOrObjectNames(request);
            using Activity? activity = StartBadgeActivity(ShowBadgesActivityName, org, project, rulesObjectName, projectObjectName);
            try
            {
                (Task<IRuleDefinitionRepository> rulesetTask, Task<IpMaturityModel> immTask) =
                    this.FetchFromGitHub(org, project, rulesObjectName, projectObjectName);
                try
                {
                    await Task.WhenAll(rulesetTask, immTask).ConfigureAwait(false);
                    var evaluationEngine = new EvaluationEngine(rulesetTask.Result);

                    ImmEvaluation evaluationResult = evaluationEngine.Evaluate(immTask.Result);

                    string html;
                    using (StartRenderActivity("showbadges"))
                    {
                        html = ImmBadges.RenderShowBadgesPage(org, project, rulesObjectName, projectObjectName, evaluationResult);
                    }

                    return CreateUncacheResponse(
                        request,
                        html,
                        "text/html");
                }
                catch (Exception ex) when (TryDescribeFailure(ex, rulesetTask, org, project, rulesObjectName, projectObjectName, out ImmFailure? failure))
                {
                    this.RecordFailure(activity, org, project, failure, ex);
                    string html;
                    using (StartRenderActivity("error-page"))
                    {
                        html = ImmBadges.RenderShowBadgesErrorPage(
                            org, project, rulesObjectName, projectObjectName, failure.BadgeMessage, failure.PageMessage, failure.Url, failure.Hint);
                    }

                    request.HttpContext.Response.Headers[ErrorHeaderName] = failure.Code;
                    return CreateUncacheResponse(request, html, "text/html", failure.PageStatusCode);
                }
            }
            catch (Exception ex)
            {
                RecordUnexpectedException(activity, ex);
                throw;
            }
        }

        private static ContentResult CreateUncacheResponse(HttpRequest request, string body, string mediaType, int statusCode = StatusCodes.Status200OK)
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
                StatusCode = statusCode,
            };
        }

        private static (string RuleDefinitionsObjectName, string ProjectObjectName) GetGitHubBranchOrObjectNames(
            HttpRequest request)
        {
            string definitionsObject = ObjectNameFromQuerystring("definitionsBranch", IpMaturityModelRuleset.DefaultDefinitionsBranch);
            string projectObject = ObjectNameFromQuerystring("projectBranch", "master");
            return (definitionsObject, projectObject);

            // A repeated parameter uses its first value: StringValues.ToString() would join them all with
            // commas, naming a branch that does not exist.
            string ObjectNameFromQuerystring(string name, string defaultValue) =>
                request.Query.TryGetValue(name, out StringValues value) && !string.IsNullOrEmpty(value[0])
                    ? value[0]!
                    : defaultValue;
        }

        /// <summary>
        /// Works out whether an exception is one that the endpoints report as an error badge or
        /// page (rather than letting it propagate), and if so, how to describe it.
        /// </summary>
        private static bool TryDescribeFailure(
            Exception exception,
            Task<IRuleDefinitionRepository> rulesetTask,
            string org,
            string project,
            string rulesObjectName,
            string projectObjectName,
            [NotNullWhen(true)] out ImmFailure? failure)
        {
            // The rule set and the IMM are fetched together, so the exception could have come from
            // either. (When both fail, it is the rule set's exception.)
            bool fromRuleSet = rulesetTask.IsFaulted && ReferenceEquals(rulesetTask.Exception?.InnerException, exception);

            failure = exception switch
            {
                GitHubContentNotFoundException notFound when fromRuleSet => ImmFailure.RuleSetNotFound(notFound.Url, rulesObjectName),
                GitHubContentNotFoundException notFound => ImmFailure.ImmNotFound(notFound.Url, org, project, projectObjectName),
                GitHubContentUnavailableException unavailable => ImmFailure.Unavailable(unavailable),

                // A rule set that is not valid is our problem, not the requester's, so that is not
                // reported as a badge.
                InvalidImmContentException invalid when !fromRuleSet => ImmFailure.InvalidImm(invalid),
                ImmEvaluationException evaluation => ImmFailure.InvalidImm(evaluation),
                UnknownRuleException unknownRule => ImmFailure.UnknownRule(unknownRule.RuleId, rulesObjectName),
                _ => null,
            };

            return failure is not null;
        }

        /// <summary>
        /// Gets the subject for a rule's badge: the rule's name in the IMM, or failing that in the
        /// rule set, if whichever of those was fetched names it.
        /// </summary>
        private static string RuleSubject(Task<IRuleDefinitionRepository> rulesetTask, Task<IpMaturityModel> immTask, Guid ruleId)
        {
            string? name = null;
            if (immTask.IsCompletedSuccessfully)
            {
                name = immTask.Result.Rules.FirstOrDefault(r => r is not null && r.Id == ruleId)?.Name;
            }

            if (string.IsNullOrEmpty(name) && rulesetTask.IsCompletedSuccessfully)
            {
                name = rulesetTask.Result.GetAll().FirstOrDefault(d => d is not null && d.Id == ruleId)?.Name;
            }

            return string.IsNullOrEmpty(name) ? UnnamedRuleSubject : name;
        }

        private (Task<IRuleDefinitionRepository> RuleSet, Task<IpMaturityModel> Imm) FetchFromGitHub(
            string org,
            string project,
            string ruleDefinitionsObjectName,
            string projectObjectName)
        {
            Task<IRuleDefinitionRepository> rulesetTask = this.ruleDefinitionRepositorySource.GetRuleDefinitionRepositoryAsync(ruleDefinitionsObjectName);
            Task<IpMaturityModel> immTask = this.immSource.GetIpMaturityModelAsync(org, project, projectObjectName);
            return (rulesetTask, immTask);
        }

        /// <summary>
        /// Starts the span for a badge endpoint, tagged with what was requested.
        /// </summary>
        private static Activity? StartBadgeActivity(
            string name,
            string org,
            string project,
            string rulesObjectName,
            string projectObjectName)
        {
            Activity? activity = HostTelemetry.Source.StartActivity(name, ActivityKind.Internal);
            if (activity is not null)
            {
                activity.SetTag("imm.github.owner", org);
                activity.SetTag("imm.github.repo", project);
                activity.SetTag("imm.ruleset.ref", rulesObjectName);
                activity.SetTag("imm.github.ref", projectObjectName);
            }

            return activity;
        }

        /// <summary>
        /// Starts the span around rendering a badge or page.
        /// </summary>
        /// <param name="kind">
        /// What is rendered: <c>total</c>, <c>rule</c>, <c>showbadges</c>, <c>error-badge</c> or
        /// <c>error-page</c>.
        /// </param>
        private static Activity? StartRenderActivity(string kind)
        {
            Activity? activity = HostTelemetry.Source.StartActivity(RenderActivityName, ActivityKind.Internal);
            activity?.SetTag("imm.render.kind", kind);
            return activity;
        }

        /// <summary>
        /// Marks an endpoint's span as failed by an exception that propagates, which is a bug.
        /// </summary>
        private static void RecordUnexpectedException(Activity? activity, Exception exception)
        {
            if (activity is not null)
            {
                activity.SetStatus(ActivityStatusCode.Error, exception.Message);
                activity.SetTag("error.type", exception.GetType().FullName);
                activity.AddException(exception);
            }
        }

        private ContentResult CreateErrorBadgeResponse(
            Activity? activity,
            HttpRequest request,
            string org,
            string project,
            string subject,
            ImmFailure failure,
            Exception? exception)
        {
            this.RecordFailure(activity, org, project, failure, exception);
            request.HttpContext.Response.Headers[ErrorHeaderName] = failure.Code;
            string svg;
            using (StartRenderActivity("error-badge"))
            {
                svg = ImmBadges.RenderErrorBadge(subject, failure.BadgeMessage);
            }

            return CreateUncacheResponse(request, svg, SvgMediaType);
        }

        /// <summary>
        /// Logs a problem shown as an error badge or page, and records it on the endpoint's span.
        /// </summary>
        /// <remarks>
        /// The span's status follows the logging policy: a problem that is logged as a warning
        /// (or worse) is one we want to hear about, so it makes the span an error; the others are
        /// a correct answer to a request for something that is missing or not valid, so they only
        /// tag it.
        /// </remarks>
        private void RecordFailure(Activity? activity, string org, string project, ImmFailure failure, Exception? exception)
        {
            bool isWarning = failure.LogLevel >= LogLevel.Warning;
            if (activity is not null)
            {
                activity.SetTag("imm.error", failure.Code);
                if (isWarning)
                {
                    activity.SetStatus(ActivityStatusCode.Error, failure.PageMessage);
                }
            }

            this.LogImmNotShown(
                failure.LogLevel,
                isWarning ? exception : null,
                org,
                project,
                failure.Code,
                failure.PageMessage,
                failure.Url ?? "none");
        }

        [LoggerMessage(EventId = 1, Message = "IMM for {Org}/{Project} not shown ({ImmError}): {Detail} (URL: {Url})")]
        private partial void LogImmNotShown(
            LogLevel level,
            Exception? exception,
            string org,
            string project,
            string immError,
            string detail,
            string url);

        /// <summary>
        /// Describes a problem that is shown to the requester as an error badge or page.
        /// </summary>
        /// <param name="Code">The value of the <c>X-Imm-Error</c> header.</param>
        /// <param name="BadgeMessage">The badge's status text.</param>
        /// <param name="PageStatusCode">The status code of the showbadges error page.</param>
        /// <param name="PageMessage">What went wrong, for the showbadges error page and the log.</param>
        /// <param name="Url">The URL requested from GitHub, if the problem is with one.</param>
        /// <param name="Hint">What to try next, for the showbadges error page.</param>
        /// <param name="LogLevel">The level at which to log the problem.</param>
        private sealed record ImmFailure(
            string Code,
            string BadgeMessage,
            int PageStatusCode,
            string PageMessage,
            string? Url,
            string? Hint,
            LogLevel LogLevel)
        {
            public static ImmFailure ImmNotFound(Uri url, string org, string project, string projectObjectName) => new(
                "imm-not-found",
                "imm.yaml not found",
                StatusCodes.Status404NotFound,
                $"No imm.yaml was found for {org}/{project} on branch '{projectObjectName}'.",
                url.OriginalString,
                "Check the repository name and branch. If the repository's default branch is main, add ?projectBranch=main to this page's URL.",
                LogLevel.Information);

            public static ImmFailure RuleSetNotFound(Uri url, string rulesObjectName) => new(
                "ruleset-not-found",
                "rule set not found",
                StatusCodes.Status404NotFound,
                $"No rule set was found on branch '{rulesObjectName}'.",
                url.OriginalString,
                $"Check the definitionsBranch parameter, or remove it to use the rule set on {IpMaturityModelRuleset.DefaultDefinitionsBranch}.",
                LogLevel.Information);

            public static ImmFailure Unavailable(GitHubContentUnavailableException exception) => new(
                "upstream-unavailable",
                "unavailable",
                StatusCodes.Status502BadGateway,
                exception.StatusCode is { } status
                    ? $"GitHub returned {(int)status} ({status})."
                    : "GitHub could not be reached.",
                exception.Url.OriginalString,
                "Try again later.",
                LogLevel.Warning);

            public static ImmFailure InvalidImm(InvalidImmContentException exception) => new(
                "invalid-imm",
                "invalid imm.yaml",
                StatusCodes.Status422UnprocessableEntity,
                $"The imm.yaml is not valid: {exception.Problem}",
                exception.Url.OriginalString,
                null,
                LogLevel.Information);

            public static ImmFailure InvalidImm(ImmEvaluationException exception) => new(
                "invalid-imm",
                "invalid imm.yaml",
                StatusCodes.Status422UnprocessableEntity,
                $"The imm.yaml is not valid. {exception.Message}",
                null,
                null,
                LogLevel.Information);

            public static ImmFailure UnknownRule(Guid ruleId, string rulesObjectName) => new(
                "unknown-rule",
                "unknown rule",
                StatusCodes.Status422UnprocessableEntity,
                $"The imm.yaml refers to rule id {ruleId}, which the rule set on branch '{rulesObjectName}' does not define.",
                null,
                "Check the rule id, or use the definitionsBranch parameter to choose the rule set the imm.yaml was written for.",
                LogLevel.Information);

            public static ImmFailure InvalidRuleId(string ruleId) => new(
                "invalid-rule-id",
                "invalid rule id",
                StatusCodes.Status400BadRequest,
                $"'{ruleId}' is not a rule id.",
                null,
                null,
                LogLevel.Information);

            public static ImmFailure RuleNotFound(Guid ruleId) => new(
                "rule-not-found",
                "rule not found",
                StatusCodes.Status404NotFound,
                $"The imm.yaml has no rule with id {ruleId}.",
                null,
                null,
                LogLevel.Information);

            public static ImmFailure RuleOptedOut(Guid ruleId) => new(
                "rule-opted-out",
                "opted out",
                StatusCodes.Status200OK,
                $"The imm.yaml opts out of rule {ruleId}.",
                null,
                null,
                LogLevel.Debug);
        }
    }
}