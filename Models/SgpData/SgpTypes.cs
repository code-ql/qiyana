using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace qiyana.Models.SgpData;

public class SgpMatchHistoryLol
{
    [JsonPropertyName("games")]
    public List<SgpGameSummaryLol> Games { get; set; } = [];
}

public class SgpGameSummaryLol
{
    [JsonPropertyName("metadata")]
    public SgpGameMetadataLol? Metadata { get; set; }

    [JsonPropertyName("json")]
    public SgpGameSummaryJsonLol? Json { get; set; }
}

public class SgpGameMetadataLol
{
    [JsonPropertyName("product")]
    public string Product { get; set; } = string.Empty;

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];

    [JsonPropertyName("participants")]
    public List<string> Participants { get; set; } = [];

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [JsonPropertyName("data_version")]
    public string DataVersion { get; set; } = string.Empty;

    [JsonPropertyName("info_type")]
    public string InfoType { get; set; } = string.Empty;

    [JsonPropertyName("match_id")]
    public string MatchId { get; set; } = string.Empty;

    [JsonPropertyName("private")]
    public bool Private { get; set; }
}

public class SgpGameSummaryJsonLol
{
    [JsonPropertyName("endOfGameResult")]
    public string EndOfGameResult { get; set; } = string.Empty;

    [JsonPropertyName("gameCreation")]
    public long GameCreation { get; set; }

    [JsonPropertyName("gameDuration")]
    public int GameDuration { get; set; }

    [JsonPropertyName("gameEndTimestamp")]
    public long GameEndTimestamp { get; set; }

    [JsonPropertyName("gameId")]
    public long GameId { get; set; }

    [JsonPropertyName("gameMode")]
    public string GameMode { get; set; } = string.Empty;

    [JsonPropertyName("gameName")]
    public string GameName { get; set; } = string.Empty;

    [JsonPropertyName("gameStartTimestamp")]
    public long GameStartTimestamp { get; set; }

    [JsonPropertyName("gameType")]
    public string GameType { get; set; } = string.Empty;

    [JsonPropertyName("gameVersion")]
    public string GameVersion { get; set; } = string.Empty;

    [JsonPropertyName("mapId")]
    public int MapId { get; set; }

    [JsonPropertyName("participants")]
    public List<SgpParticipantLol> Participants { get; set; } = [];

    [JsonPropertyName("platformId")]
    public string PlatformId { get; set; } = string.Empty;

    [JsonPropertyName("queueId")]
    public int QueueId { get; set; }

    [JsonPropertyName("seasonId")]
    public int SeasonId { get; set; }

    [JsonPropertyName("teams")]
    public List<SgpTeam> Teams { get; set; } = [];

    [JsonPropertyName("tournamentCode")]
    public string TournamentCode { get; set; } = string.Empty;
}

public class SgpTeam
{
    [JsonPropertyName("bans")]
    public List<SgpBan> Bans { get; set; } = [];

    [JsonPropertyName("objectives")]
    public SgpObjectives? Objectives { get; set; }

    [JsonPropertyName("teamId")]
    public int TeamId { get; set; }

    [JsonPropertyName("win")]
    public bool Win { get; set; }
}

public class SgpBan
{
    [JsonPropertyName("championId")]
    public int ChampionId { get; set; }

    [JsonPropertyName("pickTurn")]
    public int PickTurn { get; set; }
}

public class SgpObjectives
{
    [JsonPropertyName("baron")]
    public SgpObjective? Baron { get; set; }

    [JsonPropertyName("champion")]
    public SgpObjective? Champion { get; set; }

    [JsonPropertyName("dragon")]
    public SgpObjective? Dragon { get; set; }

    [JsonPropertyName("horde")]
    public SgpObjective? Horde { get; set; }

    [JsonPropertyName("inhibitor")]
    public SgpObjective? Inhibitor { get; set; }

    [JsonPropertyName("riftHerald")]
    public SgpObjective? RiftHerald { get; set; }

    [JsonPropertyName("tower")]
    public SgpObjective? Tower { get; set; }
}

public class SgpObjective
{
    [JsonPropertyName("first")]
    public bool First { get; set; }

    [JsonPropertyName("kills")]
    public int Kills { get; set; }
}

public class SgpParticipantLol
{
    [JsonPropertyName("allInPings")]
    public int AllInPings { get; set; }

    [JsonPropertyName("assistMePings")]
    public int AssistMePings { get; set; }

    [JsonPropertyName("assists")]
    public int Assists { get; set; }

    [JsonPropertyName("baronKills")]
    public int BaronKills { get; set; }

    [JsonPropertyName("basicPings")]
    public int BasicPings { get; set; }

    [JsonPropertyName("bountyLevel")]
    public int BountyLevel { get; set; }

    [JsonPropertyName("champExperience")]
    public int ChampExperience { get; set; }

    [JsonPropertyName("champLevel")]
    public int ChampLevel { get; set; }

    [JsonPropertyName("championId")]
    public int ChampionId { get; set; }

    [JsonPropertyName("championName")]
    public string ChampionName { get; set; } = string.Empty;

    [JsonPropertyName("consumablesPurchased")]
    public int ConsumablesPurchased { get; set; }

    [JsonPropertyName("damageDealtToBuildings")]
    public int DamageDealtToBuildings { get; set; }

    [JsonPropertyName("damageDealtToObjectives")]
    public int DamageDealtToObjectives { get; set; }

    [JsonPropertyName("damageDealtToTurrets")]
    public int DamageDealtToTurrets { get; set; }

    [JsonPropertyName("damageSelfMitigated")]
    public int DamageSelfMitigated { get; set; }

    [JsonPropertyName("dangerPings")]
    public int DangerPings { get; set; }

    [JsonPropertyName("deaths")]
    public int Deaths { get; set; }

    [JsonPropertyName("detectorWardsPlaced")]
    public int DetectorWardsPlaced { get; set; }

    [JsonPropertyName("doubleKills")]
    public int DoubleKills { get; set; }

    [JsonPropertyName("dragonKills")]
    public int DragonKills { get; set; }

    [JsonPropertyName("enemyMissingPings")]
    public int EnemyMissingPings { get; set; }

    [JsonPropertyName("enemyVisionPings")]
    public int EnemyVisionPings { get; set; }

    [JsonPropertyName("firstBloodAssist")]
    public bool FirstBloodAssist { get; set; }

    [JsonPropertyName("firstBloodKill")]
    public bool FirstBloodKill { get; set; }

    [JsonPropertyName("firstTowerAssist")]
    public bool FirstTowerAssist { get; set; }

    [JsonPropertyName("firstTowerKill")]
    public bool FirstTowerKill { get; set; }

    [JsonPropertyName("getBackPings")]
    public int GetBackPings { get; set; }

    [JsonPropertyName("goldEarned")]
    public int GoldEarned { get; set; }

    [JsonPropertyName("goldSpent")]
    public int GoldSpent { get; set; }

    [JsonPropertyName("holdPings")]
    public int HoldPings { get; set; }

    [JsonPropertyName("individualPosition")]
    public string IndividualPosition { get; set; } = string.Empty;

    [JsonPropertyName("inhibitorKills")]
    public int InhibitorKills { get; set; }

    [JsonPropertyName("inhibitorTakedowns")]
    public int InhibitorTakedowns { get; set; }

    [JsonPropertyName("inhibitorsLost")]
    public int InhibitorsLost { get; set; }

    [JsonPropertyName("item0")]
    public int Item0 { get; set; }

    [JsonPropertyName("item1")]
    public int Item1 { get; set; }

    [JsonPropertyName("item2")]
    public int Item2 { get; set; }

    [JsonPropertyName("item3")]
    public int Item3 { get; set; }

    [JsonPropertyName("item4")]
    public int Item4 { get; set; }

    [JsonPropertyName("item5")]
    public int Item5 { get; set; }

    [JsonPropertyName("item6")]
    public int Item6 { get; set; }

    [JsonPropertyName("itemsPurchased")]
    public int ItemsPurchased { get; set; }

    [JsonPropertyName("killingSprees")]
    public int KillingSprees { get; set; }

    [JsonPropertyName("kills")]
    public int Kills { get; set; }

    [JsonPropertyName("lane")]
    public string Lane { get; set; } = string.Empty;

    [JsonPropertyName("largestCriticalStrike")]
    public int LargestCriticalStrike { get; set; }

    [JsonPropertyName("largestKillingSpree")]
    public int LargestKillingSpree { get; set; }

    [JsonPropertyName("largestMultiKill")]
    public int LargestMultiKill { get; set; }

    [JsonPropertyName("longestTimeSpentLiving")]
    public int LongestTimeSpentLiving { get; set; }

    [JsonPropertyName("magicDamageDealt")]
    public int MagicDamageDealt { get; set; }

    [JsonPropertyName("magicDamageDealtToChampions")]
    public int MagicDamageDealtToChampions { get; set; }

    [JsonPropertyName("magicDamageTaken")]
    public int MagicDamageTaken { get; set; }

    [JsonPropertyName("needVisionPings")]
    public int NeedVisionPings { get; set; }

    [JsonPropertyName("neutralMinionsKilled")]
    public int NeutralMinionsKilled { get; set; }

    [JsonPropertyName("nexusKills")]
    public int NexusKills { get; set; }

    [JsonPropertyName("nexusLost")]
    public int NexusLost { get; set; }

    [JsonPropertyName("nexusTakedowns")]
    public int NexusTakedowns { get; set; }

    [JsonPropertyName("objectivesStolen")]
    public int ObjectivesStolen { get; set; }

    [JsonPropertyName("objectivesStolenAssists")]
    public int ObjectivesStolenAssists { get; set; }

    [JsonPropertyName("onMyWayPings")]
    public int OnMyWayPings { get; set; }

    [JsonPropertyName("participantId")]
    public int ParticipantId { get; set; }

    [JsonPropertyName("pentaKills")]
    public int PentaKills { get; set; }

    [JsonPropertyName("physicalDamageDealt")]
    public int PhysicalDamageDealt { get; set; }

    [JsonPropertyName("physicalDamageDealtToChampions")]
    public int PhysicalDamageDealtToChampions { get; set; }

    [JsonPropertyName("physicalDamageTaken")]
    public int PhysicalDamageTaken { get; set; }

    [JsonPropertyName("placement")]
    public int Placement { get; set; }

    [JsonPropertyName("profileIcon")]
    public int ProfileIcon { get; set; }

    [JsonPropertyName("pushPings")]
    public int PushPings { get; set; }

    [JsonPropertyName("puuid")]
    public string Puuid { get; set; } = string.Empty;

    [JsonPropertyName("quadraKills")]
    public int QuadraKills { get; set; }

    [JsonPropertyName("riotIdGameName")]
    public string RiotIdGameName { get; set; } = string.Empty;

    [JsonPropertyName("riotIdTagline")]
    public string RiotIdTagline { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("sightWardsBoughtInGame")]
    public int SightWardsBoughtInGame { get; set; }

    [JsonPropertyName("spell1Casts")]
    public int Spell1Casts { get; set; }

    [JsonPropertyName("spell1Id")]
    public int Spell1Id { get; set; }

    [JsonPropertyName("spell2Casts")]
    public int Spell2Casts { get; set; }

    [JsonPropertyName("spell2Id")]
    public int Spell2Id { get; set; }

    [JsonPropertyName("summonerId")]
    public long SummonerId { get; set; }

    [JsonPropertyName("summonerLevel")]
    public int SummonerLevel { get; set; }

    [JsonPropertyName("summonerName")]
    public string SummonerName { get; set; } = string.Empty;

    [JsonPropertyName("teamId")]
    public int TeamId { get; set; }

    [JsonPropertyName("teamPosition")]
    public string TeamPosition { get; set; } = string.Empty;

    [JsonPropertyName("timeCCingOthers")]
    public int TimeCCingOthers { get; set; }

    [JsonPropertyName("timePlayed")]
    public int TimePlayed { get; set; }

    [JsonPropertyName("totalAllyJungleMinionsKilled")]
    public int TotalAllyJungleMinionsKilled { get; set; }

    [JsonPropertyName("totalDamageDealt")]
    public int TotalDamageDealt { get; set; }

    [JsonPropertyName("totalDamageDealtToChampions")]
    public int TotalDamageDealtToChampions { get; set; }

    [JsonPropertyName("totalDamageShieldedOnTeammates")]
    public int TotalDamageShieldedOnTeammates { get; set; }

    [JsonPropertyName("totalDamageTaken")]
    public int TotalDamageTaken { get; set; }

    [JsonPropertyName("totalEnemyJungleMinionsKilled")]
    public int TotalEnemyJungleMinionsKilled { get; set; }

    [JsonPropertyName("totalHeal")]
    public int TotalHeal { get; set; }

    [JsonPropertyName("totalHealsOnTeammates")]
    public int TotalHealsOnTeammates { get; set; }

    [JsonPropertyName("totalMinionsKilled")]
    public int TotalMinionsKilled { get; set; }

    [JsonPropertyName("totalTimeCCDealt")]
    public int TotalTimeCCDealt { get; set; }

    [JsonPropertyName("totalTimeSpentDead")]
    public int TotalTimeSpentDead { get; set; }

    [JsonPropertyName("totalUnitsHealed")]
    public int TotalUnitsHealed { get; set; }

    [JsonPropertyName("tripleKills")]
    public int TripleKills { get; set; }

    [JsonPropertyName("trueDamageDealt")]
    public int TrueDamageDealt { get; set; }

    [JsonPropertyName("trueDamageDealtToChampions")]
    public int TrueDamageDealtToChampions { get; set; }

    [JsonPropertyName("trueDamageTaken")]
    public int TrueDamageTaken { get; set; }

    [JsonPropertyName("turretKills")]
    public int TurretKills { get; set; }

    [JsonPropertyName("turretTakedowns")]
    public int TurretTakedowns { get; set; }

    [JsonPropertyName("turretsLost")]
    public int TurretsLost { get; set; }

    [JsonPropertyName("unrealKills")]
    public int UnrealKills { get; set; }

    [JsonPropertyName("visionClearedPings")]
    public int VisionClearedPings { get; set; }

    [JsonPropertyName("visionScore")]
    public double VisionScore { get; set; }

    [JsonPropertyName("visionWardsBoughtInGame")]
    public int VisionWardsBoughtInGame { get; set; }

    [JsonPropertyName("wardsKilled")]
    public int WardsKilled { get; set; }

    [JsonPropertyName("wardsPlaced")]
    public int WardsPlaced { get; set; }

    [JsonPropertyName("win")]
    public bool Win { get; set; }
}

public class SgpServersConfig
{
    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("servers")]
    public Dictionary<string, SgpServerEntry> Servers { get; set; } = [];

    [JsonPropertyName("serverNames")]
    public Dictionary<string, Dictionary<string, string>> ServerNames { get; set; } = [];

    [JsonPropertyName("tencentServerMatchHistoryInteroperability")]
    public List<string> TencentServerMatchHistoryInteroperability { get; set; } = [];

    [JsonPropertyName("tencentServerSpectatorInteroperability")]
    public List<string> TencentServerSpectatorInteroperability { get; set; } = [];

    [JsonPropertyName("tencentServerSummonerInteroperability")]
    public List<string> TencentServerSummonerInteroperability { get; set; } = [];
}

public class SgpServerEntry
{
    [JsonPropertyName("matchHistory")]
    public string? MatchHistory { get; set; }

    [JsonPropertyName("common")]
    public string? Common { get; set; }
}
