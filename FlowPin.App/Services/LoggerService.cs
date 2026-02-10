using System.Text;
using System.IO;
using FlowPin.App.Models;

namespace FlowPin.App.Services;

public sealed class LoggerService
{
    private readonly string _logPath;
    private readonly object _sync = new();
    private volatile bool _debugEnabled;
    private static readonly UTF8Encoding Utf8NoBom = new(false);

    public LoggerService()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FlowPin");
        Directory.CreateDirectory(dir);
        _logPath = Path.Combine(dir, "app.log");
    }

    public void Configure(AppSettings settings)
    {
        _debugEnabled = settings.EnableDebugLog;
    }

    public void Info(string message) => Write("INF", message);

    public void Debug(string message)
    {
        if (_debugEnabled)
        {
            Write("DBG", message);
        }
    }

    public void Error(string message) => Write("ERR", message);

    private void Write(string level, string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
        lock (_sync)
        {
            File.AppendAllText(_logPath, line + Environment.NewLine, Utf8NoBom);
        }
    }
}

