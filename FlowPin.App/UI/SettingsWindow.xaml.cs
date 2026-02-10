using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using FlowPin.App.Interop;
using FlowPin.App.Models;
using WColor = System.Windows.Media.Color;
using WColorConverter = System.Windows.Media.ColorConverter;
using WBrushes = System.Windows.Media.Brushes;
using WSolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace FlowPin.App.UI;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private readonly List<string> _overlayPresets =
    [
        "#883FA9FF",
        "#8857D3C8",
        "#889C8CFF",
        "#88FF8E7A",
        "#88FFC66B",
        "#88A6E3A1",
        "#88E3B7FF"
    ];

    private bool _isLanguageUpdating;
    private string _selectedSwatchHex = string.Empty;

    public SettingsWindow(AppSettings settings)
    {
        _settings = settings;
        InitializeComponent();
        LoadView();
    }

    private void LoadView()
    {
        SensitivityBox.Text = _settings.Sensitivity.ToString(CultureInfo.InvariantCulture);
        DeadZoneBox.Text = _settings.DeadZone.ToString(CultureInfo.InvariantCulture);
        RangeBox.Text = _settings.Range.ToString(CultureInfo.InvariantCulture);
        MaxSpeedBox.Text = _settings.MaxSpeed.ToString(CultureInfo.InvariantCulture);
        GammaBox.Text = _settings.Gamma.ToString(CultureInfo.InvariantCulture);
        MiddleClickDebounceBox.Text = _settings.MiddleClickDebounceMs.ToString(CultureInfo.InvariantCulture);
        OverlaySizeBox.Text = _settings.OverlaySize.ToString(CultureInfo.InvariantCulture);
        OverlayColorBox.Text = _settings.OverlayRingColorHex;

        ProcessListBox.Text = string.Join(Environment.NewLine, _settings.ProcessList);
        WindowClassListBox.Text = string.Join(Environment.NewLine, _settings.WindowClassList);
        ContinuousPreferredBox.Text = string.Join(Environment.NewLine, _settings.ContinuousPreferredProcesses);
        EnhancedModeBox.Text = string.Join(Environment.NewLine, _settings.EnhancedModeProcesses);

        RenderColorSwatches();
        SelectPresetByHex(_settings.OverlayRingColorHex);
        UpdateColorPreview(_settings.OverlayRingColorHex);
        UpdateSizePreview(OverlaySizeBox.Text);

        _isLanguageUpdating = true;
        try
        {
            LanguageBox.SelectedIndex = string.Equals(_settings.UiLanguage, "en-US", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        }
        finally
        {
            _isLanguageUpdating = false;
        }

        FilterModeBox.SelectedIndex = (int)_settings.FilterMode;
        ApplyLanguage(GetSelectedLanguage());
        UpdateFilterModeButtons();
        UpdateLanguageButtons();
    }

    private void Save_OnClick(object sender, RoutedEventArgs e)
    {
        if (!double.TryParse(SensitivityBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var sensitivity) ||
            !int.TryParse(DeadZoneBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var deadZone) ||
            !int.TryParse(RangeBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var range) ||
            !double.TryParse(GammaBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var gamma) ||
            !double.TryParse(OverlaySizeBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var overlaySize) ||
            !int.TryParse(MiddleClickDebounceBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var debounceMs))
        {
            ShowNotice("Please check input format.", "请检查输入格式。");
            return;
        }

        var colorText = OverlayColorBox.Text.Trim();
        if (!TryParseColor(colorText, out var normalizedColor))
        {
            ShowNotice("Invalid color format. Use #AARRGGBB or #RRGGBB.", "颜色格式无效，请使用 #AARRGGBB 或 #RRGGBB。");
            return;
        }

        _settings.Sensitivity = Math.Clamp(sensitivity, 0.1, 4.0);
        _settings.DeadZone = Math.Clamp(deadZone, 0, 200);
        _settings.Range = Math.Clamp(range, 1, 4000);
        _settings.Gamma = Math.Clamp(gamma, 0.5, 5.0);
        _settings.MiddleClickDebounceMs = Math.Clamp(debounceMs, 0, 1000);
        _settings.OverlaySize = Math.Clamp(overlaySize, 12.0, 64.0);
        _settings.FilterMode = (FilterMode)Math.Clamp(FilterModeBox.SelectedIndex, 0, 2);
        _settings.OverlayRingColorHex = normalizedColor;
        _settings.UiLanguage = GetSelectedLanguage();

        _settings.ProcessList = ParseList(ProcessListBox.Text);
        _settings.WindowClassList = ParseList(WindowClassListBox.Text);
        _settings.ContinuousPreferredProcesses = ParseList(ContinuousPreferredBox.Text);
        _settings.EnhancedModeProcesses = ParseList(EnhancedModeBox.Text);

        DialogResult = true;
        Close();
    }

    private void Cancel_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OverlayColorBox_OnTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        var text = OverlayColorBox.Text.Trim();
        if (TryParseColor(text, out var normalizedColor))
        {
            UpdateColorPreview(normalizedColor);
            SelectPresetByHex(normalizedColor);
            UpdateSwatchSelectionVisual();
        }
    }

    private void OverlaySizeBox_OnTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        UpdateSizePreview(OverlaySizeBox.Text);
    }

    private void LanguageBox_OnSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_isLanguageUpdating)
        {
            return;
        }

        ApplyLanguage(GetSelectedLanguage());
        UpdateLanguageButtons();
    }

    private string GetSelectedLanguage()
    {
        return (LanguageBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag?.ToString() == "en-US"
            ? "en-US"
            : "zh-CN";
    }

    private void ApplyLanguage(string language)
    {
        var en = string.Equals(language, "en-US", StringComparison.OrdinalIgnoreCase);
        Title = en ? "FlowPin Settings" : "FlowPin 设置";
        HeaderSubtitleText.Text = en ? "Global middle-click auto-scroll for Windows" : "Windows 全局中键自动滚动工具";

        LabelSensitivity.Text = en ? "Sensitivity" : "灵敏度";
        HintSensitivity.Text = en ? "Recommended 0.5 ~ 1.2. Higher is faster." : "建议 0.5 ~ 1.2，越大越灵敏。";
        LabelDeadZone.Text = en ? "Dead Zone" : "死区";
        HintDeadZone.Text = en ? "Recommended 16 ~ 40. Higher reduces accidental scroll." : "建议 16 ~ 40，越大越不易误触。";
        LabelRange.Text = en ? "Range" : "范围";
        HintRange.Text = en ? "Distance scaling for acceleration curve." : "控制加速曲线的距离尺度。";
        LabelMaxSpeed.Visibility = Visibility.Collapsed;
        MaxSpeedBox.Visibility = Visibility.Collapsed;
        HintMaxSpeed.Visibility = Visibility.Collapsed;
        LabelGamma.Text = "Gamma";
        HintGamma.Text = en ? "Higher means slower start and faster finish." : "越大越慢起快收。";

        LabelMiddleClickDebounce.Text = en ? "Middle Click Debounce" : "中键防抖";
        HintMiddleClickDebounceUnit.Text = "ms";
        HintMiddleClickDebounce.Text = en ? "Ignore repeated middle-click within this interval." : "在该时间内忽略重复中键触发。";

        LabelFilterMode.Text = en ? "Apply To" : "应用范围";
        HintFilterMode.Text = en
            ? "All Apps: enable everywhere. Exclude List: disable listed apps. Only List: enable listed apps only."
            : "全部应用：所有程序启用；排除名单：名单内禁用；仅名单：只在名单内启用。";
        FilterModeDisabledButton.Content = en ? "All Apps" : "全部应用";
        FilterModeBlacklistButton.Content = en ? "Exclude List" : "排除名单";
        FilterModeWhitelistButton.Content = en ? "Only List" : "仅名单";

        LabelProcessList.Text = en ? "Process List (no .exe)" : "进程名单（不含 .exe）";
        AddForegroundToGeneralButton.Content = en ? "Add Foreground Process" : "添加前台进程";

        LabelWindowClassList.Text = en ? "Window Class List" : "窗口类名名单";
        AddForegroundClassButton.Content = en ? "Add Foreground Class" : "添加前台类名";
        HintWindowClassList.Text = en ? "Capture current foreground window class." : "添加当前前台窗口类名。";

        LabelContinuousPreferred.Text = en ? "Continuous Priority Apps" : "无极优先应用";
        HintContinuousPreferred.Text = en ? "One process name per line. Try continuous mode first." : "每行一个进程名，优先尝试连续模式。";
        AddForegroundToContinuousButton.Content = en ? "Add Foreground Process" : "添加前台进程";

        LabelEnhancedMode.Text = en ? "Enhanced Mode Apps" : "增强模式应用";
        HintEnhancedMode.Text = en ? "One process name per line. Use smoother wheel fallback." : "每行一个进程名，使用平滑滚轮回退。";
        AddForegroundToEnhancedButton.Content = en ? "Add Foreground Process" : "添加前台进程";

        LabelOverlaySize.Text = en ? "Indicator Size" : "指示器大小";
        HintOverlaySizeUnit.Text = en ? "px (16 ~ 28 recommended)" : "px（建议 16 ~ 28）";
        LabelPresetColor.Text = en ? "Preset Colors" : "预置颜色";
        HintPresetColor.Text = en ? "Click a swatch to select." : "点击色块即可选中。";
        LabelManualColor.Text = en ? "Manual Color" : "手动颜色";
        HintManualColor.Text = en ? "Supports #AARRGGBB or #RRGGBB." : "支持 #AARRGGBB 或 #RRGGBB。";

        LabelLanguage.Text = "语言/Language";
        LanguageChineseButton.Content = "中文";
        LanguageEnglishButton.Content = "English";

        SaveButton.Content = en ? "Save" : "保存";
        CancelButton.Content = en ? "Cancel" : "取消";
        ResetDefaultsButton.Content = en ? "Restore Defaults" : "恢复默认设置";
        SaveHint.Text = en ? "Changes apply only after clicking Save." : "修改后必须点击“保存”才会生效。";

        UpdateFilterModeButtons();
    }

    private void FilterModeDisabledButton_OnClick(object sender, RoutedEventArgs e)
    {
        FilterModeBox.SelectedIndex = 0;
        UpdateFilterModeButtons();
    }

    private void FilterModeBlacklistButton_OnClick(object sender, RoutedEventArgs e)
    {
        FilterModeBox.SelectedIndex = 1;
        UpdateFilterModeButtons();
    }

    private void FilterModeWhitelistButton_OnClick(object sender, RoutedEventArgs e)
    {
        FilterModeBox.SelectedIndex = 2;
        UpdateFilterModeButtons();
    }

    private void LanguageChineseButton_OnClick(object sender, RoutedEventArgs e)
    {
        LanguageBox.SelectedIndex = 0;
        ApplyLanguage(GetSelectedLanguage());
        UpdateLanguageButtons();
    }

    private void LanguageEnglishButton_OnClick(object sender, RoutedEventArgs e)
    {
        LanguageBox.SelectedIndex = 1;
        ApplyLanguage(GetSelectedLanguage());
        UpdateLanguageButtons();
    }

    private void UpdateFilterModeButtons()
    {
        var selected = Math.Clamp(FilterModeBox.SelectedIndex, 0, 2);
        ApplySegmentButtonSelectedStyle(FilterModeDisabledButton, selected == 0);
        ApplySegmentButtonSelectedStyle(FilterModeBlacklistButton, selected == 1);
        ApplySegmentButtonSelectedStyle(FilterModeWhitelistButton, selected == 2);
    }

    private void UpdateLanguageButtons()
    {
        var selected = LanguageBox.SelectedIndex == 1 ? 1 : 0;
        ApplySegmentButtonSelectedStyle(LanguageChineseButton, selected == 0);
        ApplySegmentButtonSelectedStyle(LanguageEnglishButton, selected == 1);
    }

    private static void ApplySegmentButtonSelectedStyle(System.Windows.Controls.Button button, bool selected)
    {
        button.Background = selected ? new WSolidColorBrush(WColor.FromRgb(47, 120, 230)) : WBrushes.White;
        button.BorderBrush = selected ? new WSolidColorBrush(WColor.FromRgb(47, 120, 230)) : new WSolidColorBrush(WColor.FromRgb(197, 215, 240));
        button.Foreground = selected ? WBrushes.White : new WSolidColorBrush(WColor.FromRgb(43, 78, 132));
    }

    private void RenderColorSwatches()
    {
        ColorSwatchPanel.Children.Clear();
        foreach (var hex in _overlayPresets)
        {
            var button = new System.Windows.Controls.Button
            {
                Tag = hex,
                Width = 24,
                Height = 24,
                Margin = new Thickness(0, 0, 8, 0),
                Padding = new Thickness(0),
                BorderThickness = new Thickness(0),
                BorderBrush = WBrushes.Transparent,
                Background = WBrushes.Transparent,
                ToolTip = hex
            };

            var rectangle = new System.Windows.Shapes.Rectangle
            {
                Width = 16,
                Height = 16,
                RadiusX = 2,
                RadiusY = 2,
                Fill = new WSolidColorBrush((WColor)WColorConverter.ConvertFromString(hex)),
                Stroke = new WSolidColorBrush(WColor.FromRgb(90, 90, 90)),
                StrokeThickness = 1
            };

            button.Content = rectangle;
            button.Click += ColorSwatchButton_OnClick;
            ColorSwatchPanel.Children.Add(button);
        }

        UpdateSwatchSelectionVisual();
    }

    private void ColorSwatchButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button || button.Tag is not string hex)
        {
            return;
        }

        _selectedSwatchHex = hex;
        OverlayColorBox.Text = hex;
        UpdateColorPreview(hex);
        UpdateSwatchSelectionVisual();
    }

    private void UpdateSwatchSelectionVisual()
    {
        foreach (var child in ColorSwatchPanel.Children)
        {
            if (child is not System.Windows.Controls.Button button || button.Tag is not string hex)
            {
                continue;
            }

            var selected = !string.IsNullOrWhiteSpace(_selectedSwatchHex) &&
                           string.Equals(_selectedSwatchHex, hex, StringComparison.OrdinalIgnoreCase);
            button.BorderThickness = selected ? new Thickness(2) : new Thickness(0);
            button.BorderBrush = selected
                ? new WSolidColorBrush(WColor.FromRgb(40, 130, 255))
                : WBrushes.Transparent;
        }
    }

    private void SelectPresetByHex(string hex)
    {
        var matched = _overlayPresets.FirstOrDefault(item =>
            string.Equals(item, hex, StringComparison.OrdinalIgnoreCase));
        _selectedSwatchHex = string.IsNullOrWhiteSpace(matched) ? string.Empty : matched;
    }

    private void UpdateColorPreview(string colorText)
    {
        if (!TryParseColor(colorText, out var normalized))
        {
            return;
        }

        var parsed = (WColor)WColorConverter.ConvertFromString(normalized);
        OverlaySizePreview.Stroke = new WSolidColorBrush(parsed);
        OverlaySizePreview.Fill = new WSolidColorBrush(WColor.FromArgb(0x24, parsed.R, parsed.G, parsed.B));
    }

    private void UpdateSizePreview(string sizeText)
    {
        if (!double.TryParse(sizeText, NumberStyles.Float, CultureInfo.InvariantCulture, out var size))
        {
            return;
        }

        size = Math.Clamp(size, 12.0, 64.0);
        var previewSize = Math.Clamp(size, 10.0, 40.0);
        OverlaySizePreview.Width = previewSize;
        OverlaySizePreview.Height = previewSize;
    }

    private static List<string> ParseList(string text)
    {
        return text
            .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Trim())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool TryParseColor(string input, out string normalized)
    {
        normalized = string.Empty;
        try
        {
            var parsed = WColorConverter.ConvertFromString(input);
            if (parsed is not WColor color)
            {
                return false;
            }

            normalized = $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async void AddForegroundToContinuousButton_OnClick(object sender, RoutedEventArgs e)
    {
        var context = await CaptureForegroundWindowContextAsync();
        if (context is null || string.Equals(context.ProcessName, "FlowPin.App", StringComparison.OrdinalIgnoreCase))
        {
            ShowCaptureFailedMessage();
            return;
        }

        AddUniqueLineToTextBox(ContinuousPreferredBox, context.ProcessName);
    }

    private void ResetDefaultsButton_OnClick(object sender, RoutedEventArgs e)
    {
        var defaults = new AppSettings();
        SensitivityBox.Text = defaults.Sensitivity.ToString(CultureInfo.InvariantCulture);
        DeadZoneBox.Text = defaults.DeadZone.ToString(CultureInfo.InvariantCulture);
        RangeBox.Text = defaults.Range.ToString(CultureInfo.InvariantCulture);
        MaxSpeedBox.Text = defaults.MaxSpeed.ToString(CultureInfo.InvariantCulture);
        GammaBox.Text = defaults.Gamma.ToString(CultureInfo.InvariantCulture);
        MiddleClickDebounceBox.Text = defaults.MiddleClickDebounceMs.ToString(CultureInfo.InvariantCulture);
        OverlaySizeBox.Text = defaults.OverlaySize.ToString(CultureInfo.InvariantCulture);
        OverlayColorBox.Text = defaults.OverlayRingColorHex;

        ProcessListBox.Text = string.Join(Environment.NewLine, defaults.ProcessList);
        WindowClassListBox.Text = string.Join(Environment.NewLine, defaults.WindowClassList);
        ContinuousPreferredBox.Text = string.Join(Environment.NewLine, defaults.ContinuousPreferredProcesses);
        EnhancedModeBox.Text = string.Join(Environment.NewLine, defaults.EnhancedModeProcesses);

        FilterModeBox.SelectedIndex = (int)defaults.FilterMode;
        LanguageBox.SelectedIndex = string.Equals(defaults.UiLanguage, "en-US", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        ApplyLanguage(GetSelectedLanguage());
        UpdateFilterModeButtons();
        UpdateLanguageButtons();

        SelectPresetByHex(defaults.OverlayRingColorHex);
        UpdateColorPreview(defaults.OverlayRingColorHex);
        UpdateSwatchSelectionVisual();
        UpdateSizePreview(OverlaySizeBox.Text);
    }

    private async void AddForegroundToEnhancedButton_OnClick(object sender, RoutedEventArgs e)
    {
        var context = await CaptureForegroundWindowContextAsync();
        if (context is null || string.Equals(context.ProcessName, "FlowPin.App", StringComparison.OrdinalIgnoreCase))
        {
            ShowCaptureFailedMessage();
            return;
        }

        AddUniqueLineToTextBox(EnhancedModeBox, context.ProcessName);
    }

    private async void AddForegroundToGeneralButton_OnClick(object sender, RoutedEventArgs e)
    {
        var context = await CaptureForegroundWindowContextAsync();
        if (context is null || string.Equals(context.ProcessName, "FlowPin.App", StringComparison.OrdinalIgnoreCase))
        {
            ShowCaptureFailedMessage();
            return;
        }

        AddUniqueLineToTextBox(ProcessListBox, context.ProcessName);
    }

    private async void AddForegroundClassButton_OnClick(object sender, RoutedEventArgs e)
    {
        var context = await CaptureForegroundWindowContextAsync();
        if (context is null || string.IsNullOrWhiteSpace(context.WindowClass))
        {
            ShowCaptureFailedMessage();
            return;
        }

        AddUniqueLineToTextBox(WindowClassListBox, context.WindowClass);
    }

    private async Task<ForegroundWindowContext?> CaptureForegroundWindowContextAsync()
    {
        var oldTopmost = Topmost;
        Topmost = false;
        Hide();
        await Task.Delay(900);

        var context = TryGetForegroundWindowContext();

        Show();
        Activate();
        Topmost = oldTopmost;
        return context;
    }

    private ForegroundWindowContext? TryGetForegroundWindowContext()
    {
        var hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
        {
            return null;
        }

        NativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
        if (processId == 0)
        {
            return null;
        }

        try
        {
            var processName = Process.GetProcessById((int)processId).ProcessName;
            if (string.IsNullOrWhiteSpace(processName))
            {
                return null;
            }

            var className = NativeMethods.GetWindowClassName(hwnd);
            return new ForegroundWindowContext(processName, className);
        }
        catch
        {
            return null;
        }
    }

    private void AddUniqueLineToTextBox(System.Windows.Controls.TextBox target, string value)
    {
        var list = ParseList(target.Text);
        if (!list.Any(item => string.Equals(item, value, StringComparison.OrdinalIgnoreCase)))
        {
            list.Add(value);
            target.Text = string.Join(Environment.NewLine, list);
        }
    }

    private void ShowCaptureFailedMessage()
    {
        ShowNotice(
            "Failed to capture target app. Click button then switch to target app immediately.",
            "未捕获到目标应用。点击按钮后请立刻切换到目标应用。");
    }

    private void ShowNotice(string englishText, string chineseText)
    {
        var en = GetSelectedLanguage() == "en-US";
        System.Windows.MessageBox.Show(
            en ? englishText : chineseText,
            en ? "Notice" : "提示",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private sealed record ForegroundWindowContext(string ProcessName, string WindowClass);
}
