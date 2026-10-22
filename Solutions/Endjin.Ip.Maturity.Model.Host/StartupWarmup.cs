namespace Endjin.Ip.Maturity.Model.Host
{
    #region Using Directives

    using Endjin.Badger;
    using Endjin.Imm.Contracts;
    using Endjin.Imm.Domain;
    using Endjin.Imm.Processing;
    using Endjin.Imm.Repository;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    #endregion

    /// <summary>
    /// Does, in the background as soon as the worker starts, the one-off work that would otherwise
    /// make the first badge request after a cold start slow.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The first request to a new instance pays for JIT compilation of the badge rendering, YAML
    /// and evaluation code, for building the YAML deserializer, and for the first fetch of the
    /// canonical rule set from GitHub (DNS, TLS and the download). This service starts that work
    /// on the thread pool and returns immediately, so it never delays the worker's startup (on
    /// Flex Consumption, specialization has a hard time limit). Requests that arrive before it
    /// finishes are served as they would be without it.
    /// </para>
    /// <para>
    /// Every step catches its own failures, so the warm-up can never fault or take the worker
    /// down. It writes one log line when it finishes, starting "Warm-up:", with the time each
    /// step took and whether it succeeded.
    /// </para>
    /// <para>
    /// It is also traced, as a trace of its own rather than part of whatever was going on when the
    /// worker started: an <c>imm.warmup</c> span from <see cref="HostTelemetry.ActivitySourceName"/>
    /// with a child for each step (<c>imm.warmup.badges</c>, <c>imm.warmup.yaml</c> and
    /// <c>imm.warmup.ruleset</c>). A step that fails has status Error, as does <c>imm.warmup</c>,
    /// whose <c>imm.warmup.failed</c> tag says whether any step failed.
    /// </para>
    /// </remarks>
    public sealed partial class StartupWarmup : IHostedService
    {
        private const string WarmupActivityName = "imm.warmup";
        private const string BadgesActivityName = "imm.warmup.badges";
        private const string YamlActivityName = "imm.warmup.yaml";
        private const string RuleSetActivityName = "imm.warmup.ruleset";

        /// <summary>
        /// The branch of the rule set that the badges use when no other branch is requested.
        /// </summary>
        private const string DefaultRuleSetBranch = IpMaturityModelRuleset.DefaultDefinitionsBranch;

        private const string WarmupRuleId = "00000000-0000-0000-0000-000000000001";

        private const string WarmupRuleSetYaml = $"""
            -
                Name: 'Warm-up'
                Id: '{WarmupRuleId}'
                DataType: Continuous
                Measures:
                    -
                        Score: 1
                        Description: Done
                    -
                        Score: 1
                        Description: 'Not done'
            """;

        private const string WarmupImmYaml = $"""
            -
                Name: 'Warm-up'
                Id: '{WarmupRuleId}'
                Measures:
                    -
                        Score: 1
                        Description: Done
            """;

        private readonly IRuleDefinitionRepositorySource ruleDefinitionRepositorySource;
        private readonly ILogger<StartupWarmup> logger;
        private Task completion = Task.CompletedTask;

        /// <summary>
        /// Creates a <see cref="StartupWarmup"/>.
        /// </summary>
        /// <param name="ruleDefinitionRepositorySource">
        /// The source of rule sets, which caches what it fetches, so that fetching the default rule
        /// set here means the first request finds it in the cache.
        /// </param>
        /// <param name="logger">The logger.</param>
        public StartupWarmup(
            IRuleDefinitionRepositorySource ruleDefinitionRepositorySource,
            ILogger<StartupWarmup> logger)
        {
            this.ruleDefinitionRepositorySource = ruleDefinitionRepositorySource;
            this.logger = logger;
        }

        /// <summary>
        /// Gets a task that completes when the warm-up has finished. It never faults.
        /// </summary>
        /// <remarks>
        /// Nothing in the Host waits for this; it is here so that tests and diagnostics can.
        /// </remarks>
        public Task Completion => this.completion;

        /// <summary>
        /// Starts the warm-up in the background, and returns without waiting for it.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the start has been aborted.</param>
        /// <returns>A completed task.</returns>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            this.completion = Task.Run(this.WarmUpAsync, CancellationToken.None);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Does nothing: stopping the worker never waits for the warm-up.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the shutdown should no longer be graceful.</param>
        /// <returns>A completed task.</returns>
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private static void WarmUpBadges() => BadgePainter.WarmUp();

        private static void WarmUpYamlAndEvaluation()
        {
            // Small documents, so that this costs little more than the JIT compilation and the
            // construction of the shared YAML deserializer, which is what it is here to pay for.
            var ruleDefinitions = new RuleDefinitionRepository(IpMaturityModelRuleset.FromYaml(WarmupRuleSetYaml));
            IpMaturityModel imm = IpMaturityModel.FromYaml(WarmupImmYaml);
            ImmEvaluation evaluation = new EvaluationEngine(ruleDefinitions).Evaluate(imm);

            ImmBadges.RenderTotalBadge(evaluation);
            ImmBadges.RenderRuleBadge(evaluation.RuleEvaluations[0]);
            ImmBadges.RenderShowBadgesPage("org", "project", DefaultRuleSetBranch, DefaultRuleSetBranch, evaluation);
        }

        private static string Describe(Exception? failure)
        {
            return failure is null ? "ok" : $"failed: {failure.GetType().Name}: {failure.Message}";
        }

        private async Task WarmUpAsync()
        {
            try
            {
                // Make the warm-up a trace of its own. A default parent context on its own is not
                // enough, because StartActivity then uses Activity.Current as the parent, and
                // whatever was current when the worker started has flowed into this task. (This
                // only changes Activity.Current inside the warm-up.)
                Activity.Current = null;
                using Activity? activity = HostTelemetry.Source.StartActivity(WarmupActivityName, ActivityKind.Internal, parentContext: default);

                (long badgesMs, Exception? badgesFailure) = Measure(BadgesActivityName, WarmUpBadges);
                (long yamlMs, Exception? yamlFailure) = Measure(YamlActivityName, WarmUpYamlAndEvaluation);

                long startTimestamp = Stopwatch.GetTimestamp();
                Exception? ruleSetFailure = null;
                using (Activity? ruleSetActivity = HostTelemetry.Source.StartActivity(RuleSetActivityName, ActivityKind.Internal))
                {
                    try
                    {
                        await this.ruleDefinitionRepositorySource.GetRuleDefinitionRepositoryAsync(DefaultRuleSetBranch).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        ruleSetFailure = ex;
                        RecordFailure(ruleSetActivity, ex);
                    }
                }

                long ruleSetMs = (long)Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;

                bool anyFailed = badgesFailure is not null || yamlFailure is not null || ruleSetFailure is not null;
                if (activity is not null)
                {
                    activity.SetTag("imm.warmup.failed", anyFailed);
                    if (anyFailed)
                    {
                        activity.SetStatus(ActivityStatusCode.Error);
                    }
                }

                this.LogWarmupFinished(
                    anyFailed ? LogLevel.Warning : LogLevel.Information,
                    badgesMs,
                    Describe(badgesFailure),
                    yamlMs,
                    Describe(yamlFailure),
                    ruleSetMs,
                    Describe(ruleSetFailure));
            }
            catch (Exception)
            {
                // Only logging and tracing can get here. The warm-up is an optimization, so nothing
                // it does is allowed to fault the task (which would be an unobserved exception).
            }
        }

        private static (long ElapsedMs, Exception? Failure) Measure(string activityName, Action step)
        {
            using Activity? activity = HostTelemetry.Source.StartActivity(activityName, ActivityKind.Internal);
            long startTimestamp = Stopwatch.GetTimestamp();
            try
            {
                step();
                return ((long)Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds, null);
            }
            catch (Exception ex)
            {
                long elapsedMs = (long)Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
                RecordFailure(activity, ex);
                return (elapsedMs, ex);
            }
        }

        private static void RecordFailure(Activity? activity, Exception failure)
        {
            if (activity is not null)
            {
                activity.SetStatus(ActivityStatusCode.Error, failure.Message);
                activity.SetTag("error.type", failure.GetType().FullName);
            }
        }

        [LoggerMessage(EventId = 1, Message = "Warm-up: badges {BadgesMs} ms ({BadgesOutcome}), yaml/evaluate {YamlMs} ms ({YamlOutcome}), rule set {RuleSetMs} ms ({RuleSetOutcome})")]
        private partial void LogWarmupFinished(
            LogLevel level,
            long badgesMs,
            string badgesOutcome,
            long yamlMs,
            string yamlOutcome,
            long ruleSetMs,
            string ruleSetOutcome);
    }
}
