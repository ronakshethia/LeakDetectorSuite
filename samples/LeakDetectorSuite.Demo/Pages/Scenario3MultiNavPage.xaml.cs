using LeakDetector.Demo.Services;
using LeakDetector.Memory;

namespace LeakDetector.Demo;

public partial class Scenario3MultiNavPage : ContentPage
{
#pragma warning disable CS0067
    private static event EventHandler? _growingLeakEvent;
#pragma warning restore CS0067

    private static int _totalInstancesCreated;
    private readonly int _myInstance;

    private sealed class Scenario3MultiNavViewModel;

    public Scenario3MultiNavPage()
    {
        BindingContext = new Scenario3MultiNavViewModel();
        InitializeComponent();

        _myInstance = ++_totalInstancesCreated;
        InstanceLabel.Text = $"Instance #{_myInstance}";
        _growingLeakEvent += OnGrowingLeak;

        DiagnosticsService.Instance.Log($"[S3] Instance #{_myInstance} created and subscribed to static event.");
    }

    public static void ResetCounter() => _totalInstancesCreated = 0;

    private void OnGrowingLeak(object? sender, EventArgs e)
    {
    }

    private async void OnGoBack(object? sender, EventArgs e)
    {
        DiagnosticsService.Instance.Log($"[S3] Instance #{_myInstance} navigating back while still rooted by event.");
        await Shell.Current.GoToAsync("..");

        LeakTracker.ForceGC();
        var before = DemoStateService.Instance.HomeSnapshot;
        if (before is null)
            return;

        var after = LeakTracker.Snapshot();
        var diff = LeakTracker.Compare(before, after);
        DiagnosticsService.Instance.Log(diff.ToString());
    }
}
