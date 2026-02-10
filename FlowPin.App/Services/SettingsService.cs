using System.Text.Json;
using System.Text;
using System.IO;
using FlowPin.App.Models;

namespace FlowPin.App.Services;

public sealed class SettingsService
{
    private readonly LoggerService _logger;
    private readonly string _settingsPath;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public SettingsService(LoggerService logger)
    {
        _logger = logger;
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FlowPin");
        Directory.CreateDirectory(dir);
        _settingsPath = Path.Combine(dir, "settings.json");
    }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                var defaultSettings = new AppSettings();
                Save(defaultSettings);
                _logger.Configure(defaultSettings);
                return defaultSettings;
            }

            var json = File.ReadAllText(_settingsPath, Encoding.UTF8);
            AppSettings settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            settings.WindowClassList ??= new List<string>();
            settings.ProcessList ??= new List<string>();
            settings.ContinuousPreferredProcesses ??= new List<string>();
            settings.EnhancedModeProcesses ??= new List<string>();
            _logger.Configure(settings);
            return settings;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to load settings: {ex.Message}");
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        try
        {
            settings.WindowClassList ??= new List<string>();
            settings.ProcessList ??= new List<string>();
            settings.ContinuousPreferredProcesses ??= new List<string>();
            settings.EnhancedModeProcesses ??= new List<string>();
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(_settingsPath, json, new UTF8Encoding(false));
            _logger.Configure(settings);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to save settings: {ex.Message}");
        }
    }
}

