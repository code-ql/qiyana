using System;
using System.IO;
using System.Text.Json;

namespace qiyana.Services;

public class AppSettings
{
    public bool AutoAcceptMatch { get; set; }
    public bool AutoSelectEnabled { get; set; }
    public int[] AutoPickChampionIds { get; set; } = [];
    public int[] AutoBanChampionIds { get; set; } = [];
}

public class SettingsService
{
    private readonly string _filePath;
    private AppSettings _settings = new();

    public SettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dir = Path.Combine(appData, "qiyana");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "settings.json");
        Load();
    }

    public AppSettings Load()
    {
        try
        {
            var json = File.ReadAllText(_filePath);
            _settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new();
        }
        catch
        {
            _settings = new();
        }
        return _settings;
    }

    public void Save(AppSettings settings)
    {
        _settings = settings;
        var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }
}
