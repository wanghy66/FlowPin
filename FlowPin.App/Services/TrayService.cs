using Forms = System.Windows.Forms;
using FlowPin.App.Models;

namespace FlowPin.App.Services;

public sealed class TrayService : IDisposable
{
    private readonly Action _onToggleEnabled;
    private readonly Action _onShowSettings;
    private readonly Action _onExit;
    private readonly Func<bool> _isEnabledAccessor;
    private readonly Func<string> _languageAccessor;
    private readonly Func<FilterMode> _filterModeAccessor;
    private Forms.NotifyIcon? _notifyIcon;
    private Forms.ToolStripMenuItem? _toggleMenuItem;
    private Forms.ToolStripMenuItem? _settingsMenuItem;
    private Forms.ToolStripMenuItem? _exitMenuItem;

    public TrayService(
        Action onToggleEnabled,
        Action onShowSettings,
        Action onExit,
        Func<bool> isEnabledAccessor,
        Func<string> languageAccessor,
        Func<FilterMode> filterModeAccessor)
    {
        _onToggleEnabled = onToggleEnabled;
        _onShowSettings = onShowSettings;
        _onExit = onExit;
        _isEnabledAccessor = isEnabledAccessor;
        _languageAccessor = languageAccessor;
        _filterModeAccessor = filterModeAccessor;
    }

    public void Start()
    {
        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Visible = true,
            Text = "FlowPin"
        };
        _notifyIcon.MouseClick += (_, args) =>
        {
            if (args.Button == Forms.MouseButtons.Left)
            {
                _onShowSettings();
            }
        };

        var menu = new Forms.ContextMenuStrip();
        menu.RenderMode = Forms.ToolStripRenderMode.Professional;
        menu.Renderer = new Forms.ToolStripProfessionalRenderer(new CoolBlueColorTable());
        menu.ShowImageMargin = false;
        menu.BackColor = System.Drawing.Color.FromArgb(249, 252, 255);
        menu.ForeColor = System.Drawing.Color.FromArgb(36, 74, 134);
        _toggleMenuItem = new Forms.ToolStripMenuItem();
        _toggleMenuItem.Click += (_, _) => _onToggleEnabled();
        menu.Items.Add(_toggleMenuItem);

        _settingsMenuItem = new Forms.ToolStripMenuItem();
        _settingsMenuItem.Click += (_, _) => _onShowSettings();
        menu.Items.Add(_settingsMenuItem);

        _exitMenuItem = new Forms.ToolStripMenuItem();
        _exitMenuItem.Click += (_, _) => _onExit();
        menu.Items.Add(_exitMenuItem);

        _notifyIcon.ContextMenuStrip = menu;
        RefreshState();
    }

    public void RefreshState()
    {
        if (_notifyIcon is null || _toggleMenuItem is null || _settingsMenuItem is null || _exitMenuItem is null)
        {
            return;
        }

        var enabled = _isEnabledAccessor();
        var en = IsEnglish();

        _toggleMenuItem.Text = enabled
            ? (en ? "Disable Scroll" : "禁用滚动")
            : (en ? "Enable Scroll" : "启用滚动");
        _settingsMenuItem.Text = en ? "Settings" : "设置";
        _exitMenuItem.Text = en ? "Exit" : "退出";

        _notifyIcon.Icon = enabled ? System.Drawing.SystemIcons.Application : System.Drawing.SystemIcons.Warning;
        var modeText = GetFilterModeText(en);
        _notifyIcon.Text = enabled
            ? (en ? $"FlowPin: Enabled{Environment.NewLine}{modeText}" : $"FlowPin：已启用{Environment.NewLine}{modeText}")
            : (en ? $"FlowPin: Disabled{Environment.NewLine}{modeText}" : $"FlowPin：已禁用{Environment.NewLine}{modeText}");
    }

    public void ShowStartupReady()
    {
        if (_notifyIcon is null)
        {
            return;
        }

        var en = IsEnglish();
        _notifyIcon.BalloonTipTitle = en ? "FlowPin Started" : "FlowPin 已启动";
        _notifyIcon.BalloonTipText = en ? "Running in tray background." : "程序正在托盘后台运行。";
        _notifyIcon.BalloonTipIcon = Forms.ToolTipIcon.Info;
        _notifyIcon.ShowBalloonTip(1800);
    }

    public void ShowStartupGuide()
    {
        if (_notifyIcon is null)
        {
            return;
        }

        var en = IsEnglish();
        _notifyIcon.BalloonTipTitle = en ? "Quick Tip" : "使用提示";
        _notifyIcon.BalloonTipText = en
            ? "Right-click tray icon to open settings or exit."
            : "右键托盘图标可打开设置或退出。";
        _notifyIcon.BalloonTipIcon = Forms.ToolTipIcon.None;
        _notifyIcon.ShowBalloonTip(3000);
    }

    public void ShowRuntimeWarning(string title, string message)
    {
        if (_notifyIcon is null)
        {
            return;
        }

        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.BalloonTipIcon = Forms.ToolTipIcon.Warning;
        _notifyIcon.ShowBalloonTip(2800);
    }

    public void Dispose()
    {
        if (_notifyIcon is not null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }
    }

    private bool IsEnglish()
    {
        return string.Equals(_languageAccessor(), "en-US", StringComparison.OrdinalIgnoreCase);
    }

    private string GetFilterModeText(bool en)
    {
        return _filterModeAccessor() switch
        {
            FilterMode.Disabled => en ? "Mode: All Apps" : "模式：全部应用",
            FilterMode.Blacklist => en ? "Mode: Exclude List" : "模式：排除名单",
            FilterMode.Whitelist => en ? "Mode: Only List" : "模式：仅名单",
            _ => en ? "Mode: Unknown" : "模式：未知"
        };
    }

    private sealed class CoolBlueColorTable : Forms.ProfessionalColorTable
    {
        public override System.Drawing.Color MenuBorder => System.Drawing.Color.FromArgb(216, 227, 245);
        public override System.Drawing.Color ToolStripDropDownBackground => System.Drawing.Color.FromArgb(249, 252, 255);
        public override System.Drawing.Color ImageMarginGradientBegin => System.Drawing.Color.FromArgb(249, 252, 255);
        public override System.Drawing.Color ImageMarginGradientMiddle => System.Drawing.Color.FromArgb(249, 252, 255);
        public override System.Drawing.Color ImageMarginGradientEnd => System.Drawing.Color.FromArgb(249, 252, 255);
        public override System.Drawing.Color MenuItemBorder => System.Drawing.Color.FromArgb(47, 120, 230);
        public override System.Drawing.Color MenuItemSelected => System.Drawing.Color.FromArgb(231, 241, 255);
        public override System.Drawing.Color MenuItemSelectedGradientBegin => System.Drawing.Color.FromArgb(231, 241, 255);
        public override System.Drawing.Color MenuItemSelectedGradientEnd => System.Drawing.Color.FromArgb(231, 241, 255);
    }
}

