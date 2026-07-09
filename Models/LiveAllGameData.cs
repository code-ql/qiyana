using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace qiyana.Models;

public class LiveAllGameData
{
    [JsonPropertyName("allPlayers")]
    public List<LivePlayerData> AllPlayers { get; set; } = [];

    [JsonPropertyName("gameData")]
    public LiveGameData? GameData { get; set; }

    [JsonPropertyName("activePlayer")]
    public LiveActivePlayer? ActivePlayer { get; set; }

    [JsonPropertyName("events")]
    public LiveEvents? Events { get; set; }
}

public class LivePlayerData
{
    [JsonPropertyName("championName")]
    public string ChampionName { get; set; } = "";

    [JsonPropertyName("riotId")]
    public string RiotId { get; set; } = "";

    [JsonPropertyName("riotIdGameName")]
    public string RiotIdGameName { get; set; } = "";

    [JsonPropertyName("riotIdTagLine")]
    public string RiotIdTagLine { get; set; } = "";

    [JsonPropertyName("summonerName")]
    public string SummonerName { get; set; } = "";

    [JsonPropertyName("scores")]
    public LivePlayerScores? Scores { get; set; }

    [JsonPropertyName("team")]
    public string Team { get; set; } = "";

    [JsonPropertyName("level")]
    public int Level { get; set; }

    [JsonPropertyName("skinID")]
    public int SkinId { get; set; }

    [JsonPropertyName("items")]
    // Items can be an array or other shapes depending on map/mode; keep raw JsonElement to avoid deserialization errors
    public System.Text.Json.JsonElement Items { get; set; }
}

public class LivePlayerItem
{
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = "";

    [JsonPropertyName("itemID")]
    public int ItemId { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }
}

public class LivePlayerScores
{
    [JsonPropertyName("kills")]
    public int Kills { get; set; }

    [JsonPropertyName("deaths")]
    public int Deaths { get; set; }

    [JsonPropertyName("assists")]
    public int Assists { get; set; }

    [JsonPropertyName("creepScore")]
    public int CreepScore { get; set; }

    [JsonPropertyName("wardScore")]
    public double WardScore { get; set; }
}

public class LiveGameData
{
    [JsonPropertyName("gameMode")]
    public string GameMode { get; set; } = "";

    [JsonPropertyName("gameTime")]
    public double GameTime { get; set; }
}

public class LiveEvents
{
    [JsonPropertyName("Events")]
    public List<LiveEvent>? Events { get; set; }
}

public class LiveEvent
{
    [JsonPropertyName("EventName")]
    public string EventName { get; set; } = "";

    [JsonPropertyName("KillerName")]
    public string? KillerName { get; set; }

    [JsonPropertyName("KillerTeam")]
    public string? KillerTeam { get; set; }

    [JsonPropertyName("Recipient")]
    public string? Recipient { get; set; }

    [JsonPropertyName("RecipientTeam")]
    public string? RecipientTeam { get; set; }
}

public class LiveActivePlayer
{
    [JsonPropertyName("riotIdGameName")]
    public string RiotIdGameName { get; set; } = "";

    [JsonPropertyName("riotId")]
    public string RiotId { get; set; } = "";
}
