using System.Globalization;
using Microsoft.Data.Sqlite;
using OfficeClock.Models;

namespace OfficeClock.Data;

public sealed class ClockRepository
{
    readonly string _cs;

    public ClockRepository(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        _cs = $"Data Source={path}";
        Exec("""
            CREATE TABLE IF NOT EXISTS Days (
              work_date  TEXT PRIMARY KEY,
              in_time    TEXT NOT NULL,
              out_time   TEXT NOT NULL,
              last_seen  TEXT NOT NULL,
              out_reason TEXT)
            """);
        Exec("CREATE TABLE IF NOT EXISTS Settings (key TEXT PRIMARY KEY, value TEXT NOT NULL)");
    }

    public string? GetSetting(string key)
    {
        using var c = new SqliteConnection(_cs);
        c.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT value FROM Settings WHERE key = $k";
        cmd.Parameters.AddWithValue("$k", key);
        return cmd.ExecuteScalar() as string;
    }

    public void SetSetting(string key, string value) =>
        Exec("INSERT INTO Settings VALUES ($k, $v) ON CONFLICT(key) DO UPDATE SET value = $v", ("$k", key), ("$v", value));

    /// Running day: only In is editable; the heartbeat keeps owning Out.
    public void SetIn(DateTime day, DateTime inTime) =>
        Exec("UPDATE Days SET in_time = $i WHERE work_date = $d", ("$d", D(day)), ("$i", T(inTime)));

    /// Closed day. 'manual' is not NULL, so heartbeat and recovery never touch it.
    public void SetTimes(DateTime day, DateTime inTime, DateTime outTime) =>
        Exec("UPDATE Days SET in_time = $i, out_time = $o, last_seen = $o, out_reason = 'manual' WHERE work_date = $d AND out_reason IS NOT NULL",
            ("$d", D(day)), ("$i", T(inTime)), ("$o", T(outTime)));

    static string D(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    static string T(DateTime t) => t.ToString("s", CultureInfo.InvariantCulture);
    static DateTime P(string s) => DateTime.Parse(s, CultureInfo.InvariantCulture);

    /// FR-10: days left open by a crash or power cut end at their last heartbeat.
    public void RecoverUnclosed() =>
        Exec("UPDATE Days SET out_time = last_seen, out_reason = 'unexpected' WHERE out_reason IS NULL");

    public DayRecord? Get(DateTime day) => Query("SELECT * FROM Days WHERE work_date = $d", ("$d", D(day))).FirstOrDefault();

    public void Insert(DateTime day, DateTime inTime, DateTime now) =>
        Exec("INSERT INTO Days VALUES ($d, $i, $n, $n, NULL)", ("$d", D(day)), ("$i", T(inTime)), ("$n", T(now)));

    /// Heartbeat / resume: moves Out forward; reopens a day closed earlier by shutdown or exit.
    public void Resume(DateTime day, DateTime now) =>
        Exec("UPDATE Days SET out_time = $n, last_seen = $n, out_reason = NULL WHERE work_date = $d",
            ("$d", D(day)), ("$n", T(now)));

    public void Heartbeat(DateTime day, DateTime now) =>
        Exec("UPDATE Days SET out_time = $n, last_seen = $n WHERE work_date = $d AND out_reason IS NULL",
            ("$d", D(day)), ("$n", T(now)));

    public void Close(DateTime day, DateTime at, string reason) =>
        Exec("UPDATE Days SET out_time = $n, last_seen = $n, out_reason = $r WHERE work_date = $d AND out_reason IS NULL",
            ("$d", D(day)), ("$n", T(at)), ("$r", reason));

    /// Closed days only; the running day belongs to the tracker.
    public void Delete(DateTime day) =>
        Exec("DELETE FROM Days WHERE work_date = $d AND out_reason IS NOT NULL", ("$d", D(day)));

    public List<DayRecord> Month(DateTime month) =>
        Query("SELECT * FROM Days WHERE work_date >= $a AND work_date < $b ORDER BY work_date DESC",
            ("$a", D(new DateTime(month.Year, month.Month, 1))),
            ("$b", D(new DateTime(month.Year, month.Month, 1).AddMonths(1))));

    void Exec(string sql, params (string, string)[] args)
    {
        using var c = new SqliteConnection(_cs);
        c.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (k, v) in args) cmd.Parameters.AddWithValue(k, v);
        cmd.ExecuteNonQuery();
    }

    List<DayRecord> Query(string sql, params (string, string)[] args)
    {
        using var c = new SqliteConnection(_cs);
        c.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (k, v) in args) cmd.Parameters.AddWithValue(k, v);
        using var r = cmd.ExecuteReader();
        var list = new List<DayRecord>();
        while (r.Read())
            list.Add(new DayRecord
            {
                Date = P(r.GetString(0)),
                In = P(r.GetString(1)),
                Out = P(r.GetString(2)),
                Reason = r.IsDBNull(4) ? null : r.GetString(4),
            });
        return list;
    }
}
