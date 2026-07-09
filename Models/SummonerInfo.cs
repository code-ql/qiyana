using System.Text.Json;
using System.Text.Json.Serialization;

namespace qiyana.Models;

public class SummonerInfo
{
    public int ProfileIconId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Puuid { get; set; } = string.Empty;
    public string SummonerId { get; set; } = string.Empty;
    public long SummonerLevel { get; set; }

    [JsonIgnore]
    public RankedQueueData? SoloQueue { get; set; }

    [JsonIgnore]
    public RankedQueueData? FlexQueue { get; set; }

    public string? Tier => SoloQueue?.Tier;
    public string? Rank => SoloQueue?.Rank;
    public int LeaguePoints => SoloQueue?.LeaguePoints ?? 0;
    public int Wins => SoloQueue?.Wins ?? 0;
    public int Losses => SoloQueue?.Losses ?? 0;
    public string? HighestTier { get; set; }
    public string? HighestRank { get; set; }
    public string? ProfileIconPath => ProfileIconId > 0
        ? $"/lol-game-data/assets/v1/profile-icons/{ProfileIconId}.jpg" : null;
    public string? RankedEmblemPath => SoloQueue?.EmblemPath;
    public string? HighestRankedEmblemPath => !string.IsNullOrEmpty(HighestTier)
        ? $"avares://qiyana/Assets/RankedEmblems/{HighestTier.ToLowerInvariant()}.png" : null;
    public string TierColor => SoloQueue?.TierColor ?? "#c8a84e";
    public string? HighestTierDisplayName => HighestTier switch
    {
        "IRON"        => "黑铁",
        "BRONZE"      => "黄铜",
        "SILVER"      => "白银",
        "GOLD"        => "黄金",
        "PLATINUM"    => "铂金",
        "EMERALD"     => "翡翠",
        "DIAMOND"     => "钻石",
        "MASTER"      => "大师",
        "GRANDMASTER" => "宗师",
        "CHALLENGER"  => "王者",
        _             => HighestTier
    };
    public string HighestTierColor => HighestTier switch
    {
        "IRON"        => "#9e9e9e",
        "BRONZE"      => "#cd7f32",
        "SILVER"      => "#c0c0c0",
        "GOLD"        => "#ffd700",
        "PLATINUM"    => "#00bfff",
        "EMERALD"     => "#50c878",
        "DIAMOND"     => "#b9f2ff",
        "MASTER"      => "#9b30ff",
        "GRANDMASTER" => "#ff4500",
        "CHALLENGER"  => "#00ffff",
        _             => "#c8a84e"
    };

    public long AccountId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string GameName { get; set; } = string.Empty;
    public string InternalName { get; set; } = string.Empty;
    public bool NameChangeFlag { get; set; }
    public int PercentCompleteForNextLevel { get; set; }
    public string Privacy { get; set; } = string.Empty;
    public int RerollPointsCurrent { get; set; }
    public int RerollPointsMax { get; set; }
    public string TagLine { get; set; } = string.Empty;
    public bool Unnamed { get; set; }
    public long XpSinceLastLevel { get; set; }
    public long XpUntilNextLevel { get; set; }

    [JsonIgnore]
    public bool HasNoRankedData => SoloQueue is null && FlexQueue is null;

    public string RerollDisplay
    {
        get
        {
            if (RerollPointsMax <= 0) return "";
            return $"{RerollPointsCurrent}/{RerollPointsMax}（满{RerollPointsMax}点可重随一次，最多存{RerollPointsMax / 125}次）";
        }
    }
}
