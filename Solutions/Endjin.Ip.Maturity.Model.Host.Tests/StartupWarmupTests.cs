using System.Collections.Concurrent;
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
/// and a failure in it is logged rather than thrown.
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

        fetch.SetResult(new RuleDefinitionRepository(IpMaturityMatrixRuleset.FromYaml(HostYaml.TwoRuleSet)));
        await warmup.Completion.WaitAsync(WaitLimit);
        await source.Received(1).GetRuleDefinitionRepositoryAsync("master");
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
            .Returns(_ => new RuleDefinitionRepository(IpMaturityMatrixRuleset.FromYaml(HostYaml.TwoRuleSet)));
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
