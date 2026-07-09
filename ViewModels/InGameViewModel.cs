using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using qiyana.Models;
using qiyana.Services;

namespace qiyana.ViewModels;

public partial class InGameViewModel : ViewModelBase
{
    private readonly Services.LcuWebSocketClient _ws;
    private bool _isFetching;
    private bool _isInProgress;
    private long _currentGameId;
    private long _lastGameId;
    private string _localPuuid = "";
    private CancellationTokenSource? _liveGamePollingCts;
    private Task? _liveGamePollingTask;
    private Dictionary<string, int>? _championNameToId;
    private LiveAllGameData? _latestLiveData;

    [ObservableProperty] private string _currentPhase = "None";

    [ObservableProperty] private bool _hasData;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateAiMatchupReviewCommand))]
    private bool _isAiMatchupReviewLoading;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateAiWinRateCommand))]
    private bool _isAiWinRateLoading;

    [ObservableProperty] private string _aiWinRateText = "-";

    [ObservableProperty] private string _aiMatchupStatusText = "进入游戏并加载双方战绩后，可点击 AI 生成团队评价。";
    [ObservableProperty] private string _aiTeamReviewSummary = "-";
    [ObservableProperty] private string _aiTeamWinRatePrediction = "-";
    [ObservableProperty] private string _aiTeamFocusPlayer = "-";
    [ObservableProperty] private string _aiEnemyThreat = "-";

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

    [ObservableProperty] private MatchQueueFilter? _selectedMatchQueueFilter;

    public double MyTeamAverageSkillScore => MyPlayers
        .Where(p => !string.IsNullOrEmpty(p.Puuid) && p.HasMatchData)
        .Select(p => p.SkillScore)
        .DefaultIfEmpty(0)
        .Average();

    public double EnemyTeamAverageSkillScore => EnemyPlayers
        .Where(p => !string.IsNullOrEmpty(p.Puuid) && p.HasMatchData)
        .Select(p => p.SkillScore)
        .DefaultIfEmpty(0)
        .Average();

    public double TeamPredictedWinRate
    {
        get
        {
            var myScore = MyTeamAverageSkillScore;
            if (myScore <= 0) return 0;

            var enemyScore = EnemyTeamAverageSkillScore;
            if (enemyScore <= 0)
                return Math.Clamp(50 + (myScore - 50) * 0.6, 35, 65);

            return Math.Clamp(50 + (myScore - enemyScore) * 0.7, 25, 75);
        }
    }

    public string MyTeamAverageSkillScoreText => MyTeamAverageSkillScore > 0 ? MyTeamAverageSkillScore.ToString("F0") : "-";
    public string EnemyTeamAverageSkillScoreText => EnemyTeamAverageSkillScore > 0 ? EnemyTeamAverageSkillScore.ToString("F0") : "-";
    public string TeamPredictedWinRateText => TeamPredictedWinRate > 0 ? $"{TeamPredictedWinRate:F0}%" : "-";

    [ObservableProperty] private string _myTeamTotalKda = "-";
    [ObservableProperty] private string _enemyTeamTotalKda = "-";

    public ObservableCollection<byte[]?> MyTeamChampionIcons { get; } = [];
    public ObservableCollection<byte[]?> EnemyTeamChampionIcons { get; } = [];

    public ObservableCollection<MatchQueueFilter> MatchQueueFilters { get; } =
    [
        new("SoloDuo", "单双排位", "q_420", 420),
        new("Flex", "灵活排位", "q_440", 440),
        new("Normal", "匹配模式", "q_430", 430),
        new("ARAM", "极地大乱斗", "q_450", 450),
        new("HextechAram", "海克斯大乱斗", "q_2400", 2400),
    ];

    public ObservableCollection<TeammateDisplayInfo> MyPlayers { get; } = [];
    public ObservableCollection<TeammateDisplayInfo> EnemyPlayers { get; } = [];

    public InGameViewModel()
    {
        _ws = App.LcuWebSocket;
        _ws.GameflowPhaseChanged += OnGameflowPhaseChanged;
        _ws.GameflowSessionChanged += OnGameflowSessionChanged;
        _ws.Reconnected += OnWebSocketReconnected;

        if (!_ws.IsConnected)
            _ = _ws.StartAsync();

        _ = InitializeAsync();
    }

    partial void OnSelectedMatchQueueFilterChanged(MatchQueueFilter? value)
    {
        if (value is null || !HasData || _isFetching) return;

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
            var summoner = await App.LcuApi.GetCurrentSummonerAsync();
            if (summoner is not null)
                _localPuuid = summoner.Puuid;

            var rawPhase = await App.LcuApi.GetJsonStringAsync("/lol-gameflow/v1/gameflow-phase");
            if (rawPhase is not null)
            {
                var phase = JsonSerializer.Deserialize<string>(rawPhase) ?? rawPhase;
                Dispatcher.UIThread.Post(() => CurrentPhase = phase);
                if (phase == "InProgress")
                {
                    await FetchSessionAsync();
                    StartLiveGamePolling();
                }
            }
        }
        catch
        {
        }
    }

    private void OnGameflowPhaseChanged(string phase)
    {
        var wasInProgress = _isInProgress;
        _isInProgress = phase == "InProgress";

        Dispatcher.UIThread.Post(() =>
        {
            CurrentPhase = phase;
            if (phase != "InProgress")
            {
                _currentGameId = 0;
                ClearTeamSummaries();
            }
            ClearPlayers();
        });

        if (phase == "InProgress")
        {
            _ = FetchSessionAsync();
            StartLiveGamePolling();
        }
        else if (wasInProgress)
        {
            StopLiveGamePolling();
        }
    }

    private async void OnGameflowSessionChanged(JsonElement data)
    {
        if (!_isInProgress) return;
        if (_isFetching) return;
        _isFetching = true;
        try
        {
            if (!data.TryGetProperty("gameData", out var gameData)) return;
            ApplyQueueFilter(gameData);
            var newGameId = gameData.TryGetProperty("gameId", out var g) ? g.GetInt64() : 0;

            if (_currentGameId == 0 || _currentGameId != newGameId)
            {
                _currentGameId = newGameId;
                _lastGameId = newGameId;
                if (TryParseSession(data, out var myTeam, out var enemyTeam))
                    await ApplySessionDataAsync(myTeam, enemyTeam);
                else
                    await Dispatcher.UIThread.InvokeAsync(ClearPlayers);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[InGame] Error: {ex.Message}");
        }
        finally
        {
            _isFetching = false;
        }
    }

    private async Task FetchSessionAsync()
    {
        if (_isFetching) return;
        _isFetching = true;
        try
        {
            var summoner = await App.LcuApi.GetCurrentSummonerAsync();
            if (summoner is not null)
                _localPuuid = summoner.Puuid;

            var json = await App.LcuApi.GetJsonStringAsync("/lol-gameflow/v1/session");
            if (json is null) return;
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("gameData", out var gd) &&
                gd.TryGetProperty("gameId", out var g))
            {
                var gameId = g.GetInt64();
                if (_lastGameId != 0 && gameId == _lastGameId)
                    return; // Stale session from previous game
                _currentGameId = gameId;
                _lastGameId = gameId;
            }

            if (TryParseSession(doc.RootElement, out var myTeam, out var enemyTeam))
                await ApplySessionDataAsync(myTeam, enemyTeam);
            else
                await Dispatcher.UIThread.InvokeAsync(ClearPlayers);
        }
        catch
        {
        }
        finally
        {
            _isFetching = false;
        }
    }

    private bool TryParseSession(JsonElement data, out List<TeammateDisplayInfo> myTeam, out List<TeammateDisplayInfo> enemyTeam)
    {
        myTeam = [];
        enemyTeam = [];

        if (!data.TryGetProperty("gameData", out var gameData)) return false;
        ApplyQueueFilter(gameData);
        if (!gameData.TryGetProperty("teamOne", out var teamOne)) return false;
        if (!gameData.TryGetProperty("teamTwo", out var teamTwo)) return false;

        var spellMap = new Dictionary<string, (int Spell1Id, int Spell2Id, int SkinIndex)>();
        if (gameData.TryGetProperty("playerChampionSelections", out var selections))
        {
            foreach (var s in selections.EnumerateArray())
            {
                var puuid = s.GetProperty("puuid").GetString() ?? "";
                if (string.IsNullOrEmpty(puuid)) continue;
                spellMap[puuid] = (
                    s.GetProperty("spell1Id").GetInt32(),
                    s.GetProperty("spell2Id").GetInt32(),
                    s.GetProperty("selectedSkinIndex").GetInt32()
                );
            }
        }

        var teamOnePlayers = ParseTeamPlayers(teamOne, spellMap);
        var teamTwoPlayers = ParseTeamPlayers(teamTwo, spellMap);

        if (teamOnePlayers.Count + teamTwoPlayers.Count == 0) return false;

        bool localInTeamOne = teamOnePlayers.Any(p => p.Puuid == _localPuuid);
        bool localInTeamTwo = teamTwoPlayers.Any(p => p.Puuid == _localPuuid);

        if (localInTeamOne)
        {
            myTeam = teamOnePlayers;
            enemyTeam = teamTwoPlayers;
        }
        else
        {
            myTeam = teamTwoPlayers;
            enemyTeam = teamOnePlayers;
        }

        SortTeam(myTeam);
        SortTeam(enemyTeam);

        return myTeam.Count > 0;
    }

    private void ApplyQueueFilter(JsonElement gameData)
    {
        if (!gameData.TryGetProperty("queue", out var queue)) return;
        if (!queue.TryGetProperty("id", out var id)) return;

        var filter = MatchQueueFilters.FirstOrDefault(f => f.QueueId == id.GetInt32());
        if (filter is not null && filter != SelectedMatchQueueFilter)
            Dispatcher.UIThread.Post(() => SelectedMatchQueueFilter = filter);
    }

    private static List<TeammateDisplayInfo> ParseTeamPlayers(JsonElement team, Dictionary<string, (int Spell1Id, int Spell2Id, int SkinIndex)> spellMap)
    {
        var players = new List<TeammateDisplayInfo>();
        foreach (var m in team.EnumerateArray())
        {
            var puuid = m.GetProperty("puuid").GetString() ?? "";
            if (string.IsNullOrEmpty(puuid)) continue;

            var championId = m.GetProperty("championId").GetInt32();
            string championName = "";
            if (championId > 0)
                championName = App.GameData.Champions.GetValueOrDefault(championId)?.Name ?? "";

            var (spell1Id, spell2Id, skinIndex) = spellMap.GetValueOrDefault(puuid);

            players.Add(new TeammateDisplayInfo
            {
                Puuid = puuid,
                SummonerName = m.TryGetProperty("summonerName", out var sn) ? sn.GetString() ?? "" : "",
                ChampionId = championId,
                ChampionName = championName,
                AssignedPosition = (m.TryGetProperty("selectedPosition", out var sp) ? sp.GetString() ?? "" : "").ToLowerInvariant(),
                Spell1Id = spell1Id,
                Spell2Id = spell2Id,
                SelectedSkinId = skinIndex,
                ProfileIconId = m.TryGetProperty("profileIconId", out var pi) ? pi.GetInt32() : 0,
            });
        }
        return players;
    }

    private void SortTeam(List<TeammateDisplayInfo> players)
    {
        var positionOrder = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["top"] = 0,
            ["jungle"] = 1,
            ["middle"] = 2,
            ["bottom"] = 3,
            ["support"] = 4,
            ["utility"] = 4,
        };

        players.Sort((a, b) =>
        {
            int pa = positionOrder.GetValueOrDefault(a.AssignedPosition, 5);
            int pb = positionOrder.GetValueOrDefault(b.AssignedPosition, 5);
            return pa.CompareTo(pb);
        });
    }

    private async Task ApplySessionDataAsync(List<TeammateDisplayInfo> myTeam, List<TeammateDisplayInfo> enemyTeam)
    {
        await Dispatcher.UIThread.InvokeAsync(ClearAiMatchupReview);
        var allPlayers = myTeam.Concat(enemyTeam).ToList();
        await FetchProfilesAsync(allPlayers);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            ReplaceTeam(MyPlayers, myTeam);
            ReplaceTeam(EnemyPlayers, enemyTeam);

            HasData = true;
            UpdateTeamSummariesFromSessionData();
            NotifyTeamPredictionChanged();
            GenerateAiMatchupReviewCommand.NotifyCanExecuteChanged();
            GenerateAiWinRateCommand.NotifyCanExecuteChanged();
        });

        await TryFetchInitialLiveDataAsync();
    }

    private async Task TryFetchInitialLiveDataAsync()
    {
        for (var i = 0; i < 5; i++)
        {
            try
            {
                    var (data, status, error) = await App.LiveGameData.FetchAllGameDataWithStatusAsync();
                if (data?.AllPlayers is { Count: > 0 })
                {
                    _latestLiveData = data;
                    await Dispatcher.UIThread.InvokeAsync(() => ApplyLiveGameData(data));
                    return;
                }
            }
            catch
            {
            }
            await Task.Delay(2000);
        }
    }

    private bool CanGenerateAiMatchupReview() => HasData && !IsAiMatchupReviewLoading;
    private bool CanGenerateAiWinRate() => HasData && !IsAiWinRateLoading;

    [RelayCommand(CanExecute = nameof(CanGenerateAiMatchupReview))]
    private async Task GenerateAiMatchupReviewAsync()
    {
        if (IsAiMatchupReviewLoading) return;

        var myPlayers = MyPlayers.Where(p => !string.IsNullOrWhiteSpace(p.Puuid)).ToList();
        var enemyPlayers = EnemyPlayers.Where(p => !string.IsNullOrWhiteSpace(p.Puuid)).ToList();
        if (myPlayers.Count == 0)
        {
            AiMatchupStatusText = "暂无己方数据，无法生成 AI 团队评价。";
            return;
        }

        IsAiMatchupReviewLoading = true;
        AiMatchupStatusText = "AI 分析中...";
        try
        {
            var review = await App.AgnesAiTeamReview.GenerateMatchupReviewAsync(
                myPlayers,
                enemyPlayers,
                TeamPredictedWinRate,
                MyTeamAverageSkillScore,
                EnemyTeamAverageSkillScore);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                AiMatchupStatusText = "AI 团队评价已生成。";
                AiTeamReviewSummary = review.TeamReview;
                AiTeamWinRatePrediction = review.WinRatePrediction;
                AiTeamFocusPlayer = review.FocusPlayer;
                AiEnemyThreat = review.EnemyThreat;
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                AiMatchupStatusText = $"AI 请求失败：{ex.Message}";
            });
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() => IsAiMatchupReviewLoading = false);
        }
    }

    [RelayCommand(CanExecute = nameof(CanGenerateAiWinRate))]
    private async Task GenerateAiWinRateAsync()
    {
        if (IsAiWinRateLoading) return;

        var myPlayers = MyPlayers.Where(p => !string.IsNullOrWhiteSpace(p.Puuid)).ToList();
        var enemyPlayers = EnemyPlayers.Where(p => !string.IsNullOrWhiteSpace(p.Puuid)).ToList();
        if (myPlayers.Count == 0) return;

        IsAiWinRateLoading = true;
        AiWinRateText = "AI分析中...";
        try
        {
            var review = await App.AgnesAiTeamReview.GenerateMatchupReviewWithLiveDataAsync(
                myPlayers,
                enemyPlayers,
                TeamPredictedWinRate,
                MyTeamAverageSkillScore,
                EnemyTeamAverageSkillScore,
                _latestLiveData);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                AiWinRateText = review.WinRatePrediction;
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                AiWinRateText = $"请求失败：{ex.Message}";
            });
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() => IsAiWinRateLoading = false);
        }
    }

    private void ClearPlayers()
    {
        ReplaceTeam(MyPlayers, []);
        ReplaceTeam(EnemyPlayers, []);
        HasData = false;
        ClearAiMatchupReview();
    }

    private void ClearAiMatchupReview()
    {
        AiMatchupStatusText = HasData
            ? "点击 AI 生成团队评价。"
            : "进入游戏并加载双方战绩后，可点击 AI 生成团队评价。";
        AiTeamReviewSummary = "-";
        AiTeamWinRatePrediction = "-";
        AiTeamFocusPlayer = "-";
        AiEnemyThreat = "-";
        AiWinRateText = "-";
    }

    private void UpdateTeamSummariesFromSessionData()
    {
        MyTeamTotalKda = "-";
        EnemyTeamTotalKda = "-";

        // Middle section is authoritative from the liveclient allgamedata (2999).
        // Do not populate champion icons from session data here. Leave placeholders;
        // ApplyLiveGameData will fill icons from allgamedata (and those icons are
        // fetched via LCU asset endpoints).
        MyTeamChampionIcons.Clear();
        EnemyTeamChampionIcons.Clear();
        for (var i = 0; i < 5; i++)
        {
            MyTeamChampionIcons.Add(null);
            EnemyTeamChampionIcons.Add(null);
        }
    }

    private void LoadChampionIconFromSession(TeammateDisplayInfo player, ObservableCollection<byte[]?> target)
    {
        if (player.ChampionId <= 0)
        {
            target.Add(null);
            return;
        }

        var iconPath = GameDataService.GetChampionIconUrl(player.ChampionId);
        var cached = App.GameData.GetCachedIconBytes(iconPath);
        if (cached is not null)
        {
            target.Add(cached);
        }
        else
        {
            target.Add(null);
            _ = App.GameData.LoadIconBytesAsync(iconPath);
        }
    }

    private void ClearTeamSummaries()
    {
        MyTeamTotalKda = "-";
        EnemyTeamTotalKda = "-";
        Dispatcher.UIThread.Post(() =>
        {
            MyTeamChampionIcons.Clear();
            EnemyTeamChampionIcons.Clear();
        });
    }

    private void StartLiveGamePolling()
    {
        StopLiveGamePolling();
        _liveGamePollingCts = new CancellationTokenSource();
        var ct = _liveGamePollingCts.Token;
        _liveGamePollingTask = Task.Run(async () => await PollLiveGameLoopAsync(ct), ct);
    }

    private void StopLiveGamePolling()
    {
        _liveGamePollingCts?.Cancel();
        _liveGamePollingCts = null;
        _liveGamePollingTask = null;
    }

    private async Task PollLiveGameLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            // Only poll the live client when we're in the InProgress phase. If the phase changes
            // to something else while this task is running, skip performing HTTP requests.
            if (!_isInProgress)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                continue;
            }
                try
                {
                    var (data, status, error) = await App.LiveGameData.FetchAllGameDataWithStatusAsync();
                    if (data?.AllPlayers is { Count: > 0 })
                    {
                        _latestLiveData = data;
                        await Dispatcher.UIThread.InvokeAsync(() => ApplyLiveGameData(data));
                    }

                    try
                    {
                        // Print status code and current totals to console for diagnostics (include neutral summary)
                        var neutral = NeutralSummary;
                        System.Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] LiveClient poll: status={status}, error={error ?? "-"}, My={MyTeamTotalKda}, Enemy={EnemyTeamTotalKda}, Neutral={neutral}");
                    }
                    catch { }
                }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void ApplyLiveGameData(LiveAllGameData data)
    {
        var myPlayers = MyPlayers.Where(p => !string.IsNullOrEmpty(p.SummonerName)).ToList();
        var enemyPlayers = EnemyPlayers.Where(p => !string.IsNullOrEmpty(p.SummonerName)).ToList();

        var orderTeam = data.AllPlayers.Where(p => p.Team == "ORDER").ToList();
        var chaosTeam = data.AllPlayers.Where(p => p.Team == "CHAOS").ToList();

        // Prefer determining my team via activePlayer from allgamedata when available (more reliable in custom modes)
        List<LivePlayerData> myLiveTeam;
        List<LivePlayerData> enemyLiveTeam;
        if (data.ActivePlayer is not null && !string.IsNullOrEmpty(data.ActivePlayer.RiotIdGameName))
        {
            var activeName = NormalizeRiotName(data.ActivePlayer.RiotIdGameName);
            if (orderTeam.Any(p => NormalizeRiotName(p.RiotIdGameName) == activeName))
            {
                myLiveTeam = orderTeam;
                enemyLiveTeam = chaosTeam;
            }
            else
            {
                myLiveTeam = chaosTeam;
                enemyLiveTeam = orderTeam;
            }
        }
        else
        {
            var orderMatchCount = myPlayers.Count(m => orderTeam.Any(l => NormalizeRiotName(l.RiotIdGameName) == NormalizeRiotName(m.SummonerName)));
            var chaosMatchCount = myPlayers.Count(m => chaosTeam.Any(l => NormalizeRiotName(l.RiotIdGameName) == NormalizeRiotName(m.SummonerName)));

            myLiveTeam = orderMatchCount >= chaosMatchCount ? orderTeam : chaosTeam;
            enemyLiveTeam = orderMatchCount >= chaosMatchCount ? chaosTeam : orderTeam;
        }

        foreach (var livePlayer in myLiveTeam.Concat(enemyLiveTeam))
        {
            var matched = myPlayers.Concat(enemyPlayers)
                .FirstOrDefault(m => NormalizeRiotName(m.SummonerName) == NormalizeRiotName(livePlayer.RiotIdGameName));
            if (matched is not null && livePlayer.Scores is not null)
            {
                matched.Kills = livePlayer.Scores.Kills;
                matched.Deaths = livePlayer.Scores.Deaths;
                matched.Assists = livePlayer.Scores.Assists;
            }
        }

        // Build champion name→ID reverse lookup
        if (_championNameToId is null && App.GameData.Champions.Count > 0)
        {
            _championNameToId = App.GameData.Champions.Values
                .GroupBy(c => c.Name)
                .ToDictionary(g => g.Key, g => g.First().Id);
        }

        // Aggregate team summaries
        // Debug logging to help diagnose why totals may not update (can be removed later)
        try
        {
            System.Diagnostics.Debug.WriteLine($"ApplyLiveGameData: myLiveTeam={myLiveTeam.Count}, enemyLiveTeam={enemyLiveTeam.Count}");
        }
        catch { }
        int myTotalK = 0, myTotalD = 0, myTotalA = 0;
        int enemyTotalK = 0, enemyTotalD = 0, enemyTotalA = 0;
        // Neutral resource summaries (will be stored to NeutralSummary for console/AI)
        int myDragonKills = 0, enemyDragonKills = 0;
        int myBaronKills = 0, enemyBaronKills = 0;
        int myHeraldKills = 0, enemyHeraldKills = 0;
        int myTurretKills = 0, enemyTurretKills = 0;
        bool firstBrickByMy = false, firstBrickByEnemy = false;

        MyTeamChampionIcons.Clear();
        EnemyTeamChampionIcons.Clear();

        foreach (var livePlayer in myLiveTeam)
        {
            try { System.Diagnostics.Debug.WriteLine($"My team player {livePlayer.RiotIdGameName} scores={(livePlayer.Scores is null ? "null" : livePlayer.Scores.ToString())}"); } catch { }
            if (livePlayer.Scores is not null)
            {
                myTotalK += livePlayer.Scores.Kills;
                myTotalD += livePlayer.Scores.Deaths;
                myTotalA += livePlayer.Scores.Assists;
            }
            LoadChampionIcon(livePlayer.ChampionName, MyTeamChampionIcons);
        }

        foreach (var livePlayer in enemyLiveTeam)
        {
            try { System.Diagnostics.Debug.WriteLine($"Enemy team player {livePlayer.RiotIdGameName} scores={(livePlayer.Scores is null ? "null" : livePlayer.Scores.ToString())}"); } catch { }
            if (livePlayer.Scores is not null)
            {
                enemyTotalK += livePlayer.Scores.Kills;
                enemyTotalD += livePlayer.Scores.Deaths;
                enemyTotalA += livePlayer.Scores.Assists;
            }
            LoadChampionIcon(livePlayer.ChampionName, EnemyTeamChampionIcons);
        }

        MyTeamTotalKda = $"{myTotalK}/{myTotalD}/{myTotalA}";
        EnemyTeamTotalKda = $"{enemyTotalK}/{enemyTotalD}/{enemyTotalA}";

        // Aggregate neutral resources from events if present
        try
        {
            if (data.Events is not null)
            {
                foreach (var ev in (data.Events.Events ?? new System.Collections.Generic.List<LiveEvent>()))
                {
                    switch (ev.EventName)
                    {
                        case "DragonKill":
                            if (ev.KillerTeam == "ORDER") myDragonKills++; else if (ev.KillerTeam == "CHAOS") enemyDragonKills++;
                            break;
                        case "BaronKill":
                            if (ev.KillerTeam == "ORDER") myBaronKills++; else if (ev.KillerTeam == "CHAOS") enemyBaronKills++;
                            break;
                        case "HeraldKill":
                        case "RiftHeraldKill":
                            if (ev.KillerTeam == "ORDER") myHeraldKills++; else if (ev.KillerTeam == "CHAOS") enemyHeraldKills++;
                            break;
                        case "TurretKilled":
                        case "TowerKilled":
                            if (ev.KillerTeam == "ORDER") myTurretKills++; else if (ev.KillerTeam == "CHAOS") enemyTurretKills++;
                            break;
                        case "FirstBrick":
                            if (ev.RecipientTeam == "ORDER") firstBrickByMy = true; else if (ev.RecipientTeam == "CHAOS") firstBrickByEnemy = true;
                            break;
                    }
                }
            }
        }
        catch { }

        // Build a neutral summary string for console output and AI
        NeutralSummary = $"Dragons {myDragonKills}/{enemyDragonKills}, Baron {myBaronKills}/{enemyBaronKills}, Herald {myHeraldKills}/{enemyHeraldKills}, Turrets {myTurretKills}/{enemyTurretKills}, FirstBrick {(firstBrickByMy ? "My" : firstBrickByEnemy ? "Enemy" : "-")}";

        try
        {
            System.Diagnostics.Debug.WriteLine($"Team totals -> My:{MyTeamTotalKda} Enemy:{EnemyTeamTotalKda}");
        }
        catch { }

        // Ensure UI update occurs on UI thread and notify bindings
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            NotifyTeamPredictionChanged();
        });
    }

    private void LoadChampionIcon(string championName, ObservableCollection<byte[]?> target)
    {
        if (_championNameToId is null || !_championNameToId.TryGetValue(championName, out var championId))
        {
            target.Add(null);
            return;
        }

        var iconPath = GameDataService.GetChampionIconUrl(championId);
        var cached = App.GameData.GetCachedIconBytes(iconPath);
        if (cached is not null)
        {
            target.Add(cached);
        }
        else
        {
            target.Add(null);
            _ = App.GameData.LoadIconBytesAsync(iconPath);
        }
    }

    // Normalize Riot game name / summoner name for matching (strip "#TagLine" if present)
    private static string NormalizeRiotName(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var idx = s.IndexOf('#');
        return idx <= 0 ? s : s.Substring(0, idx);
    }

    private static void ReplaceTeam(ObservableCollection<TeammateDisplayInfo> target, List<TeammateDisplayInfo> players)
    {
        target.Clear();
        for (var i = 0; i < 5; i++)
            target.Add(i < players.Count ? players[i] : new TeammateDisplayInfo());
    }

    private IEnumerable<TeammateDisplayInfo> EnumerateAllPlayers()
    {
        foreach (var p in MyPlayers)
            if (!string.IsNullOrEmpty(p.Puuid))
                yield return p;
        foreach (var p in EnemyPlayers)
            if (!string.IsNullOrEmpty(p.Puuid))
                yield return p;
    }

    // Neutral resource summary exposed for UI/AI
    public string NeutralSummary { get; private set; } = "";

    private async Task RefreshCurrentPlayersMatchHistoryAsync()
    {
        if (_isFetching) return;

        var players = EnumerateAllPlayers().ToList();
        if (players.Count == 0) return;

        _isFetching = true;
        try
        {
            BeginMatchHistoryLoading(players.Count);
            await Task.WhenAll(players.Select(async p =>
            {
                await FetchMatchHistoryAsync(p);
                IncrementMatchHistoryLoading();
            }));
        }
        finally
        {
            _isFetching = false;
        }
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
                        if (string.IsNullOrEmpty(p.SummonerName))
                        {
                            if (!string.IsNullOrEmpty(info.GameName))
                                p.SummonerName = info.GameName;
                            else if (!string.IsNullOrEmpty(info.DisplayName))
                                p.SummonerName = info.DisplayName;
                        }
                        if (string.IsNullOrEmpty(p.TagLine) && !string.IsNullOrEmpty(info.TagLine))
                            p.TagLine = info.TagLine;
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

                if (p.ChampionId > 0)
                {
                    var path = GameDataService.GetChampionIconUrl(p.ChampionId);
                    await App.GameData.LoadIconBytesAsync(path);
                }

                await FetchMatchHistoryAsync(p);
            }
            catch
            {
            }
            finally
            {
                IncrementMatchHistoryLoading();
            }
        });

        await Task.WhenAll(tasks);
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
            player.SkillScore = CalculateSkillScore(player);
            NotifyTeamPredictionChanged();
        });
    }

    private void NotifyTeamPredictionChanged()
    {
        OnPropertyChanged(nameof(MyTeamAverageSkillScore));
        OnPropertyChanged(nameof(EnemyTeamAverageSkillScore));
        OnPropertyChanged(nameof(TeamPredictedWinRate));
        OnPropertyChanged(nameof(MyTeamAverageSkillScoreText));
        OnPropertyChanged(nameof(EnemyTeamAverageSkillScoreText));
        OnPropertyChanged(nameof(TeamPredictedWinRateText));
        OnPropertyChanged(nameof(MyTeamTotalKda));
        OnPropertyChanged(nameof(EnemyTeamTotalKda));
        OnPropertyChanged(nameof(AiWinRateText));
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
}
