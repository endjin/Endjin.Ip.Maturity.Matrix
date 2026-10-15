using Microsoft.Extensions.Internal;

namespace Endjin.Imm.Tests.TestData;

/// <summary>
/// A clock for the memory cache that only moves when a test advances it, so that sliding
/// expiration can be tested without waiting.
/// </summary>
internal sealed class FakeSystemClock : ISystemClock
{
    private long utcNowTicks = new DateTimeOffset(2019, 12, 3, 9, 0, 0, TimeSpan.Zero).UtcTicks;

    /// <inheritdoc/>
    public DateTimeOffset UtcNow => new(Interlocked.Read(ref this.utcNowTicks), TimeSpan.Zero);

    /// <summary>
    /// Moves the clock forwards.
    /// </summary>
    /// <param name="duration">How far to move it.</param>
    public void Advance(TimeSpan duration) => Interlocked.Add(ref this.utcNowTicks, duration.Ticks);
}
