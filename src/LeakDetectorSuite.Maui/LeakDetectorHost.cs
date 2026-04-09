using LeakDetector.Memory;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using System.Timers;

namespace LeakDetector.Maui;

/// <summary>
/// MAUI integration host for <see cref="LeakTracker"/>.
/// Hooks into page lifecycle events to automatically track pages and view-models.
/// </summary>
internal sealed class LeakDetectorHost : IDisposable
{
    private readonly IApplication _application;
    private readonly System.Timers.Timer _logTimer;
    private bool _disposed;

    internal LeakDetectorHost(IApplication application)
    {
        _application = application ?? throw new ArgumentNullException(nameof(application));

        // Periodic logger: every 5 seconds output alive tracked objects.
        _logTimer = new System.Timers.Timer(5_000);
        _logTimer.Elapsed += OnTimerElapsed;
        _logTimer.AutoReset = true;
        _logTimer.Start();

        HookWindowPages();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Page hooking
    // ──────────────────────────────────────────────────────────────────────────

    private void HookWindowPages()
    {
        if (_application is Application mauiApp)
        {
            mauiApp.PageAppearing += OnPageAppearing;
        }
    }

    private void OnPageAppearing(object? sender, Page page)
    {
        if (page is null) return;

        // Track the page itself.
        var pageTag = page.GetType().Name;
        LeakTracker.Track(page, pageTag);

        // Track the binding context if present (typically a view-model).
        var vm = page.BindingContext;
        if (vm is not null)
        {
            var vmTag = vm.GetType().Name;
            LeakTracker.Track(vm, vmTag);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Periodic logging
    // ──────────────────────────────────────────────────────────────────────────

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        // WARN-03 fix: only log when there are actually live objects to report.
        var alive = LeakTracker.GetAliveObjects();
        if (alive.Count > 0)
            LeakTracker.LogAlive();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IDisposable
    // ──────────────────────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _logTimer.Stop();
        _logTimer.Dispose();

        if (_application is Application mauiApp)
            mauiApp.PageAppearing -= OnPageAppearing;
    }
}
