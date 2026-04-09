using LeakDetector.Memory;

namespace LeakDetector.Demo.Services;

public sealed class DemoStateService
{
    private static DemoStateService? _instance;

    public static DemoStateService Instance => _instance ??= new DemoStateService();

    private DemoStateService()
    {
    }

    public LeakSnapshot? HomeSnapshot { get; set; }
}
