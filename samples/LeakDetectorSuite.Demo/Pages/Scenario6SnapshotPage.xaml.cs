using LeakDetector.Demo.Services;
using LeakDetector.Memory;

namespace LeakDetector.Demo;

public partial class Scenario6SnapshotPage : ContentPage
{
    private LeakSnapshot? _before;
    private readonly List<DemoObject> _heldObjects = new();

    private sealed class DemoObject
    {
        public DemoObject(int id)
        {
            Id = id;
        }

        public int Id { get; }
    }

    public Scenario6SnapshotPage()
    {
        InitializeComponent();
    }

    private void OnTakeBefore(object? sender, EventArgs e)
    {
        LeakTracker.ForceGC();
        _before = LeakTracker.Snapshot();
        DiagnosticsService.Instance.Log($"[S6] Before snapshot. {_before.TotalAlive} live objects.");
        CreateButton.IsEnabled = true;
    }

    private void OnCreateObjects(object? sender, EventArgs e)
    {
        _heldObjects.Clear();
        for (var i = 1; i <= 3; i++)
        {
            var demo = new DemoObject(i);
            _heldObjects.Add(demo);
            LeakTracker.Track(demo, nameof(DemoObject));
        }

        DiagnosticsService.Instance.Log("[S6] Tracked 3 DemoObject instances with strong references held.");
        ReleaseButton.IsEnabled = true;
        CompareButton.IsEnabled = true;
    }

    private void OnReleaseObjects(object? sender, EventArgs e)
    {
        _heldObjects.Clear();
        LeakTracker.ForceGC();
        DiagnosticsService.Instance.Log("[S6] Released strong references and forced GC.");
    }

    private void OnCompare(object? sender, EventArgs e)
    {
        if (_before is null)
            return;

        var after = LeakTracker.Snapshot();
        var diff = LeakTracker.Compare(_before, after);
        DiagnosticsService.Instance.Log(diff.ToString());
        DiagnosticsService.Instance.Log(diff.HasLeaks
            ? "[S6] Leaks detected. Some DemoObject instances are still alive."
            : "[S6] Clean comparison. DemoObject instances were released.");
    }
}
