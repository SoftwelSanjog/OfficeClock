using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OfficeClock.Data;
using OfficeClock.Models;
using OfficeClock.Services;

namespace OfficeClock.ViewModels;

public partial class MainViewModel : ObservableObject
{
    readonly ClockRepository _repo;

    [ObservableProperty] DateTime _month = new(DateTime.Now.Year, DateTime.Now.Month, 1);
    [ObservableProperty] string _footer = "";

    public ObservableCollection<DayRecord> Rows { get; } = new();
    public string MonthLabel => Month.ToString("MMMM yyyy");

    public MainViewModel(ClockRepository repo)
    {
        _repo = repo;
        Refresh();
    }

    partial void OnMonthChanged(DateTime value)
    {
        OnPropertyChanged(nameof(MonthLabel));
        Refresh();
    }

    [RelayCommand] void PrevMonth() => Month = Month.AddMonths(-1);
    [RelayCommand] void NextMonth() => Month = Month.AddMonths(1);

    public void Save(DayRecord d, TimeSpan inT, TimeSpan outT)
    {
        try
        {
            if (d.Reason is null) _repo.SetIn(d.Date, d.Date + inT);
            else _repo.SetTimes(d.Date, d.Date + inT, d.Date + outT);
        }
        catch (Exception e) { Log.Error(e, "save"); }
        Refresh();
    }

    public void Delete(DayRecord d)
    {
        try { _repo.Delete(d.Date); }
        catch (Exception e) { Log.Error(e, "delete"); }
        Refresh();
    }

    public void Refresh()
    {
        try
        {
            var days = _repo.Month(Month);
            Rows.Clear();
            foreach (var d in days) Rows.Add(d);
            var total = days.Aggregate(TimeSpan.Zero, (a, d) => a + d.Total);
            Footer = $"Total: {DayRecord.Format(total)} h  |  {days.Count} days worked";
        }
        catch (Exception e) { Log.Error(e, "refresh"); }
    }
}
