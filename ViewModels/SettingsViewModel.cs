using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using qiyana.Models.GameData;
using qiyana.Services;

namespace qiyana.ViewModels;

public record ChampionSelectItem(int Id, string DisplayName);

public partial class SettingsViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;
    private AppSettings _appSettings;
    private readonly IReadOnlyDictionary<int, ChampionSummary> _championMap;

    [ObservableProperty]
    private bool _autoAcceptMatch;

    [ObservableProperty]
    private bool _autoSelectEnabled;

    public ObservableCollection<ChampionSelectItem> PickChampionItems { get; } = [];
    public ObservableCollection<ChampionSelectItem> BanChampionItems { get; } = [];
    public List<ChampionSummary> AllChampions { get; }

    public SettingsViewModel()
    {
        _settingsService = App.Settings;
        _appSettings = _settingsService.Load();
        _autoAcceptMatch = _appSettings.AutoAcceptMatch;
        _autoSelectEnabled = _appSettings.AutoSelectEnabled;

        _championMap = App.GameData.Champions;
        AllChampions = _championMap.Values
            .OrderBy(c => c.Name)
            .ToList();

        foreach (var id in _appSettings.AutoPickChampionIds)
            PickChampionItems.Add(MakeItem(id));
        foreach (var id in _appSettings.AutoBanChampionIds)
            BanChampionItems.Add(MakeItem(id));
    }

    partial void OnAutoAcceptMatchChanged(bool value)
    {
        _appSettings.AutoAcceptMatch = value;
        _settingsService.Save(_appSettings);
    }

    partial void OnAutoSelectEnabledChanged(bool value)
    {
        _appSettings.AutoSelectEnabled = value;
        _settingsService.Save(_appSettings);
    }

    private ChampionSelectItem MakeItem(int id)
    {
        var display = _championMap.TryGetValue(id, out var c) ? $"{c.Name} ({c.Alias})" : $"英雄 {id}";
        return new ChampionSelectItem(id, display);
    }

    public void AddPickChampion(int championId)
    {
        if (PickChampionItems.Any(i => i.Id == championId)) return;
        if (PickChampionItems.Count >= 3) return;
        PickChampionItems.Add(MakeItem(championId));
        SavePickChampionIds();
    }

    public void RemovePickChampion(int championId)
    {
        var item = PickChampionItems.FirstOrDefault(i => i.Id == championId);
        if (item is null) return;
        PickChampionItems.Remove(item);
        SavePickChampionIds();
    }

    public void AddBanChampion(int championId)
    {
        if (BanChampionItems.Any(i => i.Id == championId)) return;
        if (BanChampionItems.Count >= 3) return;
        BanChampionItems.Add(MakeItem(championId));
        SaveBanChampionIds();
    }

    public void RemoveBanChampion(int championId)
    {
        var item = BanChampionItems.FirstOrDefault(i => i.Id == championId);
        if (item is null) return;
        BanChampionItems.Remove(item);
        SaveBanChampionIds();
    }

    private void SavePickChampionIds()
    {
        _appSettings.AutoPickChampionIds = PickChampionItems.Select(i => i.Id).ToArray();
        _settingsService.Save(_appSettings);
    }

    private void SaveBanChampionIds()
    {
        _appSettings.AutoBanChampionIds = BanChampionItems.Select(i => i.Id).ToArray();
        _settingsService.Save(_appSettings);
    }
}
