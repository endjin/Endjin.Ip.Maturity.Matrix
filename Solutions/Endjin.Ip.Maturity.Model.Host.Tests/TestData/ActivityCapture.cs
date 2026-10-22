using System.Collections.Concurrent;
using System.Diagnostics;

namespace Endjin.Ip.Maturity.Model.Host.Tests.TestData;

/// <summary>
/// Records the spans (activities) that the code under test creates, the way an OpenTelemetry
/// exporter would see them.
/// </summary>
/// <remarks>
/// <para>
/// Creating one starts a root activity, so that everything the test does on its own execution
/// context belongs to a trace of its own. The listener sees the spans of every test running in
/// parallel, so <see cref="Activities"/> keeps only those in this test's trace. A span that
/// deliberately starts a new trace (such as the warm-up's) can be found with
/// <see cref="InTrace(ActivityTraceId)"/>, given an id the test captured from inside it.
/// </para>
/// <para>
/// Disposing it stops the root activity and the listener.
/// </para>
/// </remarks>
internal sealed class ActivityCapture : IDisposable
{
    private readonly ConcurrentQueue<Activity> stopped = new();
    private readonly ActivityListener listener;
    private readonly Activity root;

    /// <summary>
    /// Starts recording the spans from the named sources.
    /// </summary>
    /// <param name="sourceNames">The names of the <see cref="ActivitySource"/>s to listen to.</param>
    public ActivityCapture(params string[] sourceNames)
    {
        HashSet<string> names = new(sourceNames, StringComparer.Ordinal);
        this.listener = new ActivityListener
        {
            ShouldListenTo = source => names.Contains(source.Name),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => this.stopped.Enqueue(activity),
        };
        ActivitySource.AddActivityListener(this.listener);

        this.root = new Activity("test").Start();
    }

    /// <summary>
    /// Gets the root activity that the test's spans descend from.
    /// </summary>
    public Activity Root => this.root;

    /// <summary>
    /// Gets the spans in this test's trace that have stopped, in the order they stopped.
    /// </summary>
    public IReadOnlyList<Activity> Activities => this.InTrace(this.root.TraceId);

    /// <summary>
    /// Gets the spans in the given trace that have stopped, in the order they stopped.
    /// </summary>
    /// <param name="traceId">The trace.</param>
    public IReadOnlyList<Activity> InTrace(ActivityTraceId traceId) =>
        this.stopped.Where(a => a.TraceId == traceId).ToList();

    /// <summary>
    /// Gets the spans in this test's trace with the given name.
    /// </summary>
    /// <param name="name">The span name (operation name).</param>
    public IReadOnlyList<Activity> Where(string name) =>
        this.Activities.Where(a => a.OperationName == name).ToList();

    /// <summary>
    /// Gets the one span in this test's trace with the given name, failing the test if there is
    /// not exactly one.
    /// </summary>
    /// <param name="name">The span name (operation name).</param>
    public Activity Single(string name)
    {
        IReadOnlyList<Activity> matches = this.Where(name);
        if (matches.Count != 1)
        {
            Assert.Fail($"Expected one '{name}' span, but found {matches.Count}. Spans: {string.Join(", ", this.Activities.Select(a => a.OperationName))}");
        }

        return matches[0];
    }

    public void Dispose()
    {
        this.root.Stop();
        this.listener.Dispose();
    }
}
