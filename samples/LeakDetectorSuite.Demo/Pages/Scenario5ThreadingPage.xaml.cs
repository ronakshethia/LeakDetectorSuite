using LeakDetector.Demo.Services;
using LeakDetector.Threading;

namespace LeakDetector.Demo;

public partial class Scenario5ThreadingPage : ContentPage
{
    public Scenario5ThreadingPage()
    {
        InitializeComponent();
    }

    private void OnCheckMainThread(object? sender, EventArgs e)
    {
        ThreadGuard.EnsureMainThread();
        var threadId = Environment.CurrentManagedThreadId;
        DiagnosticsService.Instance.Log($"[S5] EnsureMainThread passed on thread #{threadId}.");
    }

    private async void OnWrongThread(object? sender, EventArgs e)
    {
        DiagnosticsService.Instance.Log("[S5] Dispatching EnsureMainThread to a background thread.");
        await Task.Run(() =>
        {
            DiagnosticsService.Instance.Log($"[S5] Running on thread #{Environment.CurrentManagedThreadId}.");
            ThreadGuard.EnsureMainThread();
        });
    }

    private void OnBlockingCall(object? sender, EventArgs e)
    {
        var thread = new Thread(() =>
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            DiagnosticsService.Instance.Log($"[S5] Simulating blocking-call detection on thread #{Environment.CurrentManagedThreadId}.");
            ThreadGuard.WarnIfBlockingCall();
        });

        thread.IsBackground = true;
        thread.Start();
    }
}
