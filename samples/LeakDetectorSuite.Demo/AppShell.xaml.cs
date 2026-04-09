namespace LeakDetector.Demo;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(DiagnosticsPage), typeof(DiagnosticsPage));
        Routing.RegisterRoute(nameof(Scenario1CleanNavPage), typeof(Scenario1CleanNavPage));
        Routing.RegisterRoute(nameof(Scenario2LeakyPage), typeof(Scenario2LeakyPage));
        Routing.RegisterRoute(nameof(Scenario3MultiNavPage), typeof(Scenario3MultiNavPage));
        Routing.RegisterRoute(nameof(Scenario4PerfPage), typeof(Scenario4PerfPage));
        Routing.RegisterRoute(nameof(Scenario5ThreadingPage), typeof(Scenario5ThreadingPage));
        Routing.RegisterRoute(nameof(Scenario6SnapshotPage), typeof(Scenario6SnapshotPage));
    }
}
