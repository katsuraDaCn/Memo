using Microsoft.Win32;

namespace Memo.Services;

/// <summary>开机自启：写当前用户注册表 Run 键（无需管理员权限），任务管理器“启动应用”里可见、可关。</summary>
public static class StartupService
{
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string ValueName = "Memo";

    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(ValueName) is string v && v.Contains(AppPath(), StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception) { return false; }
    }

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (key == null) throw new InvalidOperationException("无法打开注册表启动项。");
        if (enabled) key.SetValue(ValueName, $"\"{AppPath()}\"");
        else key.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    static string AppPath() => Environment.ProcessPath ?? Application.ExecutablePath;
}
