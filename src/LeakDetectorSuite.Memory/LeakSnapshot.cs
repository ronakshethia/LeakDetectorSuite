namespace LeakDetector.Memory;

/// <summary>
/// Captures a snapshot of tracked objects at a point in time.
/// </summary>
public sealed class LeakSnapshot
{
    /// <summary>When this snapshot was taken.</summary>
    public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;

    /// <summary>Groups of tracked entries keyed by tag/type label.</summary>
    public IReadOnlyDictionary<string, int> Counts { get; }

    /// <summary>Total number of live objects at snapshot time.</summary>
    public int TotalAlive { get; }

    internal LeakSnapshot(IReadOnlyDictionary<string, int> counts)
    {
        Counts = counts;
        TotalAlive = counts.Values.Sum();
    }
}
