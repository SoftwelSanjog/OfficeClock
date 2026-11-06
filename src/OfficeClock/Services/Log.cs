namespace OfficeClock.Services;

public static class Log
{
    public static readonly string Dir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OfficeClock");

    static readonly string File_ = Path.Combine(Dir, "log.txt");
    static readonly object Gate = new();

    public static void Error(Exception e, string context = "")
    {
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(Dir);
                var f = new FileInfo(File_);
                if (f.Exists && f.Length > 512 * 1024) f.MoveTo(Path.Combine(Dir, "log.old.txt"), true);
                File.AppendAllText(File_, $"{DateTime.Now:s} {context} {e}{Environment.NewLine}");
            }
        }
        catch { /* NFR-5: logging must never surface errors */ }
    }
}
