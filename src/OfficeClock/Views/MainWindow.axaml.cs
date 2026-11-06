using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using OfficeClock.Models;
using OfficeClock.ViewModels;

namespace OfficeClock.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        // FR-22: closing hides; real shutdown (Exit / OS) is let through.
        Closing += (_, e) =>
        {
            if (e.CloseReason == WindowCloseReason.WindowClosing)
            {
                e.Cancel = true;
                Hide();
            }
        };
    }

    void OnGridKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete) DeleteSelected();
    }

    void OnDeleteClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => DeleteSelected();

    async void DeleteSelected()
    {
        if (Grid.SelectedItem is not DayRecord d || DataContext is not MainViewModel vm) return;
        if (d.Reason is null) return; // running day can't be deleted
        if (await new ConfirmWindow($"Delete record for {d.DateText}?").ShowDialog<bool>(this)) vm.Delete(d);
    }

    async void OnRowDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is not Visual v || v.FindAncestorOfType<DataGridRow>() is null) return; // ignore header taps
        if (Grid.SelectedItem is not DayRecord d || DataContext is not MainViewModel vm) return;
        var dlg = new EditWindow(d);
        if (await dlg.ShowDialog<bool>(this)) vm.Save(d, dlg.In, dlg.Out);
    }
}
