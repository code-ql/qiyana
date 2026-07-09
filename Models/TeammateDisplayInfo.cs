using System.Collections.Generic;
using System.Text.Json.Serialization;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using qiyana.Helpers;
using qiyana.Models.MatchData;
using qiyana.Services;

namespace qiyana.Models;

public partial class TeammateDisplayInfo : ObservableObject
{
    public string SummonerName { get; set; } = "";
    public string TagLine { get; set; } = "";
    public string Puuid { get; set; } = "";
    public int CellId { get; set; }
    public int ChampionId { get; set; }
    public string ChampionName { get; set; } = "";
    public int Spell1Id { get; set; }
    public int Spell2Id { get; set; }
    public string AssignedPosition { get; set; } = "";
    public Geometry? PositionGeometry => PositionIcons.Get(AssignedPosition);
    public bool IsAutofilled { get; set; }
    public int SelectedSkinId { get; set; }
    public bool IsLocalPlayer { get; set; }

    public void NotifyChampSelectFieldsChanged()
    {
        OnPropertyChanged(nameof(PositionDisplay));
        OnPropertyChanged(nameof(Spell1Name));
        OnPropertyChanged(nameof(Spell2Name));
        OnPropertyChanged(nameof(Spell1IconPath));
        OnPropertyChanged(nameof(Spell2IconPath));
        OnPropertyChanged(nameof(ChampionDisplay));
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProfileIconPath))]
    private int _profileIconId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SoloDisplay))]
    [NotifyPropertyChangedFor(nameof(SoloColor))]
    private RankedQueueData? _soloQueue;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FlexDisplay))]
    [NotifyPropertyChangedFor(nameof(FlexColor))]
    private RankedQueueData? _flexQueue;

    public string? ProfileIconPath => ProfileIconId > 0
        ? GameDataService.GetProfileIconUrl(ProfileIconId) : null;

    public string? SoloDisplay => SoloQueue is not null
        ? $"{SoloQueue.TierDisplayName} {SoloQueue.Rank}" : null;
    public string? FlexDisplay => FlexQueue is not null
        ? $"{FlexQueue.TierDisplayName} {FlexQueue.Rank}" : null;
    public string SoloColor => SoloQueue?.TierColor ?? "#666";
    public string FlexColor => FlexQueue?.TierColor ?? "#666";

    public string PositionDisplay => AssignedPosition switch
    {
        "top" => "上单",
        "jungle" => "打野",
        "middle" => "中单",
        "bottom" => "下路",
        "support" or "utility" => "辅助",
        _ => ""
    };

    public string Spell1Name => SpellName(Spell1Id);
    public string Spell2Name => SpellName(Spell2Id);
    public string? Spell1IconPath => Spell1Id > 0 ? GameDataService.GetSpellIconUrl(Spell1Id) : null;
    public string? Spell2IconPath => Spell2Id > 0 ? GameDataService.GetSpellIconUrl(Spell2Id) : null;

    public string ChampionDisplay => ChampionId > 0 ? ChampionName : "未选择";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SkillScoreText))]
    private double _skillScore;

    public string SkillScoreText => HasMatchData ? SkillScore.ToString("F0") : "-";

    private static string SpellName(int id) => id switch
    {
        4 => "闪现",
        14 => "点燃",
        11 => "惩戒",
        21 => "治疗",
        12 => "传送",
        3 => "屏障",
        7 => "净化",
        6 => "幽灵疾步",
        13 => "清晰术",
        32 => "雪球",
        _ => $"Spell{id}"
    };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalGames))]
    [NotifyPropertyChangedFor(nameof(WinRate))]
    [NotifyPropertyChangedFor(nameof(HasMatchData))]
    private int _wins;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalGames))]
    [NotifyPropertyChangedFor(nameof(WinRate))]
    [NotifyPropertyChangedFor(nameof(HasMatchData))]
    private int _losses;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(KdaRatio))]
    [NotifyPropertyChangedFor(nameof(KdaText))]
    private int _kills;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(KdaRatio))]
    [NotifyPropertyChangedFor(nameof(KdaText))]
    private int _deaths;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(KdaRatio))]
    [NotifyPropertyChangedFor(nameof(KdaText))]
    private int _assists;

    public int TotalGames => Wins + Losses;
    public string WinRate => TotalGames > 0 ? $"{(double)Wins / TotalGames:P0}" : "-";

    [JsonIgnore]
    public bool HasMatchData => TotalGames > 0;

    [JsonIgnore]
    public double KdaRatio => Deaths > 0 ? (double)(Kills + Assists) / Deaths : Kills + Assists;

    [JsonIgnore]
    public string KdaText => Deaths == 0 ? "完美" : KdaRatio.ToString("F1");

    [ObservableProperty]
    private List<GameDetail> _recentGames = [];

    [ObservableProperty]
    private bool _isExpanded;
}
