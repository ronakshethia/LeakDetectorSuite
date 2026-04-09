using LeakDetector.Demo.Services;
using LeakDetector.Memory;

namespace LeakDetector.Demo;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();
    }

    private async void OnDiagnostics(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(DiagnosticsPage));

    private async void OnScenario1(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(Scenario1CleanNavPage));

    private async void OnScenario2(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(Scenario2LeakyPage));

    private async void OnScenario3(object? sender, EventArgs e)
    {
        LeakTracker.ForceGC();
        DemoStateService.Instance.HomeSnapshot = LeakTracker.Snapshot();
        DiagnosticsService.Instance.Log("[Home] Auto-snapshot before S3 navigation.");
        await Shell.Current.GoToAsync(nameof(Scenario3MultiNavPage));
    }

    private async void OnScenario4(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(Scenario4PerfPage));

    private async void OnScenario5(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(Scenario5ThreadingPage));

    private async void OnScenario6(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(Scenario6SnapshotPage));

    private void OnForceGc(object? sender, EventArgs e)
    {
        LeakTracker.ForceGC();
        DiagnosticsService.Instance.Log("[Home] Force GC complete.");
    }

    private void OnSnapshot(object? sender, EventArgs e)
    {
        LeakTracker.ForceGC();
        var snapshot = LeakTracker.Snapshot();
        DemoStateService.Instance.HomeSnapshot = snapshot;
        DiagnosticsService.Instance.Log($"[Home] Snapshot captured with {snapshot.TotalAlive} live tracked objects.");
    }

    private void OnReset(object? sender, EventArgs e)
    {
        LeakTracker.Reset();
        Scenario3MultiNavPage.ResetCounter();
        DemoStateService.Instance.HomeSnapshot = null;
        DiagnosticsService.Instance.Clear();
        DiagnosticsService.Instance.Log("[Home] Tracker reset. Scenario counters and baselines cleared.");
    }
}
