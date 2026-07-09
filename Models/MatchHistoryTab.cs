using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using qiyana.Models.MatchData;
using qiyana.Services;

namespace qiyana.Models;

public partial class MatchHistoryTab : ObservableObject
{
    public string Puuid { get; set; } = string.Empty;

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
    private bool _isActive;

    [ObservableProperty]
    private GameDetail? _selectedMatch;

    [ObservableProperty]
    private GameDetail? _selectedDetail;

    [ObservableProperty]
    private Participant? _selectedParticipant;

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

    partial void OnSelectedMatchChanged(GameDetail? value)
    {
        if (value is not null)
            SelectMatchAction?.Invoke(value);
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

    public int CurrentOffset;
    public const int PageSize = 11;

    public static readonly Dictionary<string, string?> SgpTags = new()
    {
        ["All"] = null,
        ["SoloDuo"] = "q_420",
        ["Flex"] = "q_440",
        ["Normal"] = "q_430",
        ["ARAM"] = "q_450",
        ["HextechAram"] = "q_2400",
    };

    public Action<GameDetail>? SelectMatchAction { get; set; }
    public CommunityToolkit.Mvvm.Input.IRelayCommand<string>? FilterCommand { get; set; }
    public Func<Task>? LoadMoreAction { get; set; }

    public async Task LoadMatchesAsync()
    {
        IsLoading = true;
        try
        {
            var puuid = Puuid;
            if (string.IsNullOrEmpty(puuid)) return;

            var tag = SgpTags.GetValueOrDefault(SelectedFilter);
            var batch = await App.SgpApi.GetMatchHistoryAsync(puuid, CurrentOffset, PageSize, tag, puuid);
            if (batch.Count > 0)
            {
                await LoadChampionIconsAsync(batch);
                foreach (var entry in batch)
                    Matches.Add(entry);
                CurrentOffset += PageSize;
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
        finally
        {
            IsLoading = false;
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
}
