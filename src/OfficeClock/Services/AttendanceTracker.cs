using Avalonia.Threading;
using Microsoft.Win32;
using OfficeClock.Data;
using OfficeClock.Models;

namespace OfficeClock.Services;

public sealed class AttendanceTracker
{
    readonly ClockRepository _repo;
    readonly object _gate = new();
    readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(60) };
    DateTime _day;
    bool _closed;

    /// Raised on the UI thread after every heartbeat.
    public event Action? Beat;

    public AttendanceTracker(ClockRepository repo) => _repo = repo;

    public void Start()
    {
        lock (_gate)
        {
            try
            {
                _repo.RecoverUnclosed();
                var now = DateTime.Now;
                _day = now.Date;
                if (_repo.Get(_day) is null) _repo.Insert(_day, BootTimeProvider.CheckIn(now), now); // FR-4..6
                else _repo.Resume(_day, now);
            }
            catch (Exception e) { Log.Error(e, "start"); }
        }
        SystemEvents.SessionEnding += (_, _) => Close("shutdown"); // FR-9
        _timer.Tick += (_, _) => Tick();
        _timer.Start();
    }

    public DayRecord? Today()
    {
        lock (_gate)
        {
            try { return _repo.Get(_day); }
            catch (Exception e) { Log.Error(e, "today"); return null; }
        }
    }

    /// FR-9/FR-26: first close wins; later calls (e.g. exit after shutdown) are no-ops.
    public void Close(string reason)
    {
        lock (_gate)
        {
            if (_closed) return;
            _closed = true;
            try { _repo.Close(_day, DateTime.Now, reason); }
            catch (Exception e) { Log.Error(e, "close"); }
        }
    }

    void Tick()
    {
        lock (_gate)
        {
            if (_closed) return;
            try
            {
                var now = DateTime.Now;
                if (now.Date != _day)
                {
                    // FR-16: day ends 23:59:59, next day starts 00:00:00.
                    _repo.Close(_day, _day.AddDays(1).AddSeconds(-1), "midnight");
                    _day = now.Date;
                    if (_repo.Get(_day) is null) _repo.Insert(_day, _day, now);
                    else _repo.Resume(_day, now);
                }
                else _repo.Heartbeat(_day, now);
            }
            catch (Exception e) { Log.Error(e, "tick"); }
        }
        Beat?.Invoke();
    }
}
