using Microsoft.Win32;

namespace OfficeClock.Services;

public sealed class AutoStartService
{
    const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string ValueName = "OfficeClock";

    public bool Enabled
    {
        get
        {
            try
            {
                using var k = Registry.CurrentUser.OpenSubKey(KeyPath);
                return k?.GetValue(ValueName) is not null;
            }
            catch (Exception e) { Log.Error(e, "autostart read"); return false; }
        }
        set
        {
            try
            {
                using var k = Registry.CurrentUser.CreateSubKey(KeyPath);
                if (value) k.SetValue(ValueName, $"\"{Environment.ProcessPath}\"");
                else k.DeleteValue(ValueName, false);
            }
            catch (Exception e) { Log.Error(e, "autostart write"); }
        }
    }
}
