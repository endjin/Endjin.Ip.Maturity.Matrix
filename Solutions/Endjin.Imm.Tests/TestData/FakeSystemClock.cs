using Microsoft.Extensions.Internal;

namespace Endjin.Imm.Tests.TestData;

/// <summary>
/// A clock that only moves when a test advances it, so that expiration can be tested without
/// waiting. It drives both the memory cache (sliding expiration, through <see cref="ISystemClock"/>)
/// and the freshness of cached GitHub content (through <see cref="TimeProvider"/>).
/// </summary>
internal sealed class FakeSystemClock : TimeProvider, ISystemClock
{
    private long utcNowTicks = new DateTimeOffset(2019, 12, 3, 9, 0, 0, TimeSpan.Zero).UtcTicks;

    /// <inheritdoc/>
    public DateTimeOffset UtcNow => new(Interlocked.Read(ref this.utcNowTicks), TimeSpan.Zero);

    /// <inheritdoc/>
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    /// <inheritdoc/>
    public override DateTimeOffset GetUtcNow() => this.UtcNow;

    /// <inheritdoc/>
    public override long GetTimestamp() => Interlocked.Read(ref this.utcNowTicks);

    /// <summary>
    /// Moves the clock forwards.
    /// </summary>
    /// <param name="duration">How far to move it.</param>
    public void Advance(TimeSpan duration) => Interlocked.Add(ref this.utcNowTicks, duration.Ticks);
}
