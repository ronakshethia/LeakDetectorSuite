using System.Collections.Concurrent;

namespace LeakDetector.Memory;

/// <summary>
/// Thread-safe tracker for detecting memory leaks using weak references.
/// All tracked references are held weakly so the GC can still collect them.
/// </summary>
public static class LeakTracker
{
    // Each entry: (tag, WeakReference)
    private static readonly ConcurrentDictionary<long, (string Tag, WeakReference Reference)> _tracked
        = new();

    private static long _idCounter = 0;

    /// <summary>
    /// Optional logging hook. Set to receive diagnostic messages.
    /// Defaults to writing to <see cref="System.Diagnostics.Debug"/>.
    /// </summary>
    public static Action<string> Logger { get; set; } =
        msg => System.Diagnostics.Debug.WriteLine(msg);

    // ──────────────────────────────────────────────────────────────────────────
    // Public API
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Begins tracking <paramref name="obj"/> under an optional <paramref name="tag"/>.
    /// If no tag is provided the object's type name is used.
    /// </summary>
    public static void Track(object obj, string? tag = null)
    {
        if (obj is null) return;

        var resolvedTag = string.IsNullOrWhiteSpace(tag)
            ? obj.GetType().Name
            : tag!;

        var id = Interlocked.Increment(ref _idCounter);
        _tracked[id] = (resolvedTag, new WeakReference(obj));

        Logger?.Invoke($"[LeakDetector] Tracking: {resolvedTag}");
    }

    /// <summary>
    /// Captures the current state of all tracked (still-alive) objects.
    /// </summary>
    public static LeakSnapshot Snapshot()
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var (_, (tag, weakRef)) in _tracked)
        {
            // BUG-01 fix: single read — avoids TOCTOU race with the GC.
            // .Target returns null atomically if already collected.
            var target = weakRef.Target;
            if (target is not null)
                counts[tag] = counts.GetValueOrDefault(tag, 0) + 1;
        }

        // Purge dead entries AFTER counting, not before (BUG-06 fix).
        // This keeps Snapshot() observational and side-effect-free during the count.
        Purge();

        return new LeakSnapshot(counts);
    }

    /// <summary>
    /// Compares two snapshots and returns a diff describing what grew.
    /// </summary>
    public static SnapshotDiff Compare(LeakSnapshot before, LeakSnapshot after)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);

        var allKeys = before.Counts.Keys.Union(after.Counts.Keys);
        var deltas = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var key in allKeys)
        {
            var b = before.Counts.GetValueOrDefault(key, 0);
            var a = after.Counts.GetValueOrDefault(key, 0);
            var delta = a - b;
            if (delta != 0)
                deltas[key] = delta;
        }

        return new SnapshotDiff(before, after, deltas);
    }

    /// <summary>
    /// Forces a full garbage collection and waits for pending finalizers.
    /// Useful to ensure genuinely alive objects are still referenced.
    /// </summary>
    public static void ForceGC()
    {
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
        Logger?.Invoke("[LeakDetector] GC forced.");
    }

    /// <summary>
    /// Returns all objects that are still alive (have not been collected).
    /// </summary>
    public static IReadOnlyList<(string Tag, object Target)> GetAliveObjects()
    {
        var results = new List<(string Tag, object Target)>();

        foreach (var (_, (tag, weakRef)) in _tracked)
        {
            var target = weakRef.Target;
            if (target is not null)
                results.Add((tag, target));
        }

        return results.AsReadOnly();
    }

    /// <summary>
    /// Resets all tracking state. Useful between test runs.
    /// </summary>
    /// <remarks>
    /// Not safe to call concurrently with <see cref="Track"/>.
    /// Intended for use between isolated test runs.
    /// </remarks>
    public static void Reset()
    {
        _tracked.Clear();
        Interlocked.Exchange(ref _idCounter, 0); // BUG-02 fix: atomic reset
        Logger?.Invoke("[LeakDetector] Tracker reset.");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Internal helpers
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>Removes dead weak references to keep the dictionary compact.</summary>
    private static void Purge()
    {
        foreach (var key in _tracked.Keys)
        {
            if (_tracked.TryGetValue(key, out var entry) && !entry.Reference.IsAlive)
                _tracked.TryRemove(key, out _);
        }
    }

    /// <summary>
    /// Logs a formatted summary of all currently alive tracked objects.
    /// </summary>
    public static void LogAlive()
    {
        var alive = GetAliveObjects();
        if (alive.Count == 0)
        {
            Logger?.Invoke("[LeakDetector] No live tracked objects.");
            return;
        }

        var grouped = alive
            .GroupBy(x => x.Tag)
            .OrderByDescending(g => g.Count());

        Logger?.Invoke("[LeakDetector] ── Live Objects ──────────────────────");
        foreach (var group in grouped)
            Logger?.Invoke($"[LeakDetector]   {group.Key}: {group.Count()}");
        Logger?.Invoke("[LeakDetector] ──────────────────────────────────────");
    }
}
