using System.Collections.ObjectModel;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Graphics;

namespace LeakDetector.Demo.Services;

public enum DiagLevel
{
    Info,
    Warning,
    Problem
}

public sealed class DiagnosticEntry
{
    public DiagnosticEntry(string timestamp, string message, DiagLevel level)
    {
        Timestamp = timestamp;
        Message = message;
        Level = level;
    }

    public string Timestamp { get; }
    public string Message { get; }
    public DiagLevel Level { get; }

    public string LevelTag => Level switch
    {
        DiagLevel.Warning => "WARN",
        DiagLevel.Problem => "ERROR",
        _ => "INFO"
    };

    public Color LevelColor => Level switch
    {
        DiagLevel.Warning => Color.FromArgb("#E8A247"),
        DiagLevel.Problem => Color.FromArgb("#E05B56"),
        _ => Color.FromArgb("#4CA7D8")
    };
}

public sealed class DiagnosticsService
{
    private const int MaxEntries = 300;
    private static DiagnosticsService? _instance;

    public static DiagnosticsService Instance => _instance ??= new DiagnosticsService();

    private DiagnosticsService()
    {
    }

    public ObservableCollection<DiagnosticEntry> Entries { get; } = new();

    public void Log(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        var entry = new DiagnosticEntry(
            DateTime.Now.ToString("HH:mm:ss.fff"),
            message,
            Classify(message));

        void AddEntry()
        {
            Entries.Insert(0, entry);
            if (Entries.Count > MaxEntries)
                Entries.RemoveAt(Entries.Count - 1);
        }

        if (MainThread.IsMainThread)
        {
            AddEntry();
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(AddEntry);
        }

        System.Diagnostics.Debug.WriteLine(message);
    }

    public void Clear()
    {
        if (MainThread.IsMainThread)
        {
            Entries.Clear();
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(Entries.Clear);
        }
    }

    private static DiagLevel Classify(string message)
    {
        if (message.Contains("NOT on main thread", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("LEAK DETECTED", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("ERROR", StringComparison.OrdinalIgnoreCase))
        {
            return DiagLevel.Problem;
        }

        if (message.Contains("WARNING", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("Possible blocking", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("leak", StringComparison.OrdinalIgnoreCase))
        {
            return DiagLevel.Warning;
        }

        return DiagLevel.Info;
    }
}
