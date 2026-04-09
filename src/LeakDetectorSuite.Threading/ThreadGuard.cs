using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LeakDetector.Threading;

/// <summary>
/// Guards against common threading violations in MAUI applications and other apps
/// that opt in by providing a main-thread detector.
/// </summary>
public static class ThreadGuard
{
    /// <summary>
    /// Optional logging hook. Defaults to <see cref="Debug.WriteLine"/>.
    /// </summary>
    public static Action<string> Logger { get; set; } =
        msg => Debug.WriteLine(msg);

    /// <summary>
    /// If true, <see cref="EnsureMainThread"/> and <see cref="WarnIfBlockingCall"/>
    /// throw exceptions instead of just logging. Defaults to false.
    /// </summary>
    public static bool ThrowOnViolation { get; set; } = false;

    /// <summary>
    /// Optional detector used on non-MAUI targets to identify the application's UI thread.
    /// For example, WPF apps can set this to <c>() => Application.Current.Dispatcher.CheckAccess()</c>.
    /// </summary>
    public static Func<bool>? MainThreadDetector { get; set; }

    // ──────────────────────────────────────────────────────────────────────────
    // Main Thread Guard
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Asserts that the current code is running on the main (UI) thread.
    /// Logs a warning (or throws) if called from a background thread.
    /// On non-MAUI targets, configure <see cref="MainThreadDetector"/> first.
    /// </summary>
    /// <param name="callerMemberName">Auto-filled by the compiler.</param>
    /// <param name="callerFilePath">Auto-filled by the compiler.</param>
    /// <param name="callerLineNumber">Auto-filled by the compiler.</param>
    public static void EnsureMainThread(
        [CallerMemberName] string callerMemberName = "",
        [CallerFilePath]   string callerFilePath   = "",
        [CallerLineNumber] int    callerLineNumber  = 0)
    {
        if (!IsMainThreadDetectionAvailable)
        {
            Logger?.Invoke("[ThreadGuard] Main-thread detection is unavailable on this target. Configure ThreadGuard.MainThreadDetector to enable UI-thread checks.");
            return;
        }

        if (IsMainThread()) return;

        var threadId  = Environment.CurrentManagedThreadId;
        var shortFile = Path.GetFileName(callerFilePath);
        var message   =
            $"[ThreadGuard] ⚠️  NOT on main thread! " +
            $"Called from '{callerMemberName}' in {shortFile}:{callerLineNumber} " +
            $"(Thread #{threadId})";

        Logger?.Invoke(message);

        if (ThrowOnViolation)
            throw new InvalidOperationException(message);
    }

    /// <summary>
    /// Checks whether the calling code appears to be making a blocking call
    /// (i.e., synchronously waiting on an async operation) and warns if so.
    /// </summary>
    /// <remarks>
    /// Because detecting .Wait()/.Result at runtime without stack analysis is
    /// non-trivial, this method uses a heuristic. It only runs when
    /// <see cref="IsMainThreadDetectionAvailable"/> is <c>true</c>.
    /// </remarks>
    /// <param name="callerMemberName">Auto-filled by the compiler.</param>
    /// <param name="callerFilePath">Auto-filled by the compiler.</param>
    /// <param name="callerLineNumber">Auto-filled by the compiler.</param>
    public static void WarnIfBlockingCall(
        [CallerMemberName] string callerMemberName = "",
        [CallerFilePath]   string callerFilePath   = "",
        [CallerLineNumber] int    callerLineNumber  = 0)
    {
        if (!IsMainThreadDetectionAvailable)
            return;

        if (SynchronizationContext.Current is not null && !IsMainThread())
        {
            var threadId  = Environment.CurrentManagedThreadId;
            var shortFile = Path.GetFileName(callerFilePath);
            var message   =
                $"[ThreadGuard] ⚠️  Possible blocking call detected! " +
                $"A SynchronizationContext is active but code is running on Thread #{threadId}. " +
                $"Avoid .Wait() / .Result in '{callerMemberName}' ({shortFile}:{callerLineNumber}).";

            Logger?.Invoke(message);

            if (ThrowOnViolation)
                throw new InvalidOperationException(message);
        }
    }

    /// <summary>
    /// Returns <c>true</c> if the current thread is the main (UI) thread.
    /// On non-MAUI targets this returns a meaningful value only when
    /// <see cref="MainThreadDetector"/> has been configured.
    /// </summary>
    public static bool IsMainThread()
    {
        if (MainThreadDetector is not null)
            return MainThreadDetector();

#if ANDROID || IOS || MACCATALYST
        // MAUI platform checks
        return Microsoft.Maui.ApplicationModel.MainThread.IsMainThread;
#else
        return false;
#endif
    }

    /// <summary>
    /// Gets whether <see cref="ThreadGuard"/> has a reliable way to identify the UI thread
    /// on the current target.
    /// </summary>
    public static bool IsMainThreadDetectionAvailable
    {
        get
        {
            if (MainThreadDetector is not null)
                return true;

#if ANDROID || IOS || MACCATALYST
            return true;
#else
            return false;
#endif
        }
    }

    /// <summary>
    /// Runs <paramref name="action"/> and verifies it completes on the main thread.
    /// Useful as a decorator for UI-bound callbacks.
    /// </summary>
    public static void RunOnMainThread(Action action, [CallerMemberName] string name = "")
    {
        ArgumentNullException.ThrowIfNull(action);
        EnsureMainThread(name);
        action();
    }
}
