namespace qiyana.Models;

public record RankedQueueData
{
    public string? Tier { get; init; }
    public string? Rank { get; init; }
    public int LeaguePoints { get; init; }
    public int Wins { get; init; }
    public int Losses { get; init; }
    public double WinRate => Wins + Losses > 0 ? (double)Wins / (Wins + Losses) * 100 : 0;
    public string? EmblemPath => Tier is not null
        ? $"avares://qiyana/Assets/RankedEmblems/{Tier.ToLowerInvariant()}.png" : null;
    public string? TierDisplayName => Tier switch
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
        _             => Tier
    };
    public string TierColor => Tier switch
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
}
