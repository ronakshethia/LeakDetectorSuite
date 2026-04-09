using LeakDetector.Demo.Services;
using LeakDetector.Maui;
using LeakDetector.Performance;
using LeakDetector.Threading;
using Microsoft.Extensions.Logging;

namespace LeakDetector.Demo;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var diagnostics = DiagnosticsService.Instance;

        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseLeakDetector(diagnostics.Log);

#if DEBUG
        builder.Logging.AddDebug();
#endif

        Perf.Logger = diagnostics.Log;
        ThreadGuard.Logger = diagnostics.Log;

        return builder.Build();
    }
}
