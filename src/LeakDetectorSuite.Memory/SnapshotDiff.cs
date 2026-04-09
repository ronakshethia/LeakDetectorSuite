namespace LeakDetector.Memory;

/// <summary>
/// Represents the diff between two <see cref="LeakSnapshot"/> instances.
/// </summary>
public sealed class SnapshotDiff
{
    /// <summary>The earlier snapshot used as a baseline.</summary>
    public LeakSnapshot Before { get; }

    /// <summary>The later snapshot.</summary>
    public LeakSnapshot After { get; }

    /// <summary>
    /// Keys that gained objects between snapshots. Positive values indicate growth.
    /// </summary>
    public IReadOnlyDictionary<string, int> Deltas { get; }

    /// <summary>Net change in total live objects (can be negative if objects were freed).</summary>
    public int TotalDelta => After.TotalAlive - Before.TotalAlive;

    /// <summary>
    /// True when ANY individual tracked type has more live instances after than before.
    /// Uses per-tag growth rather than net total to avoid false negatives when
    /// growth in one type is masked by shrinkage in another. (BUG-07 fix)
    /// </summary>
    public bool HasLeaks => Deltas.Any(kv => kv.Value > 0);

    internal SnapshotDiff(
        LeakSnapshot before,
        LeakSnapshot after,
        IReadOnlyDictionary<string, int> deltas)
    {
        Before = before;
        After = after;
        Deltas = deltas;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        if (Deltas.Count == 0)
            return "[LeakDetector] No changes detected.";

        var lines = new System.Text.StringBuilder();
        lines.AppendLine($"[LeakDetector] Snapshot diff (Δ={TotalDelta:+#;-#;0}):");
        foreach (var (key, delta) in Deltas.OrderByDescending(kv => kv.Value))
        {
            lines.AppendLine($"  [{key}] Δ={delta:+#;-#;0}  (after={After.Counts.GetValueOrDefault(key, 0)})");
        }
        return lines.ToString().TrimEnd();
    }
}
