using Avalonia;
using Avalonia.Controls;
using OfficeClock.Services;

namespace OfficeClock;

static class Program
{
    const string ShowEvent = @"Local\OfficeClock.Show";

    [STAThread]
    public static void Main(string[] args)
    {
        using var mutex = new Mutex(true, @"Local\OfficeClock.Instance", out var first);
        if (!first)
        {
            try { EventWaitHandle.OpenExisting(ShowEvent).Set(); } catch { }
            return;
        }
        using var show = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEvent);
        App.ShowSignal = show;
        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, ShutdownMode.OnExplicitShutdown);
        }
        catch (Exception e) { Log.Error(e, "fatal"); }
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect();
}
