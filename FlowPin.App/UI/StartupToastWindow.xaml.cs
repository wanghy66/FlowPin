using System.Windows;
using System.Windows.Threading;

namespace FlowPin.App.UI;

public partial class StartupToastWindow : Window
{
    public StartupToastWindow(string language)
    {
        InitializeComponent();
        ApplyLanguage(language);
        Loaded += OnLoaded;
    }

    private void ApplyLanguage(string language)
    {
        var en = string.Equals(language, "en-US", StringComparison.OrdinalIgnoreCase);
        TitleText.Text = en ? "FlowPin is running" : "FlowPin 已启动";
        BodyText.Text = en
            ? "Running in tray. Left-click tray icon to open settings."
            : "程序正在托盘运行，左键图标可打开设置。";
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 16;
        Top = workArea.Bottom - Height - 20;

        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(2200)
        };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            Close();
        };
        timer.Start();
    }
}
