using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using qiyana.Services;

namespace qiyana.Models.MatchData;

public class GameDetail
{
    public long GameId { get; set; }
    public string GameMode { get; set; } = string.Empty;
    public int GameDuration { get; set; }
    public long GameCreation { get; set; }
    public string? GameCreationDate { get; set; }
    public string GameVersion { get; set; } = string.Empty;
    public int QueueId { get; set; }
    public int MapId { get; set; }

    [JsonIgnore]
    public string MapDisplayName => MapId switch
    {
        1 or 2 or 11 => "召唤师峡谷",
        8 or 10 => "扭曲丛林",
        12 => "嚎哭深渊",
        14 => "嚎哭深渊",
        16 => "水晶之痕",
        18 or 19 or 20 => "试炼之地",
        _ => $"地图 {MapId}"
    };

    public List<Team> Teams { get; set; } = [];
    public List<ParticipantIdentity> ParticipantIdentities { get; set; } = [];
    public List<Participant> Participants { get; set; } = [];

    [JsonIgnore]
    public string QueueDisplayName => App.GameData?.Queues?.GetValueOrDefault(QueueId)?.Name ?? GameMode;

    [JsonIgnore]
    public List<Participant> TeamBlue => Participants.Where(p => p.TeamId == 100).ToList();

    [JsonIgnore]
    public List<Participant> TeamRed => Participants.Where(p => p.TeamId == 200).ToList();

    [JsonIgnore]
    public bool Win => CurrentParticipant?.Stats?.Win ?? false;

    [JsonIgnore]
    public Participant? CurrentParticipant => Participants.FirstOrDefault(p => p.IsCurrentSummoner);

    [JsonIgnore]
    public int Duration => GameDuration;

    [JsonIgnore]
    public long Timestamp => GameCreation;

    [JsonIgnore]
    public string KillsForeground => CurrentParticipant?.KillsForeground ?? "#ccc";
    [JsonIgnore]
    public string DeathsForeground => CurrentParticipant?.DeathsForeground ?? "#ccc";
    [JsonIgnore]
    public string AssistsForeground => CurrentParticipant?.AssistsForeground ?? "#ccc";

    [JsonIgnore]
    public bool TeamBlueIsWin => Teams.Count > 0 && Teams[0].Win == "Win";
    [JsonIgnore]
    public bool TeamRedIsWin => Teams.Count > 1 && Teams[1].Win == "Win";

    [JsonIgnore]
    public int TeamBlueTotalKills => TeamBlue.Sum(p => p.Stats?.Kills ?? 0);

    [JsonIgnore]
    public int TeamRedTotalKills => TeamRed.Sum(p => p.Stats?.Kills ?? 0);

    [JsonIgnore]
    public string GameVersionDisplay
    {
        get
        {
            if (string.IsNullOrEmpty(GameVersion)) return "";
            var parts = GameVersion.Split('.');
            return parts.Length >= 2 ? $"{parts[0]}.{parts[1]}" : GameVersion;
        }
    }

    [JsonIgnore]
    public int ChampionId => CurrentParticipant?.ChampionId ?? 0;

    [JsonIgnore]
    public string ChampionName => CurrentParticipant?.ChampionName ?? "";

    [JsonIgnore]
    public int Kills => CurrentParticipant?.Stats?.Kills ?? 0;

    [JsonIgnore]
    public int Deaths => CurrentParticipant?.Stats?.Deaths ?? 0;

    [JsonIgnore]
    public int Assists => CurrentParticipant?.Stats?.Assists ?? 0;

    [JsonIgnore]
    public byte[]? ChampionIcon
    {
        get => CurrentParticipant?.ChampionIcon;
        set
        {
            if (CurrentParticipant is not null)
                CurrentParticipant.ChampionIcon = value;
        }
    }

    [JsonIgnore]
    public List<byte[]?> ItemIcons
    {
        get => CurrentParticipant?.ItemIcons ?? [];
        set
        {
            if (CurrentParticipant is not null)
                CurrentParticipant.ItemIcons = value;
        }
    }

    public void ApplyTeamHighlights()
    {
        var all = Participants.Where(p => p.Stats is not null).ToList();
        if (all.Count == 0) return;
        var maxKills = all.Max(p => p.Stats!.Kills);
        var minDeaths = all.Min(p => p.Stats!.Deaths);
        var maxAssists = all.Max(p => p.Stats!.Assists);
        var maxVision = all.Max(p => p.Stats!.VisionScore);
        var maxDamageTaken = all.Max(p => p.Stats!.TotalDamageTaken);
        var maxCs = all.Max(p => p.TotalCs);
        foreach (var p in all)
        {
            p.IsTeamMostKills = p.Stats!.Kills == maxKills && maxKills > 0;
            p.IsTeamLeastDeaths = p.Stats!.Deaths == minDeaths;
            p.IsTeamMostAssists = p.Stats!.Assists == maxAssists && maxAssists > 0;
            p.IsTeamMostVision = p.Stats!.VisionScore == maxVision && maxVision > 0;
            p.IsTeamMostDamageTaken = p.Stats!.TotalDamageTaken == maxDamageTaken && maxDamageTaken > 0;
            p.IsTeamMostCs = p.TotalCs == maxCs && maxCs > 0;
        }
    }
}
