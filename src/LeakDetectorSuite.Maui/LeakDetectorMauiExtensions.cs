using LeakDetector.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.LifecycleEvents;

namespace LeakDetector.Maui;

/// <summary>
/// Extension methods to integrate LeakDetector into a MAUI application host.
/// </summary>
public static class LeakDetectorMauiExtensions
{
    /// <summary>
    /// Enables automatic memory leak detection for MAUI pages and view-models.
    /// <para>
    /// Call this inside your <c>MauiProgram.CreateMauiApp()</c>:
    /// </para>
    /// <code>
    /// builder.UseLeakDetector();
    /// </code>
    /// </summary>
    /// <param name="builder">The MAUI app builder.</param>
    /// <param name="logger">
    /// Optional logging delegate. Defaults to <see cref="System.Diagnostics.Debug.WriteLine"/>.
    /// </param>
    /// <returns>The same <paramref name="builder"/> for fluent chaining.</returns>
    public static MauiAppBuilder UseLeakDetector(
        this MauiAppBuilder builder,
        Action<string>? logger = null)
    {
        // Allow callers to supply a custom logger (e.g. ILogger, Console.WriteLine…)
        if (logger is not null)
            LeakTracker.Logger = logger;

        // Register the host singleton FIRST so lifecycle hooks can resolve it.
        builder.Services.AddSingleton<LeakDetectorHost>(sp =>
        {
            var app = sp.GetRequiredService<IApplication>();
            return new LeakDetectorHost(app);
        });

        builder.ConfigureLifecycleEvents(lc =>
        {
#if ANDROID
            lc.AddAndroid(android =>
            {
                android.OnCreate((activity, _) =>
                {
                    // BUG-03/BUG-10 fix: resolve LeakDetectorHost via the MAUI
                    // service provider directly from within the lifecycle hook.
                    // This is guaranteed to run at app start on every platform.
                    // No internal type needs to be exposed to consumer projects.
                    IPlatformApplication.Current?.Services
                        .GetRequiredService<LeakDetectorHost>();

                    LeakTracker.Logger?.Invoke("[LeakDetector] Android lifecycle hooked.");
                });
            });
#endif

#if IOS || MACCATALYST
            lc.AddiOS(ios =>
            {
                ios.FinishedLaunching((app, _) =>
                {
                    IPlatformApplication.Current?.Services
                        .GetRequiredService<LeakDetectorHost>();

                    LeakTracker.Logger?.Invoke("[LeakDetector] iOS/Mac lifecycle hooked.");
                    return true;
                });
            });
#endif

#if WINDOWS
            lc.AddWindows(windows =>
            {
                windows.OnLaunched((app, _) =>
                {
                    IPlatformApplication.Current?.Services
                        .GetRequiredService<LeakDetectorHost>();

                    LeakTracker.Logger?.Invoke("[LeakDetector] Windows lifecycle hooked.");
                });
            });
#endif
        });

        return builder;
    }
}
