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

namespace qiyana.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly LcuApiClient _apiClient;
    private string? _currentPuuid;

    [ObservableProperty]
    private SummonerInfo? _summoner;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isLoadingMore;

    [ObservableProperty]
    private string _selectedFilter = "All";

    [ObservableProperty]
    private bool _hasMore = true;

    [ObservableProperty]
    private GameDetail? _selectedMatch;

    partial void OnSelectedMatchChanged(GameDetail? value)
    {
        if (value is not null)
            SelectMatchCommand.Execute(value);
    }

    [ObservableProperty]
    private GameDetail? _selectedDetail;

    public bool ShowDetailPanel => SelectedDetail is not null;

    partial void OnSelectedDetailChanged(GameDetail? value)
    {
        OnPropertyChanged(nameof(ShowDetailPanel));
        OnPropertyChanged(nameof(TeamBlueWinLabel));
        OnPropertyChanged(nameof(TeamRedWinLabel));
        OnPropertyChanged(nameof(TeamBlueIsWin));
        OnPropertyChanged(nameof(TeamRedIsWin));
        SelectedParticipant = null;
        if (value is not null)
            App.GameData.ResolveParticipantNames(value);
    }

    [ObservableProperty]
    private Participant? _selectedParticipant;

    partial void OnSelectedParticipantChanged(Participant? value)
    {
    }

    public int TeamBlueTotalKills => SelectedDetail?.TeamBlueTotalKills ?? 0;
    public int TeamRedTotalKills => SelectedDetail?.TeamRedTotalKills ?? 0;

    public string TeamBlueWinLabel => SelectedDetail?.Teams is { Count: > 0 } && SelectedDetail.Teams[0].Win == "Win" ? "胜利" : "失败";
    public string TeamRedWinLabel => SelectedDetail?.Teams is { Count: > 1 } && SelectedDetail.Teams[1].Win == "Win" ? "胜利" : "失败";

    public bool TeamBlueIsWin => SelectedDetail?.Teams is { Count: > 0 } && SelectedDetail.Teams[0].Win == "Win";
    public bool TeamRedIsWin => SelectedDetail?.Teams is { Count: > 1 } && SelectedDetail.Teams[1].Win == "Win";

    public void NotifyTeamTotalsChanged()
    {
        OnPropertyChanged(nameof(TeamBlueTotalKills));
        OnPropertyChanged(nameof(TeamRedTotalKills));
    }

    public void ToggleParticipantDetail(Participant? participant)
    {
        SelectedParticipant = SelectedParticipant == participant ? null : participant;
    }

    public ObservableCollection<GameDetail> Matches { get; } = [];
    public ObservableCollection<string> Filters { get; } = ["All", "SoloDuo", "Flex", "Normal", "ARAM"];

    private int _currentOffset;
    private const int PageSize = 11;

    private static readonly Dictionary<string, string?> SgpTags = new()
    {
        ["All"] = null,
        ["SoloDuo"] = "q_420",
        ["Flex"] = "q_440",
        ["Normal"] = "q_430",
        ["ARAM"] = "q_450",
        ["HextechAram"] = "q_2400",
    };

    public DashboardViewModel()
    {
        _apiClient = App.LcuApi;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var info = App.LcuDiscovery.GetLcuInfo();
        if (info is null)
        {
            ErrorMessage = "Not connected to League Client";
            return;
        }

        IsLoading = true;
        _ = App.GameData.LoadAsync();

        try
        {
            var summoner = await _apiClient.GetCurrentSummonerAsync();
            if (summoner is null)
            {
                ErrorMessage = "Failed to load summoner info";
                return;
            }

            _currentPuuid = summoner.Puuid;

            if (summoner.ProfileIconPath is not null)
                await App.GameData.LoadIconBytesAsync(summoner.ProfileIconPath);
            if (summoner.RankedEmblemPath is not null)
                await App.GameData.LoadIconBytesAsync(summoner.RankedEmblemPath);
            if (summoner.FlexQueue?.EmblemPath is { } flexPath)
                await App.GameData.LoadIconBytesAsync(flexPath);
            if (summoner.HighestRankedEmblemPath is not null)
                await App.GameData.LoadIconBytesAsync(summoner.HighestRankedEmblemPath);

            Summoner = summoner;

            _currentOffset = 0;
            HasMore = true;
            await LoadMatchesAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadMore()
    {
        if (IsLoadingMore || !HasMore || string.IsNullOrEmpty(_currentPuuid)) return;

        IsLoadingMore = true;
        try
        {
            await LoadMatchesAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Load more failed: {ex.Message}";
        }
        finally
        {
            IsLoadingMore = false;
        }
    }

    [RelayCommand]
    private async Task Filter(string filter)
    {
        if (string.IsNullOrEmpty(filter) || filter == SelectedFilter) return;

        SelectedFilter = filter;
        _currentOffset = 0;
        HasMore = true;
        Matches.Clear();
        try
        {
            await LoadMatchesAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Filter failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SelectMatch(GameDetail? entry)
    {
        if (entry is null) return;

        try
        {
            var detail = await _apiClient.GetGameDetailAsync(entry.GameId);
            if (detail is null) return;

            await LoadDetailIconsAsync(detail);
            PrepareDetailForDisplay(detail);
            SelectedDetail = detail;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load match detail: {ex.Message}";
        }
    }

    private void PrepareDetailForDisplay(GameDetail detail)
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

        NotifyTeamTotalsChanged();
    }

    private async Task LoadMatchesAsync()
    {
        var puuid = _currentPuuid;
        if (string.IsNullOrEmpty(puuid)) return;

        var tag = SgpTags.GetValueOrDefault(SelectedFilter);
        var batch = await App.SgpApi.GetMatchHistoryAsync(puuid, _currentOffset, PageSize, tag, puuid);
        if (batch.Count > 0)
        {
            await LoadChampionIconsAsync(batch);
            foreach (var entry in batch)
                Matches.Add(entry);
            _currentOffset += PageSize;
            if (batch.Count < PageSize)
                HasMore = false;

            if (Matches.Count > 0 && SelectedMatch is null)
                SelectedMatch = Matches[0];
        }
        else
        {
            HasMore = false;
        }
    }

    private async Task LoadChampionIconsAsync(List<GameDetail> entries)
    {
        var tasks = entries.SelectMany(entry =>
        {
            var list = new List<Task>();
            var p = entry.CurrentParticipant;
            if (p is null) return list;

            if (p.ChampionId > 0)
            {
                var id = p.ChampionId;
                list.Add(Task.Run(async () =>
                {
                    p.ChampionIcon = await App.GameData.LoadIconBytesAsync(
                        GameDataService.GetChampionIconUrl(id));
                }));
            }

            foreach (var itemId in p.Items)
            {
                if (itemId <= 0) continue;
                var id = itemId;
                list.Add(Task.Run(async () =>
                {
                    var item = App.GameData.Items.GetValueOrDefault(id);
                    if (item is null) return;
                    var bytes = await App.GameData.LoadIconBytesAsync(item.IconPath);
                    p.ItemIcons.Add(bytes);
                }));
            }

            return list;
        });

        await Task.WhenAll(tasks);
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

            // Initialize item icons list with 7 null slots
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
                                // ensure list has enough slots
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
