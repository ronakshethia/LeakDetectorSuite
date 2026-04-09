using LeakDetector.Memory;
using LeakDetector.Performance;
using LeakDetector.Threading;

namespace LeakDetector.CoreTests;

public sealed class CoreLibraryTests : IDisposable
{
    private readonly Action<string> _originalPerfLogger;
    private readonly double _originalWarnThreshold;
    private readonly Action<string> _originalThreadLogger;
    private readonly bool _originalThrowOnViolation;
    private readonly Func<bool>? _originalMainThreadDetector;
    private readonly SynchronizationContext? _originalSyncContext;

    public CoreLibraryTests()
    {
        _originalPerfLogger = Perf.Logger;
        _originalWarnThreshold = Perf.WarnThresholdMs;
        _originalThreadLogger = ThreadGuard.Logger;
        _originalThrowOnViolation = ThreadGuard.ThrowOnViolation;
        _originalMainThreadDetector = ThreadGuard.MainThreadDetector;
        _originalSyncContext = SynchronizationContext.Current;

        LeakTracker.Reset();
    }

    [Fact]
    public void LeakTracker_compare_reports_growth_for_tracked_objects()
    {
        var before = LeakTracker.Snapshot();

        var tracked = new object();
        LeakTracker.Track(tracked, "Specimen");

        var after = LeakTracker.Snapshot();
        var diff = LeakTracker.Compare(before, after);

        Assert.True(diff.HasLeaks);
        Assert.Equal(1, diff.TotalDelta);
        Assert.Equal(1, after.Counts["Specimen"]);
    }

    [Fact]
    public void Perf_measure_logs_warning_when_threshold_is_exceeded()
    {
        var messages = new List<string>();
        Perf.Logger = messages.Add;
        Perf.WarnThresholdMs = 1;

        Perf.Measure(() => Thread.Sleep(20), "SlowOperation");

        Assert.Contains(messages, message => message.Contains("[Perf WARNING]", StringComparison.Ordinal));
        Assert.Contains(messages, message => message.Contains("SlowOperation", StringComparison.Ordinal));
    }

    [Fact]
    public void ThreadGuard_does_not_claim_to_know_the_ui_thread_without_a_detector()
    {
        var messages = new List<string>();
        ThreadGuard.Logger = messages.Add;
        ThreadGuard.MainThreadDetector = null;

        ThreadGuard.EnsureMainThread();

        Assert.False(ThreadGuard.IsMainThreadDetectionAvailable);
        Assert.False(ThreadGuard.IsMainThread());
        Assert.Contains(messages, message => message.Contains("Main-thread detection is unavailable", StringComparison.Ordinal));
        Assert.DoesNotContain(messages, message => message.Contains("NOT on main thread", StringComparison.Ordinal));
    }

    [Fact]
    public void ThreadGuard_uses_custom_detector_for_main_thread_checks()
    {
        ThreadGuard.MainThreadDetector = () => true;

        Assert.True(ThreadGuard.IsMainThreadDetectionAvailable);
        Assert.True(ThreadGuard.IsMainThread());
    }

    [Fact]
    public void WarnIfBlockingCall_logs_when_sync_context_is_present_and_detector_reports_background_thread()
    {
        var messages = new List<string>();
        ThreadGuard.Logger = messages.Add;
        ThreadGuard.MainThreadDetector = () => false;
        SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

        ThreadGuard.WarnIfBlockingCall();

        Assert.Contains(messages, message => message.Contains("Possible blocking call detected", StringComparison.Ordinal));
    }

    public void Dispose()
    {
        Perf.Logger = _originalPerfLogger;
        Perf.WarnThresholdMs = _originalWarnThreshold;

        ThreadGuard.Logger = _originalThreadLogger;
        ThreadGuard.ThrowOnViolation = _originalThrowOnViolation;
        ThreadGuard.MainThreadDetector = _originalMainThreadDetector;
        SynchronizationContext.SetSynchronizationContext(_originalSyncContext);

        LeakTracker.Reset();
    }
}
