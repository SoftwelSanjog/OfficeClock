using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Threading;
using OfficeClock.Data;
using OfficeClock.Services;
using OfficeClock.ViewModels;
using OfficeClock.Views;

namespace OfficeClock;

public partial class App : Application
{
    /// Set by Program; a second launch signals it (FR-7).
    public static EventWaitHandle? ShowSignal;

    MainWindow? _window;
    ClockRepository? _repo;
    MainViewModel? _vm;
    AttendanceTracker? _tracker;
    TrayIcon? _tray;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try { Setup(desktop); }
            catch (Exception e) { Log.Error(e, "setup"); } // NFR-5: no dialogs
        }
        base.OnFrameworkInitializationCompleted();
    }

    void Setup(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var dbPath = Path.Combine(Log.Dir, "clock.db");
        var firstRun = !File.Exists(dbPath);
        var repo = _repo = new ClockRepository(dbPath);
        var auto = new AutoStartService();
        if (firstRun) auto.Enabled = true; // FR-2

        _vm = new MainViewModel(repo);
        _tracker = new AttendanceTracker(repo);
        _tracker.Start();

        var open = new NativeMenuItem("Open");
        open.Click += (_, _) => ShowMain();
        var startup = new NativeMenuItem("Start with Windows")
        {
            ToggleType = NativeMenuItemToggleType.CheckBox,
            IsChecked = auto.Enabled,
        };
        startup.Click += (_, _) =>
        {
            auto.Enabled = !auto.Enabled;
            startup.IsChecked = auto.Enabled; // reflect the real registry state
        };
        var target = new NativeMenuItem("Daily target...");
        target.Click += (_, _) =>
        {
            var w = new TargetWindow(TargetHours());
            w.Saved += h => _repo!.SetSetting("target_hours", h.ToString(CultureInfo.InvariantCulture));
            w.Show();
            w.Activate();
        };
        var exit = new NativeMenuItem("Exit");
        exit.Click += (_, _) =>
        {
            _tracker.Close("exit"); // FR-26
            _tray!.Dispose();
            desktop.Shutdown();
        };

        _tray = new TrayIcon
        {
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://OfficeClock/Assets/tray.ico"))),
            Menu = new NativeMenu { open, startup, target, exit },
            IsVisible = true,
        };
        // ponytail: Avalonia TrayIcon exposes only single-click (Clicked), not double-click.
        _tray.Clicked += (_, _) => ShowMain();
        TrayIcon.SetIcons(this, new TrayIcons { _tray });

        _tracker.Beat += () =>
        {
            UpdateTooltip();
            CheckTarget();
            if (_window?.IsVisible == true) _vm.Refresh(); // FR-21
        };
        UpdateTooltip();

        desktop.Exit += (_, _) => _tracker.Close("exit"); // FR-9

        if (ShowSignal is { } sig)
            new Thread(() =>
            {
                while (sig.WaitOne()) Dispatcher.UIThread.Post(ShowMain);
            }) { IsBackground = true }.Start();
    }

    void ShowMain()
    {
        _window ??= new MainWindow { DataContext = _vm };
        _vm!.Refresh();
        _window.Show();
        if (_window.WindowState == WindowState.Minimized) _window.WindowState = WindowState.Normal;
        _window.Activate();
    }

    double TargetHours() =>
        double.TryParse(_repo!.GetSetting("target_hours"), CultureInfo.InvariantCulture, out var h) ? h : 8;

    /// Notify once per day when the running day reaches the target.
    void CheckTarget()
    {
        try
        {
            if (_tracker?.Today() is not { Reason: null } d) return;
            if (_repo!.GetSetting("notified") == d.DateText) return;
            var hours = TargetHours();
            if (DateTime.Now - d.In < TimeSpan.FromHours(hours)) return;
            _repo.SetSetting("notified", d.DateText);
            new ToastWindow($"Daily target reached ({hours:0.#}h)").Show();
        }
        catch (Exception e) { Log.Error(e, "target"); }
    }

    void UpdateTooltip()
    {
        if (_tray is null || _tracker?.Today() is not { } d) return;
        var t = DateTime.Now - d.In;
        if (t < TimeSpan.Zero) t = TimeSpan.Zero;
        _tray.ToolTipText = $"OfficeClock: In {d.In:HH:mm}, {(int)t.TotalHours}h {t.Minutes}m today";
    }
}
