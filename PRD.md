# Product Requirements: OfficeClock

A single-user Windows tray application that automatically records office check-in (Windows start / logon) and check-out (shutdown), and shows a daily attendance grid.

## 1. Goals

- Record attendance with zero manual effort: no button to press to check in or out.
- Run silently from the system tray; the main window opens only on demand.
- Show a simple grid of daily in, out and total hours.
- Be reliable: a crash or power cut must not lose the day's check-out time.

## 2. Non-Goals (v1)

- No multi-user support, login, or server sync.
- No HR or payroll integration.
- No leave, holiday, or overtime rules.
- No Linux or macOS support.
- No editing of records from the UI (may be added later).

## 3. Users and Context

- One user on one Windows PC (Windows 10 or 11, x64).
- The PC is used for office work; Windows start is treated as office check-in and Windows shutdown as check-out.

## 4. Tech Stack

| Concern | Choice |
|---|---|
| Runtime | .NET 8, `net8.0` (Windows) |
| UI | Avalonia 11 (`TrayIcon` plus one window) |
| Grid | `Avalonia.Controls.DataGrid` |
| MVVM | CommunityToolkit.Mvvm |
| Storage | SQLite via `Microsoft.Data.Sqlite` |
| DB location | `%LocalAppData%\OfficeClock\clock.db` |
| Packaging | `dotnet publish -r win-x64 --self-contained -p:PublishSingleFile=true` |

## 5. Functional Requirements

### 5.1 Auto-start

- FR-1: The app registers itself to start at user logon through `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` (no admin rights).
- FR-2: Auto-start is enabled by default on first run.
- FR-3: The tray menu has a checkable item **Start with Windows** to toggle auto-start.

### 5.2 Check-in

- FR-4: On launch, the app records the day's check-in time.
- FR-5: Check-in time is the Windows boot time, calculated as `DateTime.Now - TimeSpan.FromMilliseconds(Environment.TickCount64)`, when the app starts within 10 minutes of boot. Otherwise (for example, the app was started manually later) it is the app start time.
- FR-6: Only the first launch of a day sets that day's check-in. Later launches the same day do not change it.
- FR-7: Only one instance may run at a time (named `Mutex`). A second launch brings the existing window to the front and exits.

### 5.3 Check-out

- FR-8: Check-out is the last time the PC was in use that day.
- FR-9: On shutdown, restart, or logoff (`SystemEvents.SessionEnding` and Avalonia exit), the app writes the check-out time synchronously and quickly.
- FR-10: A heartbeat timer updates the day's `last_seen` time every 60 seconds, so check-out is correct after a crash or power cut. On the next startup, any unclosed day is closed with `out_time = last_seen`.
- FR-11: Check-out is simply the latest recorded time for the day: it is updated by the heartbeat and finalized on shutdown.

### 5.4 Work time rules

- FR-12: **Total hours = Out − In.** Lunch breaks are included.
- FR-13: Time with the screen locked counts as work time.
- FR-14: Sleep and hibernate also count as work time (the day runs from first In to last Out). Lock, unlock, and sleep events therefore do not split the day.
- FR-15: After waking from sleep, the heartbeat resumes and moves Out forward as normal.
- FR-16: If the PC runs past midnight, the day's record ends at 23:59:59 and a new record begins at 00:00:00 the next day. Both are counted.

### 5.5 Main window (grid)

- FR-17: The window shows a `DataGrid` with these columns:

| Column | Content |
|---|---|
| Date | `yyyy-MM-dd` |
| Day | Mon, Tue, ... |
| In | `HH:mm` of check-in |
| Out | `HH:mm` of check-out (shows "Running" for today while the app is active) |
| Total Hours | Out − In as `H:mm` |

- FR-18: A month picker selects which month to display; the default is the current month.
- FR-19: A footer shows the month's total hours and the number of days worked.
- FR-20: Rows are sorted by date, newest first.
- FR-21: Today's row updates live (about every 60 seconds) while the window is open.
- FR-22: Closing the window hides it; the app keeps running in the tray.

### 5.6 Tray icon

- FR-23: A tray icon is always visible while the app runs, with a tooltip like `OfficeClock: In 09:02, 5h 14m today`.
- FR-24: Double-clicking the icon opens the main window.
- FR-25: The context menu contains: **Open**, **Start with Windows** (checkable), **Exit**.
- FR-26: **Exit** performs a final check-out write and quits.

## 6. Data Model

Table `Days`:

| Column | Type | Notes |
|---|---|---|
| `work_date` | TEXT (PK) | `yyyy-MM-dd` |
| `in_time` | TEXT | ISO 8601 local time |
| `out_time` | TEXT | ISO 8601, last known out time |
| `last_seen` | TEXT | Latest heartbeat |
| `out_reason` | TEXT | `shutdown`, `exit`, `unexpected`, `midnight`, or null while running |

Derived on read: Day name, Total = `out_time − in_time`.

## 7. Project Structure

```
OfficeClock/
  OfficeClock.sln
  src/OfficeClock/
    App.axaml(.cs)             // tray icon, lifetime, single-instance guard
    Program.cs
    Models/DayRecord.cs
    Data/ClockRepository.cs    // SQLite access, unclosed-day recovery
    Services/
      AutoStartService.cs      // registry Run key on/off
      AttendanceTracker.cs     // check-in, heartbeat, shutdown hooks, midnight rollover
      BootTimeProvider.cs
    ViewModels/MainViewModel.cs
    Views/MainWindow.axaml(.cs)
    Assets/tray.ico
```

## 8. Non-Functional Requirements

- NFR-1: Idle RAM under 100 MB and negligible CPU.
- NFR-2: Starts to tray in under 3 seconds with no visible window.
- NFR-3: Shutdown write completes in under 500 ms; use a single short SQLite write with WAL off, or a flush with no pending transaction.
- NFR-4: All times are stored as local time; a system clock or time-zone change must not corrupt data (a negative total is shown as `0:00`).
- NFR-5: Errors are logged to `%LocalAppData%\OfficeClock\log.txt` (simple rolling file). The app never shows error dialogs at startup.

## 9. Build Phases

1. **Skeleton:** Avalonia app, tray icon with menu, hidden main window, single-instance guard.
2. **Storage:** SQLite repository, check-in, heartbeat, unclosed-day recovery.
3. **Tracking:** shutdown, logoff, and exit hooks; midnight rollover.
4. **Grid UI:** month picker, `DataGrid`, footer totals, live refresh.
5. **Auto-start:** registry toggle, on by default.
6. **Packaging:** single-file publish, optional Inno Setup installer.

## 10. Acceptance Criteria

- [ ] Reboot the PC: after logon the tray icon appears and today's In matches boot time.
- [ ] Shutdown, restart, and logoff each leave an Out time for the day.
- [ ] Pulling power leaves Out at the last heartbeat (at most 60 seconds off) on the next start.
- [ ] Locking the screen, or sleeping the PC for an hour, still counts in the total.
- [ ] Second launch of the app does not create a duplicate record or a second tray icon.
- [ ] Restarting twice in one day keeps the first In and the latest Out.
- [ ] Total Hours always equals Out − In.
- [ ] Unchecking **Start with Windows** removes the registry entry; checking it adds it back.
- [ ] Closing the window keeps the app in the tray; **Exit** fully quits.

## 11. Open Items (future versions)

- Manual correction of In or Out times.
- Export to CSV or Excel.
- Daily working-hours target and a notification when it is reached.
- Optional sync to a Blazor + PostgreSQL backend.
