using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Media;
using qiyana.Helpers;

namespace qiyana.Models.MatchData;

public class Participant
{
    public int ParticipantId { get; set; }
    public int ChampionId { get; set; }
    public string ChampionName { get; set; } = string.Empty;
    public int Spell1Id { get; set; }
    public int Spell2Id { get; set; }
    public int TeamId { get; set; }
    public ParticipantStats? Stats { get; set; }
    public ParticipantTimeline? Timeline { get; set; }

    [JsonIgnore]
    public string SummonerName { get; set; } = string.Empty;

    [JsonIgnore]
    public string SummonerTagline { get; set; } = string.Empty;

    [JsonIgnore]
    public string SummonerFullName =>
        string.IsNullOrEmpty(SummonerTagline) ? SummonerName : $"{SummonerName}#{SummonerTagline}";

    [JsonIgnore]
    public string Puuid { get; set; } = string.Empty;

    [JsonIgnore]
    public bool IsCurrentSummoner { get; set; }

    [JsonIgnore]
    public List<int> Items => Stats is not null
        ? new[] { Stats.Item0, Stats.Item1, Stats.Item2, Stats.Item3, Stats.Item4, Stats.Item5, Stats.Item6 }
            .Where(id => id > 0).ToList()
        : [];

    [JsonIgnore]
    public List<int> PerkIds => Stats is not null
        ? new List<int> { Stats.Perk0, Stats.Perk1, Stats.Perk2, Stats.Perk3, Stats.Perk4, Stats.Perk5 }
            .Where(id => id > 0).ToList()
        : [];

    [JsonIgnore]
    public int KeystonePerkId => Stats?.Perk0 ?? 0;

    [JsonIgnore]
    public int PerkPrimaryStyle => Stats?.PerkPrimaryStyle ?? 0;

    [JsonIgnore]
    public int PerkSubStyle => Stats?.PerkSubStyle ?? 0;

    [JsonIgnore]
    public List<byte[]?> ItemIcons { get; set; } = [];

    [JsonIgnore]
    public byte[]? ChampionIcon { get; set; }

    [JsonIgnore]
    public byte[]? Spell1Icon { get; set; }

    [JsonIgnore]
    public byte[]? Spell2Icon { get; set; }

    [JsonIgnore]
    public byte[]? KeystoneRuneIcon { get; set; }

    [JsonIgnore]
    public byte[]? PrimaryStyleIcon { get; set; }

    [JsonIgnore]
    public byte[]? SecondaryStyleIcon { get; set; }

    [JsonIgnore]
    public List<byte[]?> PerkIcons { get; set; } = [];

    [JsonIgnore]
    public string PrimaryStyleName { get; set; } = string.Empty;

    [JsonIgnore]
    public string SecondaryStyleName { get; set; } = string.Empty;

    [JsonIgnore]
    public int TotalCs => Stats is not null ? Stats.TotalMinionsKilled + Stats.NeutralMinionsKilled : 0;

    private int _gameDuration;
    [JsonIgnore]
    public int GameDurationCache
    {
        get => _gameDuration;
        set => _gameDuration = value;
    }

    [JsonIgnore]
    public double CsPerMin => _gameDuration > 0 ? TotalCs / (_gameDuration / 60.0) : 0;

    [JsonIgnore]
    public string LaneLabel => (Timeline?.Lane, Timeline?.Role) switch
    {
        ("TOP", "SOLO") => "上单",
        ("JUNGLE", _) => "打野",
        ("MIDDLE", "SOLO") => "中单",
        ("BOTTOM", "CARRY") => "ADC",
        ("BOTTOM", "SUPPORT") => "辅助",
        _ => Timeline?.Lane ?? ""
    };

    [JsonIgnore]
    public Geometry? PositionGeometry =>
        string.IsNullOrEmpty(Timeline?.Lane) ? null : PositionIcons.Get(Timeline.Lane);

    [JsonIgnore]
    public string KdaText
    {
        get
        {
            if (Stats is null) return "0.0";
            return Stats.Deaths == 0 ? "完美" : Stats.KdaRatio.ToString("F1");
        }
    }

    [JsonIgnore]
    public double DamagePercent { get; set; }

    [JsonIgnore]
    public string MultiKillLabel
    {
        get
        {
            if (Stats is null) return "";
            if (Stats.PentaKills > 0) return "五杀";
            if (Stats.QuadraKills > 0) return "四杀";
            if (Stats.TripleKills > 0) return "三杀";
            if (Stats.DoubleKills > 0) return "双杀";
            return "";
        }
    }

    [JsonIgnore]
    public string FirstBloodText => "一血";

    [JsonIgnore]
    public string FirstTowerText => "一塔";

    [JsonIgnore]
    public bool HasMultiKill => Stats is not null && (Stats.DoubleKills > 0 || Stats.TripleKills > 0 || Stats.QuadraKills > 0 || Stats.PentaKills > 0);

    [JsonIgnore]
    public bool HasFirstBloodKill => Stats?.FirstBloodKill ?? false;

    [JsonIgnore]
    public bool HasFirstBloodAssist => Stats?.FirstBloodAssist ?? false;

    [JsonIgnore]
    public bool IsTeamMostKills { get; set; }
    [JsonIgnore]
    public bool IsTeamLeastDeaths { get; set; }
    [JsonIgnore]
    public bool IsTeamMostAssists { get; set; }
    [JsonIgnore]
    public bool IsTeamMostVision { get; set; }
    [JsonIgnore]
    public bool IsTeamMostDamageTaken { get; set; }
    [JsonIgnore]
    public bool IsTeamMostCs { get; set; }

    [JsonIgnore]
    public string KillsForeground => IsTeamMostKills ? "#4caf50" : "#ccc";
    [JsonIgnore]
    public string DeathsForeground => IsTeamLeastDeaths ? "#4caf50" : "#ccc";
    [JsonIgnore]
    public string AssistsForeground => IsTeamMostAssists ? "#4caf50" : "#ccc";
    [JsonIgnore]
    public string VisionForeground => IsTeamMostVision ? "#4caf50" : "#ccc";
    [JsonIgnore]
    public string DamageTakenForeground => IsTeamMostDamageTaken ? "#4caf50" : "#ccc";
    [JsonIgnore]
    public string CsForeground => IsTeamMostCs ? "#4caf50" : "#ccc";

    [JsonIgnore]
    public bool HasFirstTowerKill => Stats?.FirstTowerKill ?? false;

    [JsonIgnore]
    public bool HasFirstTowerAssist => Stats?.FirstTowerAssist ?? false;

    [JsonIgnore]
    public string GoldFormatted => Stats?.GoldEarned.ToString("N0") ?? "0";

    [JsonIgnore]
    public string DamageFormatted => Stats?.TotalDamageDealtToChampions.ToString("N0") ?? "0";

    [JsonIgnore]
    public string DamageTakenFormatted => Stats?.TotalDamageTaken.ToString("N0") ?? "0";

    [JsonIgnore]
    public double PhysicalDamageProportion => Stats is not null && Stats.TotalDamageDealtToChampions > 0
        ? (double)Stats.PhysicalDamageDealtToChampions / Stats.TotalDamageDealtToChampions : 0;

    [JsonIgnore]
    public double MagicDamageProportion => Stats is not null && Stats.TotalDamageDealtToChampions > 0
        ? (double)Stats.MagicDamageDealtToChampions / Stats.TotalDamageDealtToChampions : 0;

    [JsonIgnore]
    public double TrueDamageProportion => Stats is not null && Stats.TotalDamageDealtToChampions > 0
        ? (double)Stats.TrueDamageDealtToChampions / Stats.TotalDamageDealtToChampions : 0;

    [JsonIgnore]
    public string TimeCcFormatted
    {
        get
        {
            if (Stats is null) return "0";
            var seconds = Stats.TimeCCingOthers / 1000.0;
            return seconds >= 60 ? $"{(int)seconds / 60}m{(int)seconds % 60}s" : $"{seconds:F1}s";
        }
    }

    [JsonIgnore]
    public string LongestLivingFormatted
    {
        get
        {
            var seconds = Stats?.LongestTimeSpentLiving ?? 0;
            return seconds >= 60 ? $"{(int)seconds / 60}m{(int)seconds % 60}s" : $"{seconds}s";
        }
    }
}

public class ParticipantStats
{
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Assists { get; set; }
    public int ChampLevel { get; set; }
    public int TotalDamageDealtToChampions { get; set; }
    public int TotalDamageTaken { get; set; }
    public int GoldEarned { get; set; }
    public int TotalMinionsKilled { get; set; }
    public double VisionScore { get; set; }

    [JsonConverter(typeof(WinConverter))]
    public bool Win { get; set; }

    public int Item0 { get; set; }
    public int Item1 { get; set; }
    public int Item2 { get; set; }
    public int Item3 { get; set; }
    public int Item4 { get; set; }
    public int Item5 { get; set; }
    public int Item6 { get; set; }

    public int Perk0 { get; set; }
    public int Perk1 { get; set; }
    public int Perk2 { get; set; }
    public int Perk3 { get; set; }
    public int Perk4 { get; set; }
    public int Perk5 { get; set; }

    public int PerkPrimaryStyle { get; set; }
    public int PerkSubStyle { get; set; }

    public int StatPerk0 { get; set; }
    public int StatPerk1 { get; set; }
    public int StatPerk2 { get; set; }

    public int LargestMultiKill { get; set; }
    public int LargestKillingSpree { get; set; }
    public int LargestCriticalStrike { get; set; }

    public int DoubleKills { get; set; }
    public int TripleKills { get; set; }
    public int QuadraKills { get; set; }
    public int PentaKills { get; set; }

    public bool FirstBloodKill { get; set; }
    public bool FirstBloodAssist { get; set; }
    public bool FirstTowerKill { get; set; }
    public bool FirstTowerAssist { get; set; }

    public int MagicDamageDealtToChampions { get; set; }
    public int PhysicalDamageDealtToChampions { get; set; }
    public int TrueDamageDealtToChampions { get; set; }
    public int TotalHeal { get; set; }
    public int TotalDamageDealt { get; set; }
    public int TotalTimeCrowdControlDealt { get; set; }

    public int VisionWardsBoughtInGame { get; set; }
    public int SightWardsBoughtInGame { get; set; }
    public int WardsPlaced { get; set; }
    public int WardsKilled { get; set; }

    public int NeutralMinionsKilled { get; set; }
    public int NeutralMinionsKilledTeamJungle { get; set; }
    public int NeutralMinionsKilledEnemyJungle { get; set; }
    public int DamageDealtToObjectives { get; set; }
    public int DamageDealtToTurrets { get; set; }
    public int DamageSelfMitigated { get; set; }
    public int TimeCCingOthers { get; set; }
    public int LongestTimeSpentLiving { get; set; }
    public int TurretKills { get; set; }
    public int InhibitorKills { get; set; }
    public int TotalUnitsHealed { get; set; }
    public int ConsumablesPurchased { get; set; }
    public int ItemsPurchased { get; set; }

    [JsonIgnore]
    public double KdaRatio => Deaths > 0 ? (double)(Kills + Assists) / Deaths : Kills + Assists;
}

public class WinConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.True)
            return true;
        if (reader.TokenType == JsonTokenType.False)
            return false;
        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            return string.Equals(str, "Win", StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
    {
        writer.WriteBooleanValue(value);
    }
}
