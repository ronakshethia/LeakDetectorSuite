using LeakDetector.Demo.Services;
using LeakDetector.Memory;

namespace LeakDetector.Demo;

public partial class Scenario1CleanNavPage : ContentPage
{
#pragma warning disable CS0067
    private static event EventHandler? _cleanEvent;
#pragma warning restore CS0067

    private LeakSnapshot? _before;

    private sealed class Scenario1CleanNavViewModel;

    public Scenario1CleanNavPage()
    {
        BindingContext = new Scenario1CleanNavViewModel();
        InitializeComponent();

        _cleanEvent += OnCleanEvent;
        DiagnosticsService.Instance.Log("[S1] Page constructed and subscribed to static event.");
    }

    protected override void OnDisappearing()
    {
        _cleanEvent -= OnCleanEvent;
        DiagnosticsService.Instance.Log("[S1] OnDisappearing: unsubscribed from static event.");
        base.OnDisappearing();
    }

    private void OnCleanEvent(object? sender, EventArgs e)
    {
    }

    private void OnTakeSnapshot(object? sender, EventArgs e)
    {
        LeakTracker.ForceGC();
        _before = LeakTracker.Snapshot();
        DiagnosticsService.Instance.Log($"[S1] Snapshot captured with {_before.TotalAlive} live objects.");
    }

    private async void OnGoBack(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");

        LeakTracker.ForceGC();
        if (_before is null)
            return;

        var after = LeakTracker.Snapshot();
        var diff = LeakTracker.Compare(_before, after);
        DiagnosticsService.Instance.Log(diff.ToString());
        DiagnosticsService.Instance.Log(diff.HasLeaks
            ? "[S1] Unexpected leak result."
            : "[S1] No leak. Page was properly collected.");
    }
}
