using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace qiyana.Models.MatchData;

public class Team
{
    public int TeamId { get; set; }
    public string Win { get; set; } = string.Empty;
    public bool FirstBlood { get; set; }
    public bool FirstTower { get; set; }
    public bool FirstInhibitor { get; set; }
    public bool FirstBaron { get; set; }
    [JsonPropertyName("firstDargon")]
    public bool FirstDragon { get; set; }
    public int TowerKills { get; set; }
    public int InhibitorKills { get; set; }
    public int BaronKills { get; set; }
    public int DragonKills { get; set; }
    public int RiftHeraldKills { get; set; }
    public int HordeKills { get; set; }
    public int VilemawKills { get; set; }
    public int DominionVictoryScore { get; set; }
    public List<Ban> Bans { get; set; } = [];

    [JsonIgnore]
    public List<Ban> BansSorted => Bans.OrderBy(b => b.PickTurn).ToList();
}

public class Ban
{
    public int ChampionId { get; set; }
    public int PickTurn { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public byte[]? ChampionIcon { get; set; }
}
