using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using qiyana.Models;

namespace qiyana.Services;

public class SearchHistoryStorage
{
    private readonly string _filePath;

    public SearchHistoryStorage()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dir = Path.Combine(appData, "qiyana");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "search-history.json");
    }

    public List<MatchHistorySearch> Load()
    {
        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<MatchHistorySearch>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public void Save(List<MatchHistorySearch> history)
    {
        var json = JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }
}
