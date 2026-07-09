using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using qiyana.Models;
using qiyana.Models.MatchData;
using qiyana.Services;
using RelayCommand = CommunityToolkit.Mvvm.Input.RelayCommand;

namespace qiyana.ViewModels;

public partial class MatchHistoryViewModel : ViewModelBase
{
    private readonly LcuApiClient _apiClient;
    private readonly SearchHistoryStorage _storage;
    private bool _searching;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _showHistory;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private MatchHistoryTab? _currentTab;

    [ObservableProperty]
    private bool _noTabs = true;

    public ObservableCollection<MatchHistorySearch> HistoryItems { get; } = [];
    public ObservableCollection<MatchHistoryTab> Tabs { get; } = [];

    partial void OnCurrentTabChanged(MatchHistoryTab? value)
    {
        foreach (var tab in Tabs)
            tab.IsActive = tab == value;
        NoTabs = value is null;
    }

    public MatchHistoryViewModel()
    {
        _apiClient = App.LcuApi;
        _storage = new SearchHistoryStorage();
        LoadHistory();
    }

    private void LoadHistory()
    {
        var history = _storage.Load();
        HistoryItems.Clear();
        foreach (var item in history.OrderByDescending(h => h.LastSearchedAt))
            HistoryItems.Add(item);
    }

    private void PersistHistory()
    {
        try
        {
            _storage.Save([.. HistoryItems]);
        }
        catch
        {
        }
    }

    [RelayCommand]
    private async Task Search()
    {
        var input = SearchText?.Trim();
        if (string.IsNullOrEmpty(input) || _searching) return;

        _searching = true;
        ShowHistory = false;
        ErrorMessage = null;

        try
        {
            SummonerInfo? summoner;

            if (IsPuuidFormat(input))
            {
                summoner = await _apiClient.GetSummonerByPuuidAsync(input);
            }
            else
            {
                summoner = await _apiClient.SearchSummonerAsync(input);
            }

            if (summoner is null)
            {
                ErrorMessage = "Summoner not found";
                return;
            }

            await OpenOrSwitchTab(summoner);
            SearchText = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Search failed: {ex.Message}";
        }
        finally
        {
            _searching = false;
        }
    }

    private static bool IsPuuidFormat(string input)
    {
        // UUID format: 8-4-4-4-12 hex digits, e.g. e880aa4f-4e06-545f-b2be-34aade132f11
        if (input.Length != 36) return false;
        return Guid.TryParse(input, out _);
    }

    [RelayCommand]
    private void SelectHistory(MatchHistorySearch? search)
    {
        if (search is null) return;
        ShowHistory = false;
        _ = OpenOrSwitchTabByPuuid(search.Puuid);
    }

    [RelayCommand]
    private void NavigateToPlayer(string puuid)
    {
        if (string.IsNullOrEmpty(puuid)) return;
        _ = OpenOrSwitchTabByPuuid(puuid);
    }

    [RelayCommand]
    private void ToggleHistory()
    {
        ShowHistory = !ShowHistory;
    }

    [RelayCommand]
    private void CloseTab(MatchHistoryTab? tab)
    {
        if (tab is null) return;

        var index = Tabs.IndexOf(tab);
        Tabs.Remove(tab);

        if (Tabs.Count == 0)
        {
            CurrentTab = null;
        }
        else if (CurrentTab == tab)
        {
            CurrentTab = index < Tabs.Count ? Tabs[index] : Tabs[^1];
        }
    }

    [RelayCommand]
    private void SelectTab(MatchHistoryTab? tab)
    {
        if (tab is not null && tab != CurrentTab)
            CurrentTab = tab;
    }

    [RelayCommand]
    private void CloseHistoryItem(MatchHistorySearch? search)
    {
        if (search is null) return;
        HistoryItems.Remove(search);
        PersistHistory();
    }

    [RelayCommand]
    private async Task LoadMore()
    {
        if (CurrentTab?.LoadMoreAction is not null)
            await CurrentTab.LoadMoreAction();
    }

    private async Task OpenOrSwitchTab(SummonerInfo summoner)
    {
        var existing = Tabs.FirstOrDefault(t =>
            t.Puuid.Equals(summoner.Puuid, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            CurrentTab = existing;
            return;
        }

        if (summoner.ProfileIconPath is not null)
            await App.GameData.LoadIconBytesAsync(summoner.ProfileIconPath);
        if (summoner.RankedEmblemPath is not null)
            await App.GameData.LoadIconBytesAsync(summoner.RankedEmblemPath);
        if (summoner.FlexQueue?.EmblemPath is { } flexPath)
            await App.GameData.LoadIconBytesAsync(flexPath);
        if (summoner.HighestRankedEmblemPath is not null)
            await App.GameData.LoadIconBytesAsync(summoner.HighestRankedEmblemPath);

        var tab = new MatchHistoryTab
        {
            Puuid = summoner.Puuid,
            Summoner = summoner,
        };

        WireTabActions(tab);
        Tabs.Add(tab);
        CurrentTab = tab;

        UpdateHistory(summoner);

        await tab.LoadMatchesAsync();
    }

    private async Task OpenOrSwitchTabByPuuid(string puuid)
    {
        var existing = Tabs.FirstOrDefault(t =>
            t.Puuid.Equals(puuid, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            CurrentTab = existing;
            return;
        }

        try
        {
            var summoner = await _apiClient.GetSummonerByPuuidAsync(puuid);
            if (summoner is null)
            {
                ErrorMessage = "Summoner not found";
                return;
            }
            await OpenOrSwitchTab(summoner);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load: {ex.Message}";
        }
    }

    private void WireTabActions(MatchHistoryTab tab)
    {
        tab.SelectMatchAction = async (entry) =>
        {
            if (entry is null) return;
            try
            {
                var detail = await _apiClient.GetGameDetailAsync(entry.GameId);
                if (detail is null) return;

                await LoadDetailIconsAsync(detail);
                PrepareDetailForDisplay(detail, tab);
                tab.SelectedDetail = detail;
            }
            catch (Exception ex)
            {
                tab.ErrorMessage = $"Failed to load match detail: {ex.Message}";
            }
        };

        tab.FilterCommand = new AsyncRelayCommand<string>(async (filter) =>
        {
            if (string.IsNullOrEmpty(filter) || filter == tab.SelectedFilter) return;

            tab.SelectedFilter = filter;
            tab.CurrentOffset = 0;
            tab.HasMore = true;
            tab.Matches.Clear();
            try
            {
                await tab.LoadMatchesAsync();
            }
            catch (Exception ex)
            {
                tab.ErrorMessage = $"Filter failed: {ex.Message}";
            }
        });

        tab.LoadMoreAction = async () =>
        {
            if (tab.IsLoadingMore || !tab.HasMore) return;

            tab.IsLoadingMore = true;
            try
            {
                await tab.LoadMatchesAsync();
            }
            catch (Exception ex)
            {
                tab.ErrorMessage = $"Load more failed: {ex.Message}";
            }
            finally
            {
                tab.IsLoadingMore = false;
            }
        };
    }

    private void UpdateHistory(SummonerInfo summoner)
    {
        var existing = HistoryItems.FirstOrDefault(s =>
            s.Puuid.Equals(summoner.Puuid, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            existing.GameName = summoner.GameName;
            existing.TagLine = summoner.TagLine;
            existing.LastSearchedAt = DateTime.UtcNow;
            HistoryItems.Remove(existing);
            HistoryItems.Insert(0, existing);
        }
        else
        {
            var search = new MatchHistorySearch
            {
                Puuid = summoner.Puuid,
                GameName = summoner.GameName,
                TagLine = summoner.TagLine,
                LastSearchedAt = DateTime.UtcNow,
            };
            HistoryItems.Insert(0, search);
        }

        PersistHistory();
    }

    private void PrepareDetailForDisplay(GameDetail detail, MatchHistoryTab tab)
    {
        foreach (var p in detail.Participants)
        {
            p.GameDurationCache = detail.GameDuration;
        }

        var blueTeam = detail.TeamBlue;
        var redTeam = detail.TeamRed;

        var blueMaxDamage = blueTeam.Count > 0 ? blueTeam.Max(p => p.Stats?.TotalDamageDealtToChampions ?? 0) : 0;
        if (blueMaxDamage > 0)
        {
            foreach (var p in blueTeam)
            {
                var damage = p.Stats?.TotalDamageDealtToChampions ?? 0;
                p.DamagePercent = (double)damage / blueMaxDamage;
            }
        }

        var redMaxDamage = redTeam.Count > 0 ? redTeam.Max(p => p.Stats?.TotalDamageDealtToChampions ?? 0) : 0;
        if (redMaxDamage > 0)
        {
            foreach (var p in redTeam)
            {
                var damage = p.Stats?.TotalDamageDealtToChampions ?? 0;
                p.DamagePercent = (double)damage / redMaxDamage;
            }
        }

        tab.NotifyTeamTotalsChanged();
    }

    private async Task LoadDetailIconsAsync(GameDetail detail)
    {
        var tasks = new List<Task>();
        foreach (var p in detail.Participants)
        {
            if (p.ChampionId > 0)
            {
                var id = p.ChampionId;
                tasks.Add(Task.Run(async () =>
                {
                    p.ChampionIcon = await App.GameData.LoadIconBytesAsync(
                        GameDataService.GetChampionIconUrl(id));
                }));
            }

            while (p.ItemIcons.Count < 7)
                p.ItemIcons.Add(null);

            if (p.Spell1Id > 0)
            {
                var id = p.Spell1Id;
                tasks.Add(Task.Run(async () =>
                {
                    p.Spell1Icon = await App.GameData.LoadIconBytesAsync(
                        GameDataService.GetSpellIconUrl(id));
                }));
            }

            if (p.Spell2Id > 0)
            {
                var id = p.Spell2Id;
                tasks.Add(Task.Run(async () =>
                {
                    p.Spell2Icon = await App.GameData.LoadIconBytesAsync(
                        GameDataService.GetSpellIconUrl(id));
                }));
            }

            if (p.Stats is not null && p.ItemIcons.Count > 0)
            {
                var itemIds = new[] { p.Stats.Item0, p.Stats.Item1, p.Stats.Item2, p.Stats.Item3, p.Stats.Item4, p.Stats.Item5, p.Stats.Item6 };
                for (int i = 0; i < itemIds.Length; i++)
                {
                    var itemId = itemIds[i];
                    var index = i;
                    if (itemId > 0)
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            var item = App.GameData.Items.GetValueOrDefault(itemId);
                            if (item is null) return;
                            var icon = await App.GameData.LoadIconBytesAsync(item.IconPath);
                            if (index < p.ItemIcons.Count)
                                p.ItemIcons[index] = icon;
                        }));
                    }
                }
            }

            if (p.KeystonePerkId > 0)
            {
                var perkId = p.KeystonePerkId;
                tasks.Add(Task.Run(async () =>
                {
                    var perk = App.GameData.Perks.GetValueOrDefault(perkId);
                    if (perk is not null)
                        p.KeystoneRuneIcon = await App.GameData.LoadIconBytesAsync(perk.IconPath);
                }));
            }

            if (p.PerkPrimaryStyle > 0)
            {
                var styleId = p.PerkPrimaryStyle;
                tasks.Add(Task.Run(async () =>
                {
                    var style = App.GameData.Styles.GetValueOrDefault(styleId);
                    if (style is not null)
                    {
                        p.PrimaryStyleIcon = await App.GameData.LoadIconBytesAsync(style.IconPath);
                        p.PrimaryStyleName = style.Name;
                    }
                }));
            }

            if (p.PerkSubStyle > 0)
            {
                var styleId = p.PerkSubStyle;
                tasks.Add(Task.Run(async () =>
                {
                    var style = App.GameData.Styles.GetValueOrDefault(styleId);
                    if (style is not null)
                    {
                        p.SecondaryStyleIcon = await App.GameData.LoadIconBytesAsync(style.IconPath);
                        p.SecondaryStyleName = style.Name;
                    }
                }));
            }

            if (p.Stats is not null)
            {
                var perkIds = new[] { p.Stats.Perk0, p.Stats.Perk1, p.Stats.Perk2, p.Stats.Perk3, p.Stats.Perk4, p.Stats.Perk5 };
                for (int i = 0; i < perkIds.Length; i++)
                {
                    var perkId = perkIds[i];
                    var index = i;
                    if (perkId > 0)
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            var perk = App.GameData.Perks.GetValueOrDefault(perkId);
                            if (perk is not null)
                            {
                                var icon = await App.GameData.LoadIconBytesAsync(perk.IconPath);
                                while (p.PerkIcons.Count <= index)
                                    p.PerkIcons.Add(null);
                                if (index < p.PerkIcons.Count)
                                    p.PerkIcons[index] = icon;
                            }
                        }));
                    }
                }
            }
        }

        await Task.WhenAll(tasks);

        foreach (var team in detail.Teams)
        {
            foreach (var ban in team.Bans)
            {
                if (ban.ChampionId > 0)
                {
                    var id = ban.ChampionId;
                    tasks.Add(Task.Run(async () =>
                    {
                        ban.ChampionIcon = await App.GameData.LoadIconBytesAsync(
                            GameDataService.GetChampionIconUrl(id));
                    }));
                }
            }
        }

        await Task.WhenAll(tasks);
    }
}
