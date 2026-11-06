# OfficeClock

A lightweight Windows tray app that automatically logs office check-in and check-out from your office PC, so you always know your working hours without pressing a button.

## Why

Attendance and working hours are usually tracked by hand: remembering to note when you arrived, when you left, and adding it up at the end of the month. On an office PC, the machine already knows when your day starts and ends. OfficeClock treats **Windows start as check-in** and **Windows shutdown as check-out**, and keeps a simple daily record of it.

## Features

- **Automatic check-in** at launch, using the Windows boot time when the app starts within 10 minutes of boot. Only the first launch of a day sets the day's In time.
- **Automatic check-out** on shutdown, restart, logoff or exit.
- **Crash and power-cut safe.** A 60-second heartbeat keeps the day's Out time current, and any unclosed day is closed at its last heartbeat on the next start.
- **Daily grid** with Date, Day, In, Out and Total Hours, a month selector, and a footer with the month's total hours and days worked. Today's row updates live.
- **Edit and delete records.** Double-click a row to correct In or Out. Press Delete or right-click to remove a past day.
- **Daily target notification.** Set your working hours per day and get a popup once when you reach them.
- **Runs from the system tray.** Closing the window keeps the app running. The tray menu has Open, Start with Windows, Daily target and Exit.
- **Starts with Windows** (per user, no admin rights, on by default).
- **Single instance.** A second launch brings the existing window to the front.

### Work time rules

Total hours = Out − In. Lunch breaks, locked screen, sleep and hibernate all count as work time. If the PC runs past midnight, the day ends at 23:59:59 and a new record starts at 00:00:00.

## Tech stack

.NET 8, Avalonia 11 (tray icon, window, DataGrid), CommunityToolkit.Mvvm, SQLite via Microsoft.Data.Sqlite.

## Build and run

Requires the .NET 8 SDK on Windows 10 or 11 (x64).

```
dotnet run --project src/OfficeClock
```

Single-file release build:

```
dotnet publish src/OfficeClock -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

## Data

Stored locally in `%LocalAppData%\OfficeClock\`:

- `clock.db`: the attendance records (SQLite)
- `log.txt`: error log

Nothing leaves your PC.

## Known limits

- Single user, single PC. No server sync.
- With Windows Fast Startup enabled, the boot time reported by Windows can be inaccurate, which may affect the detected In time.
- The tray icon opens the window on a single click, not a double-click.

## Roadmap

- Export to CSV or Excel
- Optional sync to a Blazor and PostgreSQL backend

## Developer

Er. Sanjog Shakya
