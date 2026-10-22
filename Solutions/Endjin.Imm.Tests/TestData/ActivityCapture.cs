using System.Collections.Concurrent;
using System.Diagnostics;

namespace Endjin.Imm.Tests.TestData;

/// <summary>
/// Records the spans a test produces, as an exporter would see them.
/// </summary>
/// <remarks>
/// <para>
/// Creating a capture starts a root <see cref="Activity"/> of the test's own, so everything the
/// test does afterwards (in the same async flow) belongs to a trace with a TraceId nobody else
/// uses. The listener sees the spans of every test running in parallel, so it keeps only those
/// in this trace. Disposing the capture stops the root and removes the listener.
/// </para>
/// </remarks>
internal sealed class ActivityCapture : IDisposable
{
    private readonly Activity root;
    private readonly ActivityListener listener;
    private readonly ConcurrentQueue<Activity> stopped = new();

    /// <summary>
    /// Starts capturing the spans from the given sources.
    /// </summary>
    /// <param name="sourceNames">The names of the <see cref="ActivitySource"/>s to listen to.</param>
    public ActivityCapture(params string[] sourceNames)
    {
        // Detach from any ambient span, so that the root starts a trace of its own.
        Activity.Current = null;
        this.root = new Activity("test").Start();
        ActivityTraceId traceId = this.root.TraceId;

        this.listener = new ActivityListener
        {
            ShouldListenTo = source => sourceNames.Contains(source.Name),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                if (activity.TraceId == traceId)
                {
                    this.stopped.Enqueue(activity);
                }
            },
        };
        ActivitySource.AddActivityListener(this.listener);
    }

    /// <summary>
    /// Gets the root span the captured spans descend from.
    /// </summary>
    public Activity Root => this.root;

    /// <summary>
    /// Gets the spans of this test's trace that have stopped, in the order they stopped.
    /// </summary>
    public IReadOnlyList<Activity> Activities => [.. this.stopped];

    /// <summary>
    /// Gets the stopped spans with the given name, in the order they stopped.
    /// </summary>
    public IReadOnlyList<Activity> Where(string operationName) =>
        [.. this.stopped.Where(activity => activity.OperationName == operationName)];

    /// <summary>
    /// Gets the only stopped span with the given name, failing if there is not exactly one.
    /// </summary>
    public Activity Single(string operationName) =>
        this.stopped.Single(activity => activity.OperationName == operationName);

    /// <inheritdoc/>
    public void Dispose()
    {
        this.listener.Dispose();
        this.root.Stop();
    }
}
