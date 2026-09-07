using System;
using System.IO;
using System.Text.Json;

namespace Creature.Settings;

public class SettingsManager
{
    private readonly string _filePath;
    public UserSettings CurrentSettings { get; private set; }

    public event Action<UserSettings>? SettingsChanged;

    public SettingsManager(string? customPath = null)
    {
        _filePath = customPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Creature",
            "settings.json"
        );
        CurrentSettings = Load();
    }

    public UserSettings Load()
    {
        if (!File.Exists(_filePath))
        {
            CurrentSettings = new UserSettings();
            return CurrentSettings;
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            CurrentSettings = JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
        }
        catch
        {
            CurrentSettings = new UserSettings();
        }

        return CurrentSettings;
    }

    public void Save(UserSettings settings)
    {
        CurrentSettings = settings;
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
        SettingsChanged?.Invoke(CurrentSettings);
    }
}
