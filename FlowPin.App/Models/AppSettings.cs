namespace FlowPin.App.Models;

public sealed class AppSettings
{
    public double Sensitivity { get; set; } = 1.5;
    public int DeadZone { get; set; } = 14;
    public int Range { get; set; } = 240;
    public double MaxSpeed { get; set; } = 4200.0;
    public double Gamma { get; set; } = 1.6;
    public int MiddleClickDebounceMs { get; set; } = 120;
    public FilterMode FilterMode { get; set; } = FilterMode.Disabled;
    public List<string> ProcessList { get; set; } = new()
    {
        "chrome",
        "msedge",
        "firefox",
        "opera",
        "brave"
    };
    public List<string> WindowClassList { get; set; } = new();
    public bool EnableDebugLog { get; set; } = false;
    public string OverlayRingColorHex { get; set; } = "#883FA9FF";
    public double OverlaySize { get; set; } = 20.0;
    public string UiLanguage { get; set; } = "zh-CN";
    public bool HasShownStartupGuide { get; set; } = false;
    public List<string> ContinuousPreferredProcesses { get; set; } = new()
    {
        "Code",
        "notepad++"
    };
    public List<string> EnhancedModeProcesses { get; set; } = new()
    {
        "WindowsTerminal",
        "explorer",
        "cmd",
        "powershell"
    };
}

public enum FilterMode
{
    Disabled = 0,
    Blacklist = 1,
    Whitelist = 2
}

