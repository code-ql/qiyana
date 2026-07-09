using System.Text.Json.Serialization;

namespace qiyana.Models.MatchData;

public class ParticipantIdentity
{
    public int ParticipantId { get; set; }
    public PlayerInfo? Player { get; set; }
}

public class PlayerInfo
{
    [JsonPropertyName("gameName")]
    public string SummonerName { get; set; } = string.Empty;

    [JsonPropertyName("tagLine")]
    public string TagLine { get; set; } = string.Empty;

    public long SummonerId { get; set; }
    public string Puuid { get; set; } = string.Empty;
    public int ProfileIcon { get; set; }
    public long AccountId { get; set; }
}
