using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Endjin.Ip.Maturity.Model.Host.Tests.TestData;

/// <summary>
/// A logger that records what is logged, so that tests can check it.
/// </summary>
/// <typeparam name="T">The category.</typeparam>
internal sealed class CapturingLogger<T> : ILogger<T>
{
    public ConcurrentQueue<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = new();

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        this.Entries.Enqueue((logLevel, formatter(state, exception), exception));
    }
}
