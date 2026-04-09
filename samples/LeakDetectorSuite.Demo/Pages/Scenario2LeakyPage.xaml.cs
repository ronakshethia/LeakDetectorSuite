using LeakDetector.Demo.Services;
using LeakDetector.Memory;

namespace LeakDetector.Demo;

public partial class Scenario2LeakyPage : ContentPage
{
#pragma warning disable CS0067
    private static event EventHandler? _leakyEvent;
#pragma warning restore CS0067

    private LeakSnapshot? _before;

    private sealed class Scenario2LeakyViewModel;

    public Scenario2LeakyPage()
    {
        BindingContext = new Scenario2LeakyViewModel();
        InitializeComponent();

        _leakyEvent += OnLeakyEvent;
        DiagnosticsService.Instance.Log("[S2] Page constructed and subscribed to static event with no cleanup.");
    }

    private void OnLeakyEvent(object? sender, EventArgs e)
    {
    }

    private void OnTakeSnapshot(object? sender, EventArgs e)
    {
        LeakTracker.ForceGC();
        _before = LeakTracker.Snapshot();
        DiagnosticsService.Instance.Log($"[S2] Before snapshot taken. {_before.TotalAlive} live objects.");
    }

    private async void OnGoBack(object? sender, EventArgs e)
    {
        DiagnosticsService.Instance.Log("[S2] Navigating back without unsubscribing.");
        await Shell.Current.GoToAsync("..");

        LeakTracker.ForceGC();
        if (_before is null)
            return;

        var after = LeakTracker.Snapshot();
        var diff = LeakTracker.Compare(_before, after);
        DiagnosticsService.Instance.Log(diff.ToString());
        DiagnosticsService.Instance.Log(diff.HasLeaks
            ? "[S2] LEAK DETECTED. Page still alive after GC."
            : "[S2] No leak detected, which is unexpected for this scenario.");
    }
}
