using System.Collections.Concurrent;
using System.Diagnostics;
using Endjin.Imm.Contracts;
using Endjin.Imm.Domain;
using Endjin.Imm.Repository;
using Endjin.Ip.Maturity.Model.Host.Tests.TestData;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace Endjin.Ip.Maturity.Model.Host.Tests;

/// <summary>
/// When the worker starts, the Host warms up in the background: it renders badges, loads and
/// evaluates a small IMM, and fetches the default rule set, so that the first request after a cold
/// start does not pay for that work. The warm-up never delays starting or stopping the worker,
/// and a failure in it is logged rather than thrown. When telemetry is exported, the warm-up is
/// traced as an imm.warmup span in a trace of its own, with a child span for each step; a step
/// that fails marks its span, and the warm-up's, as an error.
/// </summary>
[TestClass]
public class StartupWarmupTests
{
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(30);

    [TestMethod]
    public async Task StartAsync_RuleSetFetchStillPending_ReturnsWithoutWaitingAndFetchesTheDefaultRuleSetOnce()
    {
        // Arrange
        TaskCompletionSource<IRuleDefinitionRepository> fetch = new(TaskCreationOptions.RunContinuationsAsynchronously);
        IRuleDefinitionRepositorySource source = Substitute.For<IRuleDefinitionRepositorySource>();
        source.GetRuleDefinitionRepositoryAsync(Arg.Any<string>()).Returns(fetch.Task);
        CapturingLogger logger = new();
        StartupWarmup warmup = new(source, logger);

        // Act
        Task start = warmup.StartAsync(CancellationToken.None);
        await WaitUntilAsync(() => source.ReceivedCalls().Any());
        Task stop = warmup.StopAsync(CancellationToken.None);

        // Assert
        start.IsCompletedSuccessfully.ShouldBeTrue();
        stop.IsCompletedSuccessfully.ShouldBeTrue();
        warmup.Completion.IsCompleted.ShouldBeFalse();
        logger.Entries.ShouldBeEmpty();

        fetch.SetResult(new RuleDefinitionRepository(IpMaturityModelRuleset.FromYaml(HostYaml.TwoRuleSet)));
        await warmup.Completion.WaitAsync(WaitLimit);
        await source.Received(1).GetRuleDefinitionRepositoryAsync("main");
        source.ReceivedCalls().Count().ShouldBe(1);
    }

    [TestMethod]
    [DataRow(false, DisplayName = "Faulted task")]
    [DataRow(true, DisplayName = "Synchronous throw")]
    public async Task StartAsync_RuleSetFetchFails_LogsTheFailureWithoutFaulting(bool throwSynchronously)
    {
        // Arrange
        IRuleDefinitionRepositorySource source = Substitute.For<IRuleDefinitionRepositorySource>();
        if (throwSynchronously)
        {
            source.GetRuleDefinitionRepositoryAsync(Arg.Any<string>())
                .Returns<Task<IRuleDefinitionRepository>>(_ => throw new HttpRequestException("GitHub is unavailable"));
        }
        else
        {
            source.GetRuleDefinitionRepositoryAsync(Arg.Any<string>())
                .Returns(Task.FromException<IRuleDefinitionRepository>(new HttpRequestException("GitHub is unavailable")));
        }

        CapturingLogger logger = new();
        StartupWarmup warmup = new(source, logger);

        // Act
        Task start = warmup.StartAsync(CancellationToken.None);
        await warmup.Completion.WaitAsync(WaitLimit);
        Task stop = warmup.StopAsync(CancellationToken.None);

        // Assert
        start.IsCompletedSuccessfully.ShouldBeTrue();
        stop.IsCompletedSuccessfully.ShouldBeTrue();
        warmup.Completion.IsCompletedSuccessfully.ShouldBeTrue();
        (LogLevel level, string message) = logger.Entries.ShouldHaveSingleItem();
        level.ShouldBe(LogLevel.Warning);
        message.ShouldStartWith("Warm-up: ");
        message.ShouldContain("rule set");
        message.ShouldContain("(failed: HttpRequestException: GitHub is unavailable)");
    }

    [TestMethod]
    public async Task StartAsync_RealDomainTypes_RendersBadgesAndEvaluatesYamlWithoutFailures()
    {
        // Arrange
        IRuleDefinitionRepositorySource source = Substitute.For<IRuleDefinitionRepositorySource>();
        source.GetRuleDefinitionRepositoryAsync(Arg.Any<string>())
            .Returns(_ => new RuleDefinitionRepository(IpMaturityModelRuleset.FromYaml(HostYaml.TwoRuleSet)));
        CapturingLogger logger = new();
        StartupWarmup warmup = new(source, logger);

        // Act
        await warmup.StartAsync(CancellationToken.None);
        await warmup.Completion.WaitAsync(WaitLimit);

        // Assert
        (LogLevel level, string message) = logger.Entries.ShouldHaveSingleItem();
        level.ShouldBe(LogLevel.Information);
        message.ShouldMatch(@"^Warm-up: badges \d+ ms \(ok\), yaml/evaluate \d+ ms \(ok\), rule set \d+ ms \(ok\)$");
    }

    [TestMethod]
    public async Task StartAsync_RealDomainTypes_TracesTheWarmupAsATraceOfItsOwnWithASpanPerStep()
    {
        // Arrange
        using ActivityCapture capture = new(HostTelemetry.ActivitySourceName);
        Activity? currentDuringFetch = null;
        IRuleDefinitionRepositorySource source = Substitute.For<IRuleDefinitionRepositorySource>();
        source.GetRuleDefinitionRepositoryAsync(Arg.Any<string>())
            .Returns(_ =>
            {
                currentDuringFetch = Activity.Current;
                return new RuleDefinitionRepository(IpMaturityModelRuleset.FromYaml(HostYaml.TwoRuleSet));
            });
        StartupWarmup warmup = new(source, new CapturingLogger());

        // Act
        await warmup.StartAsync(CancellationToken.None);
        await warmup.Completion.WaitAsync(WaitLimit);

        // Assert
        currentDuringFetch.ShouldNotBeNull();
        IReadOnlyList<Activity> spans = capture.InTrace(currentDuringFetch.TraceId);
        Activity warmupSpan = spans.Where(a => a.OperationName == "imm.warmup").ShouldHaveSingleItem();
        warmupSpan.TraceId.ShouldNotBe(capture.Root.TraceId);
        warmupSpan.ParentSpanId.ShouldBe(default);
        warmupSpan.Status.ShouldBe(ActivityStatusCode.Unset);
        warmupSpan.GetTagItem("imm.warmup.failed").ShouldBe(false);

        string[] stepNames = ["imm.warmup.badges", "imm.warmup.yaml", "imm.warmup.ruleset"];
        foreach (string stepName in stepNames)
        {
            Activity step = spans.Where(a => a.OperationName == stepName).ShouldHaveSingleItem();
            step.ParentSpanId.ShouldBe(warmupSpan.SpanId);
            step.Status.ShouldBe(ActivityStatusCode.Unset);
        }

        currentDuringFetch.OperationName.ShouldBe("imm.warmup.ruleset");
    }

    [TestMethod]
    public async Task StartAsync_RuleSetFetchFails_MarksTheRuleSetStepAndTheWarmupAsErrorsAndLogsTheSameLine()
    {
        // Arrange
        using ActivityCapture capture = new(HostTelemetry.ActivitySourceName);
        ActivityTraceId? warmupTraceId = null;
        IRuleDefinitionRepositorySource source = Substitute.For<IRuleDefinitionRepositorySource>();
        source.GetRuleDefinitionRepositoryAsync(Arg.Any<string>())
            .Returns(_ =>
            {
                warmupTraceId = Activity.Current?.TraceId;
                return Task.FromException<IRuleDefinitionRepository>(new HttpRequestException("GitHub is unavailable"));
            });
        CapturingLogger logger = new();
        StartupWarmup warmup = new(source, logger);

        // Act
        await warmup.StartAsync(CancellationToken.None);
        await warmup.Completion.WaitAsync(WaitLimit);

        // Assert
        warmup.Completion.IsCompletedSuccessfully.ShouldBeTrue();
        warmupTraceId.ShouldNotBeNull();
        IReadOnlyList<Activity> spans = capture.InTrace(warmupTraceId.Value);

        Activity ruleSetSpan = spans.Where(a => a.OperationName == "imm.warmup.ruleset").ShouldHaveSingleItem();
        ruleSetSpan.Status.ShouldBe(ActivityStatusCode.Error);
        ruleSetSpan.GetTagItem("error.type").ShouldBe(typeof(HttpRequestException).FullName);

        Activity warmupSpan = spans.Where(a => a.OperationName == "imm.warmup").ShouldHaveSingleItem();
        warmupSpan.Status.ShouldBe(ActivityStatusCode.Error);
        warmupSpan.GetTagItem("imm.warmup.failed").ShouldBe(true);

        spans.Where(a => a.OperationName == "imm.warmup.badges").ShouldHaveSingleItem().Status.ShouldBe(ActivityStatusCode.Unset);
        spans.Where(a => a.OperationName == "imm.warmup.yaml").ShouldHaveSingleItem().Status.ShouldBe(ActivityStatusCode.Unset);

        (LogLevel level, string message) = logger.Entries.ShouldHaveSingleItem();
        level.ShouldBe(LogLevel.Warning);
        message.ShouldMatch(@"^Warm-up: badges \d+ ms \(ok\), yaml/evaluate \d+ ms \(ok\), rule set \d+ ms \(failed: HttpRequestException: GitHub is unavailable\)$");
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        DateTime deadline = DateTime.UtcNow + WaitLimit;
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                Assert.Fail("Timed out waiting for the warm-up to fetch the rule set.");
            }

            await Task.Delay(10);
        }
    }

    private sealed class CapturingLogger : ILogger<StartupWarmup>
    {
        public ConcurrentQueue<(LogLevel Level, string Message)> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            this.Entries.Enqueue((logLevel, formatter(state, exception)));
        }
    }
}
