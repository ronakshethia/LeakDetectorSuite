using LeakDetector.Demo.Services;
using LeakDetector.Performance;

namespace LeakDetector.Demo;

public partial class Scenario4PerfPage : ContentPage
{
    public Scenario4PerfPage()
    {
        InitializeComponent();
        UpdateThresholdLabel();
    }

    private async void OnFastOperation(object? sender, EventArgs e)
    {
        await Perf.MeasureAsync(async () => await Task.Delay(30), "FastOperation");
    }

    private async void OnSlowOperation(object? sender, EventArgs e)
    {
        await Perf.MeasureAsync(async () => await Task.Delay(350), "SlowOperation");
    }

    private void OnStrictThreshold(object? sender, EventArgs e)
    {
        Perf.WarnThresholdMs = 20;
        UpdateThresholdLabel();
        DiagnosticsService.Instance.Log("[S4] Threshold set to 20 ms.");
    }

    private async void OnMediumOperation(object? sender, EventArgs e)
    {
        await Perf.MeasureAsync(async () => await Task.Delay(50), "MediumOperation");
    }

    private void OnResetThreshold(object? sender, EventArgs e)
    {
        Perf.WarnThresholdMs = 100;
        UpdateThresholdLabel();
        DiagnosticsService.Instance.Log("[S4] Threshold reset to 100 ms.");
    }

    private void UpdateThresholdLabel()
    {
        ThresholdLabel.Text = $"Threshold: {Perf.WarnThresholdMs:F0} ms";
    }
}
