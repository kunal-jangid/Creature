using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace Creature.SystemIntegration;

public static class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "Desktop Pets";

    public static bool SetStartup(bool enable, string? customExePath = null)
    {
        try
        {
            if (enable)
            {
                var exePath = customExePath ?? Environment.ProcessPath;
                if (string.IsNullOrEmpty(exePath))
                {
                    exePath = Process.GetCurrentProcess().MainModule?.FileName;
                }

                if (string.IsNullOrEmpty(exePath))
                {
                    exePath = AppContext.BaseDirectory;
                }

                if (!string.IsNullOrEmpty(exePath))
                {
                    using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
                    if (key == null) return false;

                    key.SetValue(AppName, $"\"{exePath}\"");
                    // Clean up legacy key if present
                    key.DeleteValue("Creature", throwOnMissingValue: false);
                    return true;
                }
                return false;
            }
            else
            {
                using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
                key?.DeleteValue(AppName, throwOnMissingValue: false);
                key?.DeleteValue("Creature", throwOnMissingValue: false);
                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    public static bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            return key?.GetValue(AppName) != null || key?.GetValue("Creature") != null;
        }
        catch
        {
            return false;
        }
    }
}

