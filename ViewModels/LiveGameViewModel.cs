using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using qiyana.Models;
using qiyana.Services;

namespace qiyana.ViewModels;

public partial class LiveGameViewModel : ViewModelBase
    {
        private readonly Services.LcuWebSocketClient _ws;
        private bool _isFetching;
        private long _currentGameId;

    [ObservableProperty] private string _currentPhase = "None";

    [ObservableProperty] private TeammateDisplayInfo? _bestPlayer;

    [ObservableProperty] private TeammateDisplayInfo? _worstPlayer;

    [ObservableProperty] private List<TeammateDisplayInfo> _otherPlayers = [];

    [ObservableProperty] private TeammateDisplayInfo? _otherPlayer1;

    [ObservableProperty] private TeammateDisplayInfo? _otherPlayer2;

    [ObservableProperty] private TeammateDisplayInfo? _otherPlayer3;

    [ObservableProperty] private TeammateDisplayInfo? _enemyBestPlayer;

    [ObservableProperty] private TeammateDisplayInfo? _enemyWorstPlayer;

    [ObservableProperty] private List<TeammateDisplayInfo> _enemyOtherPlayers = [];

    [ObservableProperty] private bool _hasData;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateAiTeamReviewCommand))]
    private bool _isAiTeamReviewLoading;

    [ObservableProperty] private string _aiTeamReviewText = "等待队友战绩加载后，可点击 AI 生成团队评价。";
    [ObservableProperty] private string _aiTeamReviewSummary = "-";
    [ObservableProperty] private string _aiTeamWinRatePrediction = "-";
    [ObservableProperty] private string _aiTeamFocusPlayer = "-";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLoadingMatchHistory))]
    [NotifyPropertyChangedFor(nameof(MatchHistoryLoadingText))]
    [NotifyPropertyChangedFor(nameof(MatchHistoryLoadingProgress))]
    private int _matchHistoryLoadedCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLoadingMatchHistory))]
    [NotifyPropertyChangedFor(nameof(MatchHistoryLoadingText))]
    [NotifyPropertyChangedFor(nameof(MatchHistoryLoadingProgress))]
    private int _matchHistoryTotalCount;

    public bool IsLoadingMatchHistory => MatchHistoryTotalCount > 0 && MatchHistoryLoadedCount < MatchHistoryTotalCount;
    public string MatchHistoryLoadingText => MatchHistoryTotalCount > 0
        ? $"加载战绩 {MatchHistoryLoadedCount}/{MatchHistoryTotalCount}"
        : "";
    public double MatchHistoryLoadingProgress => MatchHistoryTotalCount > 0
        ? (double)MatchHistoryLoadedCount / MatchHistoryTotalCount * 100
        : 0;

    public double TeamAverageSkillScore => EnumerateAllPlayers()
        .Where(p => p.HasMatchData)
        .Select(p => p.SkillScore)
        .DefaultIfEmpty(0)
        .Average();

    public double TeamPredictedWinRate => TeamAverageSkillScore <= 0
        ? 0
        : Math.Clamp(50 + (TeamAverageSkillScore - 50) * 0.6, 35, 65);

    public string TeamAverageSkillScoreText => TeamAverageSkillScore > 0 ? TeamAverageSkillScore.ToString("F0") : "-";

    public string TeamPredictedWinRateText => TeamPredictedWinRate > 0 ? $"{TeamPredictedWinRate:F0}%" : "-";

    [ObservableProperty] private MatchQueueFilter? _selectedMatchQueueFilter;

    public string AutoSelectStatusText => App.Settings.Load().AutoSelectEnabled ? "已启用" : "已禁用";
    public Avalonia.Media.ISolidColorBrush AutoSelectStatusBrush => new Avalonia.Media.SolidColorBrush(
        App.Settings.Load().AutoSelectEnabled
            ? Avalonia.Media.Color.FromArgb(255, 76, 175, 80)
            : Avalonia.Media.Color.FromArgb(255, 255, 107, 107));

    public string AutoPickChampionNames
    {
        get
        {
            var s = App.Settings.Load();
            return s.AutoPickChampionIds.Length == 0
                ? "-"
                : string.Join(" ", s.AutoPickChampionIds.Select(id => App.GameData.Champions.GetValueOrDefault(id)?.Name ?? $"id{id}"));
        }
    }

    public string AutoBanChampionNames
    {
        get
        {
            var s = App.Settings.Load();
            return s.AutoBanChampionIds.Length == 0
                ? "-"
                : string.Join(" ", s.AutoBanChampionIds.Select(id => App.GameData.Champions.GetValueOrDefault(id)?.Name ?? $"id{id}"));
        }
    }

    public ObservableCollection<MatchQueueFilter> MatchQueueFilters { get; } =
    [
        new("SoloDuo", "单双排位", "q_420", 420),
        new("Flex", "灵活排位", "q_440", 440),
        new("Normal", "匹配模式", "q_430", 430),
        new("ARAM", "极地大乱斗", "q_450", 450),
        new("HextechAram", "海克斯大乱斗", "q_2400", 2400),
    ];

    public LiveGameViewModel()
    {
        _ws = App.LcuWebSocket;
        _ws.GameflowPhaseChanged += OnGameflowPhaseChanged;
        _ws.ChampSelectSessionChanged += OnChampSelectSessionChanged;
        _ws.Reconnected += OnWebSocketReconnected;

        if (!_ws.IsConnected)
            _ = _ws.StartAsync();

        _ = InitializeAsync();
    }

    partial void OnSelectedMatchQueueFilterChanged(MatchQueueFilter? value)
    {
        if (value is null || !HasData || _isFetching) return;

        ClearAiTeamReview();
        _ = RefreshCurrentPlayersMatchHistoryAsync();
    }

    private async void OnWebSocketReconnected()
    {
        await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            var rawPhase = await App.LcuApi.GetJsonStringAsync("/lol-gameflow/v1/gameflow-phase");
            if (rawPhase is not null)
            {
                var phase = JsonSerializer.Deserialize<string>(rawPhase) ?? rawPhase;
                Dispatcher.UIThread.Post(() => CurrentPhase = phase);
                if (phase == "ChampSelect")
                {
                    await ApplyCurrentQueueFilterAsync();
                    await FetchSessionAsync();
                }
            }
        }
        catch
        {
        }
    }

    private void OnGameflowPhaseChanged(string phase)
    {
        Dispatcher.UIThread.Post(() =>
        {
            CurrentPhase = phase;
            _currentGameId = 0;
            BestPlayer = null;
            WorstPlayer = null;
            OtherPlayers = [];
            OtherPlayer1 = null;
            OtherPlayer2 = null;
            OtherPlayer3 = null;
            EnemyBestPlayer = null;
            EnemyWorstPlayer = null;
            EnemyOtherPlayers = [];
            HasData = false;
            ClearAiTeamReview();
        });

        if (phase == "ChampSelect")
            _ = ApplyCurrentQueueFilterAsync();

        if (phase == "ReadyCheck")
        {
            var settings = App.Settings.Load();
            if (settings.AutoAcceptMatch)
            {
                _ = App.LcuApi.PostAsync("/lol-matchmaking/v1/ready-check/accept");
            }
        }
    }

    private async void OnChampSelectSessionChanged(JsonElement data)
    {
        data = data.Clone(); // 克隆以避免原 JsonDocument 在 async 后被释放
        await App.AutoSelect.ProcessSessionAsync(data, App.Settings.Load());
        TrySaveRankedSession(data);

        if (_isFetching) return;
        _isFetching = true;
        try
        {
            var newGameId = data.TryGetProperty("gameId", out var g) ? g.GetInt64() : 0;

            if (_currentGameId == 0 || _currentGameId != newGameId)
            {
                _currentGameId = newGameId;
                if (TryParseSession(data, out var players))
                    await ApplySessionDataAsync(players);
            }
            else
            {
                UpdateMutableFields(data);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LiveGame] Error: {ex.Message}");
        }
        finally
        {
            _isFetching = false;
        }
    }

    private async Task FetchSessionAsync()
    {
        if (_isFetching) return;
        try
        {
            var json = await App.LcuApi.GetJsonStringAsync("/lol-champ-select/v1/session");
            if (json is null) return;
            using var doc = JsonDocument.Parse(json);
            _isFetching = true;
            try
            {
                if (TryParseSession(doc.RootElement, out var players))
                    await ApplySessionDataAsync(players);
            }
            finally
            {
                _isFetching = false;
            }
        }
        catch
        {
            _isFetching = false;
        }
    }

    private static bool TryParseSession(JsonElement data, out List<TeammateDisplayInfo> players)
    {
        players = [];

        if (!data.TryGetProperty("myTeam", out var myTeam))
            return false;

        var localCellId = data.TryGetProperty("localPlayerCellId", out var lc)
            ? lc.GetInt32()
            : -1;

        foreach (var m in myTeam.EnumerateArray())
        {
            var summonerId = m.GetProperty("summonerId").GetInt64();
            var gameName = m.GetProperty("gameName").GetString() ?? "";

            if (summonerId == 0 && string.IsNullOrEmpty(gameName))
                continue;

            var cellId = m.GetProperty("cellId").GetInt32();
            var championId = m.GetProperty("championId").GetInt32();
            string championName = "";
            if (championId > 0)
                championName = App.GameData.Champions.GetValueOrDefault(championId)?.Name ?? "";
            players.Add(new TeammateDisplayInfo
            {
                SummonerName = gameName,
                TagLine = m.GetProperty("tagLine").GetString() ?? "",
                Puuid = m.GetProperty("puuid").GetString() ?? "",
                CellId = cellId,
                ChampionId = championId,
                ChampionName = championName,
                AssignedPosition = m.GetProperty("assignedPosition").GetString() ?? "",
                Spell1Id = m.GetProperty("spell1Id").GetInt32(),
                Spell2Id = m.GetProperty("spell2Id").GetInt32(),
                IsAutofilled = m.GetProperty("isAutofilled").GetBoolean(),
                SelectedSkinId = m.GetProperty("selectedSkinId").GetInt32(),
                IsLocalPlayer = cellId == localCellId,
            });
        }

        return players.Count > 0;
    }

    private async Task ApplyCurrentQueueFilterAsync()
    {
        try
        {
            var json = await App.LcuApi.GetJsonStringAsync("/lol-gameflow/v1/session");
            if (json is null) return;

            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("gameData", out var gameData)) return;
            if (!gameData.TryGetProperty("queue", out var queue)) return;
            if (!queue.TryGetProperty("id", out var id)) return;

            var filter = MatchQueueFilters.FirstOrDefault(f => f.QueueId == id.GetInt32());
            if (filter is not null)
                await Dispatcher.UIThread.InvokeAsync(() => SelectedMatchQueueFilter = filter);
        }
        catch
        {
        }
    }

    private async Task ApplySessionDataAsync(List<TeammateDisplayInfo> players)
    {
        await Dispatcher.UIThread.InvokeAsync(ClearAiTeamReview);
        await FetchProfilesAsync(players);
        ApplyPlayerRanking(players);
    }

    private bool CanGenerateAiTeamReview() => HasData && !IsAiTeamReviewLoading;

    [RelayCommand(CanExecute = nameof(CanGenerateAiTeamReview))]
    private async Task GenerateAiTeamReviewAsync()
    {
        if (IsAiTeamReviewLoading) return;

        var players = EnumerateAllPlayers().ToList();
        if (players.Count == 0)
        {
            AiTeamReviewText = "暂无队友数据，无法生成 AI 团队评价。";
            return;
        }

        IsAiTeamReviewLoading = true;
        AiTeamReviewText = "AI 分析中...";
        try
        {
            var review = await App.AgnesAiTeamReview.GenerateReviewAsync(
                players,
                TeamPredictedWinRate,
                TeamAverageSkillScore);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                AiTeamReviewText = "AI 团队评价已生成。";
                AiTeamReviewSummary = review.TeamReview;
                AiTeamWinRatePrediction = review.WinRatePrediction;
                AiTeamFocusPlayer = review.FocusPlayer;
            });
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() => IsAiTeamReviewLoading = false);
        }
    }

    private void BeginMatchHistoryLoading(int total)
    {
        Dispatcher.UIThread.Post(() =>
        {
            MatchHistoryLoadedCount = 0;
            MatchHistoryTotalCount = total;
        });
    }

    private void IncrementMatchHistoryLoading()
    {
        Dispatcher.UIThread.Post(() => MatchHistoryLoadedCount++);
    }

    private void ApplyPlayerRanking(List<TeammateDisplayInfo> players)
    {
        var ranked = players
            .Select(p =>
            {
                var score = CalculateSkillScore(p);
                p.SkillScore = score;
                return (Player: p, Score: score);
            })
            .OrderByDescending(x => x.Score)
            .Select(x => x.Player)
            .ToList();

        Dispatcher.UIThread.Post(() =>
        {
            BestPlayer = ranked.Count > 0 ? ranked[0] : null;
            WorstPlayer = ranked.Count > 1 ? ranked[^1] : null;
            OtherPlayers = ranked.Count > 2 ? ranked[1..^1].ToList() : [];

            var others = OtherPlayers;
            OtherPlayer1 = others.Count > 0 ? others[0] : null;
            OtherPlayer2 = others.Count > 1 ? others[1] : null;
            OtherPlayer3 = others.Count > 2 ? others[2] : null;

            HasData = true;
            NotifyTeamPredictionChanged();
            GenerateAiTeamReviewCommand.NotifyCanExecuteChanged();
        });
    }

    [RelayCommand]
    private async Task RefreshCurrentPlayersMatchHistoryAsync()
    {
        if (_isFetching) return;

        var players = EnumerateAllPlayers().ToList();
        if (players.Count == 0) return;

        _isFetching = true;
        try
        {
            ClearAiTeamReview();
            BeginMatchHistoryLoading(players.Count);
            await Task.WhenAll(players.Select(async p =>
            {
                await FetchMatchHistoryAsync(p);
                IncrementMatchHistoryLoading();
            }));
            ApplyPlayerRanking(players);
        }
        finally
        {
            _isFetching = false;
        }
    }

    private static double CalculateSkillScore(TeammateDisplayInfo p)
    {
        if (!p.HasMatchData)
            return 0;

        var total = p.TotalGames;
        var winRate = (double)p.Wins / total;
        var kda = p.KdaRatio;
        var averageDeaths = (double)p.Deaths / total;

        var winRateScore = winRate * 60;
        var kdaScore = Math.Min(kda, 6.0) / 6.0 * 30;
        var confidenceScore = Math.Min(total / 10.0, 1.0) * 10;
        var deathPenalty = Math.Max(averageDeaths - 6.0, 0) * 3;

        return winRateScore + kdaScore + confidenceScore - deathPenalty;
    }

    private void UpdateMutableFields(JsonElement data)
    {
        if (!data.TryGetProperty("myTeam", out var myTeam))
            return;

        var updates = new Dictionary<int, (int ChampionId, string Position, int Spell1Id, int Spell2Id)>();
        foreach (var m in myTeam.EnumerateArray())
        {
            var summonerId = m.GetProperty("summonerId").GetInt64();
            var gameName = m.GetProperty("gameName").GetString() ?? "";
            if (summonerId == 0 && string.IsNullOrEmpty(gameName))
                continue;
            var cellId = m.GetProperty("cellId").GetInt32();
            updates[cellId] = (
                m.GetProperty("championId").GetInt32(),
                m.GetProperty("assignedPosition").GetString() ?? "",
                m.GetProperty("spell1Id").GetInt32(),
                m.GetProperty("spell2Id").GetInt32()
            );
        }

        if (updates.Count == 0) return;

        Dispatcher.UIThread.Post(async () =>
        {
            foreach (var p in EnumerateAllPlayers())
            {
                if (!updates.TryGetValue(p.CellId, out var u)) continue;

                p.ChampionId = u.ChampionId;
                p.AssignedPosition = u.Position;
                p.Spell1Id = u.Spell1Id;
                p.Spell2Id = u.Spell2Id;

                string championName = "";
                if (u.ChampionId > 0)
                    championName = App.GameData.Champions.GetValueOrDefault(u.ChampionId)?.Name ?? "";
                p.ChampionName = championName;

                await LoadSpellIconsAsync(p);

                p.NotifyChampSelectFieldsChanged();
            }
        });
    }

    private void TrySaveRankedSession(JsonElement data)
    {
        if (SelectedMatchQueueFilter?.QueueId is not (420 or 440))
            return;

        var gameId = data.TryGetProperty("gameId", out var g) ? g.GetInt64() : 0;
        var dir = @"D:\qiyana\qiyana\e\lcu-doc\排位玩家";
        Directory.CreateDirectory(dir);
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        var path = System.IO.Path.Combine(dir, $"{gameId}_{timestamp}.json");
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    private void NotifyTeamPredictionChanged()
    {
        OnPropertyChanged(nameof(TeamAverageSkillScore));
        OnPropertyChanged(nameof(TeamPredictedWinRate));
        OnPropertyChanged(nameof(TeamAverageSkillScoreText));
        OnPropertyChanged(nameof(TeamPredictedWinRateText));
    }

    private void ClearAiTeamReview()
    {
        AiTeamReviewText = HasData
            ? "点击 AI 生成团队评价。"
            : "等待队友战绩加载后，可点击 AI 生成团队评价。";
        AiTeamReviewSummary = "-";
        AiTeamWinRatePrediction = "-";
        AiTeamFocusPlayer = "-";
    }

    private static async Task LoadSpellIconsAsync(TeammateDisplayInfo p)
    {
        if (p.Spell1Id > 0)
        {
            var path = GameDataService.GetSpellIconUrl(p.Spell1Id);
            await App.GameData.LoadIconBytesAsync(path);
        }
        if (p.Spell2Id > 0)
        {
            var path = GameDataService.GetSpellIconUrl(p.Spell2Id);
            await App.GameData.LoadIconBytesAsync(path);
        }
    }

    private IEnumerable<TeammateDisplayInfo> EnumerateAllPlayers()
    {
        if (BestPlayer is not null) yield return BestPlayer;
        if (WorstPlayer is not null) yield return WorstPlayer;
        if (OtherPlayers is not null)
            foreach (var p in OtherPlayers)
                yield return p;
    }

    private async Task FetchProfilesAsync(List<TeammateDisplayInfo> players)
    {
        BeginMatchHistoryLoading(players.Count);
        var tasks = players.Select(async p =>
        {
            try
            {
                var info = await App.LcuApi.GetSummonerByPuuidAsync(p.Puuid);
                if (info is not null)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        p.SoloQueue = info.SoloQueue;
                        p.FlexQueue = info.FlexQueue;
                        p.ProfileIconId = info.ProfileIconId;
                    });

                    if (info.ProfileIconId > 0)
                    {
                        var path = GameDataService.GetProfileIconUrl(info.ProfileIconId);
                        await App.GameData.LoadIconBytesAsync(path);
                    }
                }

                if (p.Spell1Id > 0)
                {
                    var path = GameDataService.GetSpellIconUrl(p.Spell1Id);
                    await App.GameData.LoadIconBytesAsync(path);
                }
                if (p.Spell2Id > 0)
                {
                    var path = GameDataService.GetSpellIconUrl(p.Spell2Id);
                    await App.GameData.LoadIconBytesAsync(path);
                }

                await FetchMatchHistoryAsync(p);
            }
            catch
            {
                // skip — card renders without avatar/rank/history
            }
            finally
            {
                IncrementMatchHistoryLoading();
            }
        });

        await Task.WhenAll(tasks);
    }

    private async Task FetchMatchHistoryAsync(TeammateDisplayInfo player)
    {
        var puuid = player.Puuid;
        if (string.IsNullOrEmpty(puuid)) return;

        var games = await App.SgpApi.GetMatchHistoryAsync(puuid, 0, 10, SelectedMatchQueueFilter?.SgpTag, puuid);

        int wins = 0, losses = 0;
        int kills = 0, deaths = 0, assists = 0;

        var iconTasks = new List<Task>();

        foreach (var game in games)
        {
            var p = game.CurrentParticipant;
            if (p?.Stats is null) continue;

            if (p.Stats.Win) wins++;
            else losses++;
            kills += p.Stats.Kills;
            deaths += p.Stats.Deaths;
            assists += p.Stats.Assists;

            if (p.ChampionId > 0)
            {
                var id = p.ChampionId;
                iconTasks.Add(Task.Run(async () =>
                {
                    p.ChampionIcon = await App.GameData.LoadIconBytesAsync(
                        GameDataService.GetChampionIconUrl(id));
                }));
            }

            if (p.Spell1Id > 0)
            {
                var id = p.Spell1Id;
                iconTasks.Add(Task.Run(async () =>
                {
                    p.Spell1Icon = await App.GameData.LoadIconBytesAsync(
                        GameDataService.GetSpellIconUrl(id));
                }));
            }

            if (p.Spell2Id > 0)
            {
                var id = p.Spell2Id;
                iconTasks.Add(Task.Run(async () =>
                {
                    p.Spell2Icon = await App.GameData.LoadIconBytesAsync(
                        GameDataService.GetSpellIconUrl(id));
                }));
            }
        }

        await Task.WhenAll(iconTasks);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            player.RecentGames = games;
            player.Wins = wins;
            player.Losses = losses;
            player.Kills = kills;
            player.Deaths = deaths;
            player.Assists = assists;
        });
    }

}
