using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using OfficeClock.Models;

namespace OfficeClock.Views;

public sealed class EditWindow : Window
{
    public TimeSpan In { get; private set; }
    public TimeSpan Out { get; private set; }

    public EditWindow(DayRecord d)
    {
        Title = $"Edit {d.DateText}";
        Width = 280;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var running = d.Reason is null; // running day: Out belongs to the heartbeat
        var tin = new TextBox { Text = d.In.ToString("HH:mm") };
        var tout = new TextBox { Text = d.Out.ToString("HH:mm"), IsEnabled = !running };
        var err = new TextBlock { Foreground = Brushes.Red };
        var ok = new Button { Content = "Save" };
        var cancel = new Button { Content = "Cancel" };

        ok.Click += (_, _) =>
        {
            const string F = @"h\:mm";
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            if (!TimeSpan.TryParseExact(tin.Text?.Trim(), F, inv, out var i) ||
                !TimeSpan.TryParseExact(tout.Text?.Trim(), F, inv, out var o))
            {
                err.Text = "Use HH:mm (24-hour)";
                return;
            }
            if (running ? i >= DateTime.Now.TimeOfDay : i >= o)
            {
                err.Text = running ? "In must be before now" : "In must be before Out";
                return;
            }
            In = i;
            Out = o;
            Close(true);
        };
        cancel.Click += (_, _) => Close(false);

        Content = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 8,
            Children =
            {
                new TextBlock { Text = "In (HH:mm)" }, tin,
                new TextBlock { Text = "Out (HH:mm)" }, tout,
                err,
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { ok, cancel } },
            },
        };
    }
}

public sealed class ConfirmWindow : Window
{
    public ConfirmWindow(string text)
    {
        Title = "Confirm";
        Width = 300;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var yes = new Button { Content = "Delete" };
        var no = new Button { Content = "Cancel" };
        yes.Click += (_, _) => Close(true);
        no.Click += (_, _) => Close(false);
        Content = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 12,
            Children =
            {
                new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap },
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { yes, no } },
            },
        };
    }
}

public sealed class TargetWindow : Window
{
    public event Action<double>? Saved;

    public TargetWindow(double hours)
    {
        Title = "Daily target";
        Width = 260;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        var num = new NumericUpDown { Minimum = 0.5m, Maximum = 24, Increment = 0.5m, FormatString = "0.0", Value = (decimal)hours };
        var ok = new Button { Content = "Save" };
        ok.Click += (_, _) =>
        {
            Saved?.Invoke((double)(num.Value ?? 8m));
            Close();
        };
        Content = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 8,
            Children = { new TextBlock { Text = "Working hours per day" }, num, ok },
        };
    }
}

/// Bottom-right popup; Avalonia has no native toast. ponytail: swap for Windows toasts if this feels crude.
public sealed class ToastWindow : Window
{
    public ToastWindow(string text)
    {
        Width = 300;
        Height = 80;
        Topmost = true;
        ShowInTaskbar = false;
        CanResize = false;
        SystemDecorations = SystemDecorations.BorderOnly;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Content = new TextBlock
        {
            Text = text,
            Margin = new Thickness(16),
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
        };
        Opened += (_, _) =>
        {
            if (Screens.Primary is { } s)
            {
                var wa = s.WorkingArea;
                Position = new PixelPoint(wa.Right - (int)(Width * s.Scaling) - 16, wa.Bottom - (int)(Height * s.Scaling) - 16);
            }
            var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(8) };
            t.Tick += (_, _) => { t.Stop(); Close(); };
            t.Start();
        };
    }
}
