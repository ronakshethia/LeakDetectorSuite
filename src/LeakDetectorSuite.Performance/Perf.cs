using System.Diagnostics;

namespace LeakDetector.Performance;

/// <summary>
/// Lightweight execution profiler that measures how long actions take and warns
/// when they exceed an acceptable threshold.
/// </summary>
public static class Perf
{
    /// <summary>
    /// Threshold in milliseconds above which a warning is emitted. Defaults to 100 ms.
    /// </summary>
    public static double WarnThresholdMs { get; set; } = 100.0;

    /// <summary>
    /// Optional logging hook. Defaults to <see cref="Debug.WriteLine"/>.
    /// </summary>
    public static Action<string> Logger { get; set; } =
        msg => Debug.WriteLine(msg);

    // ──────────────────────────────────────────────────────────────────────────
    // Synchronous overloads
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Measures the execution time of <paramref name="action"/> and logs the result.
    /// </summary>
    /// <param name="action">The code to benchmark.</param>
    /// <param name="name">A human-readable label for the measurement.</param>
    public static void Measure(Action action, string name = "Operation")
    {
        ArgumentNullException.ThrowIfNull(action);

        var sw = Stopwatch.StartNew();
        try
        {
            action();
        }
        finally
        {
            sw.Stop();
            LogResult(name, sw.Elapsed.TotalMilliseconds);
        }
    }

    /// <summary>
    /// Measures the execution time of <paramref name="func"/> and returns its result.
    /// </summary>
    /// <typeparam name="T">Return type of the function.</typeparam>
    /// <param name="func">The code to benchmark.</param>
    /// <param name="name">A human-readable label for the measurement.</param>
    /// <returns>Whatever <paramref name="func"/> returns.</returns>
    public static T Measure<T>(Func<T> func, string name = "Operation")
    {
        ArgumentNullException.ThrowIfNull(func);

        var sw = Stopwatch.StartNew();
        try
        {
            return func();
        }
        finally
        {
            sw.Stop();
            LogResult(name, sw.Elapsed.TotalMilliseconds);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Async overloads
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Measures the execution time of an asynchronous <paramref name="asyncAction"/>.
    /// </summary>
    public static async Task MeasureAsync(Func<Task> asyncAction, string name = "Operation")
    {
        ArgumentNullException.ThrowIfNull(asyncAction);

        var sw = Stopwatch.StartNew();
        try
        {
            await asyncAction().ConfigureAwait(false);
        }
        finally
        {
            sw.Stop();
            LogResult(name, sw.Elapsed.TotalMilliseconds);
        }
    }

    /// <summary>
    /// Measures the execution time of an asynchronous function and returns its result.
    /// </summary>
    public static async Task<T> MeasureAsync<T>(Func<Task<T>> asyncFunc, string name = "Operation")
    {
        ArgumentNullException.ThrowIfNull(asyncFunc);

        var sw = Stopwatch.StartNew();
        try
        {
            return await asyncFunc().ConfigureAwait(false);
        }
        finally
        {
            sw.Stop();
            LogResult(name, sw.Elapsed.TotalMilliseconds);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helper
    // ──────────────────────────────────────────────────────────────────────────

    private static void LogResult(string name, double elapsedMs)
    {
        var isWarning = elapsedMs > WarnThresholdMs;
        var prefix = isWarning ? "⚠️  [Perf WARNING]" : "[Perf]";
        Logger?.Invoke($"{prefix} {name}: {elapsedMs:F2} ms" +
                       (isWarning ? $" (threshold={WarnThresholdMs:F0} ms)" : string.Empty));
    }
}
