using LeakDetector.Demo.Services;

namespace LeakDetector.Demo;

public partial class DiagnosticsPage : ContentPage
{
    public DiagnosticsPage()
    {
        InitializeComponent();
        BindingContext = DiagnosticsService.Instance;
    }

    private void OnClear(object? sender, EventArgs e) => DiagnosticsService.Instance.Clear();
}
