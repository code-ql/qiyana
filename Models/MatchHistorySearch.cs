using System;

namespace qiyana.Models;

public class MatchHistorySearch
{
    public string Puuid { get; set; } = string.Empty;
    public string? GameName { get; set; }
    public string? TagLine { get; set; }
    public DateTime? LastSearchedAt { get; set; }
    public string DisplayName => GameName is not null ? $"{GameName}#{TagLine}" : (Puuid.Length >= 8 ? Puuid[..8] : Puuid);
}
