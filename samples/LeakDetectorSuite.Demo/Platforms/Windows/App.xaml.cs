namespace LeakDetector.Demo.WinUI;

public partial class App : MauiWinUIApplication
{
    public App()
    {
        InitializeComponent();
    }

    protected override MauiApp CreateMauiApp() => LeakDetector.Demo.MauiProgram.CreateMauiApp();
}
