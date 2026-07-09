using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using qiyana.Models;
using qiyana.Models.MatchData;
using qiyana.Models.SgpData;

namespace qiyana.Services;

public class LcuApiClient
{
    private readonly ILcuDiscoveryService _discovery;
    private readonly GameDetailCache _cache = new();

    public void ClearGameDetailCache() => _cache.Clear();

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public LcuApiClient(ILcuDiscoveryService discovery)
    {
        _discovery = discovery;
    }

    private HttpClient CreateClient()
    {
        var info = _discovery.GetLcuInfo();
        if (info is null)
            throw new InvalidOperationException("LCU not running");

        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri($"{info.Protocol}://127.0.0.1:{info.Port}")
        };

        var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"riot:{info.Token}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);

        return client;
    }

    public async Task<byte[]?> GetAssetBytesAsync(string assetPath)
    {
        using var client = CreateClient();
        try
        {
            var response = await client.GetAsync(assetPath);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> GetJsonStringAsync(string path)
    {
        using var client = CreateClient();
        try
        {
            return await client.GetStringAsync(path);
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> PostAsync(string path, string? body = null)
    {
        using var client = CreateClient();
        try
        {
            var content = body is not null
                ? new StringContent(body, Encoding.UTF8, "application/json")
                : null;
            var response = await client.PostAsync(path, content);
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> PatchAsync(string path, string? body = null)
    {
        using var client = CreateClient();
        try
        {
            var content = body is not null
                ? new StringContent(body, Encoding.UTF8, "application/json")
                : null;
            var request = new HttpRequestMessage(HttpMethod.Patch, path) { Content = content };
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<SummonerInfo?> SearchByGameNameTagLineAsync(HttpClient client, string gameName, string tagLine)
    {
        var payload = JsonSerializer.Serialize(new[] { new { gameName, tagLine } });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/lol-summoner/v1/summoners/aliases", content);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var arr = doc.RootElement;
        if (arr.GetArrayLength() == 0) return null;

        var root = arr[0];

        var sid = root.GetProperty("summonerId");
        var summonerId = sid.ValueKind == JsonValueKind.Number ? sid.GetInt64().ToString() : sid.GetString() ?? "";

        var fullJson = await client.GetStringAsync($"/lol-summoner/v1/summoners/{summonerId}");
        using var fullDoc = JsonDocument.Parse(fullJson);
        var full = fullDoc.RootElement;

        var result = new SummonerInfo();
        PopulateFromSummonerJson(full, result);
        return result;
    }

    private static async Task<SummonerInfo?> SearchByNameAsync(HttpClient client, string name)
    {
        var json = await client.GetStringAsync($"/lol-summoner/v1/summoners?name={Uri.EscapeDataString(name)}");
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var result = new SummonerInfo();
        PopulateFromSummonerJson(root, result);
        return result;
    }

    public async Task<SummonerInfo?> GetCurrentSummonerAsync()
    {
        using var client = CreateClient();

        SummonerInfo? result;
        try
        {
            var json = await client.GetStringAsync("/lol-summoner/v1/current-summoner");
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            result = new SummonerInfo();
            PopulateFromSummonerJson(root, result);
        }
        catch
        {
            return null;
        }

        await LoadRankedStatsAsync(client, result);
        return result;
    }

    public async Task<SummonerInfo?> GetSummonerByPuuidAsync(string puuid)
    {
        using var client = CreateClient();
        try
        {
            var json = await client.GetStringAsync($"/lol-summoner/v2/summoners/puuid/{puuid}");
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var result = new SummonerInfo();
            PopulateFromSummonerJson(root, result);
            await LoadRankedStatsAsync(client, result);
            return result;
        }
        catch
        {
            return null;
        }
    }

    public async Task<SummonerInfo?> SearchSummonerAsync(string name)
    {
        using var client = CreateClient();

        SummonerInfo? result;

        if (name.Contains('#'))
        {
            var parts = name.Split('#', 2);
            result = await SearchByGameNameTagLineAsync(client, parts[0].Trim(), parts[1].Trim());
        }
        else
        {
            result = await SearchByNameAsync(client, name);
        }

        if (result is null)
            return null;

        await LoadRankedStatsAsync(client, result);
        return result;
    }

    private static void PopulateFromSummonerJson(JsonElement root, SummonerInfo result)
    {
        result.Name = root.GetProperty("displayName").GetString() ?? "";
        result.DisplayName = result.Name;
        result.Puuid = root.GetProperty("puuid").GetString() ?? "";
        var sid = root.GetProperty("summonerId");
        result.SummonerId = sid.ValueKind == JsonValueKind.Number ? sid.GetInt64().ToString() : sid.GetString() ?? "";
        result.ProfileIconId = root.GetProperty("profileIconId").GetInt32();
        result.SummonerLevel = root.GetProperty("summonerLevel").GetInt64();
        result.AccountId = root.GetProperty("accountId").GetInt64();

        if (root.TryGetProperty("gameName", out var gn))
            result.GameName = gn.GetString() ?? "";
        if (root.TryGetProperty("internalName", out var inn))
            result.InternalName = inn.GetString() ?? "";
        if (root.TryGetProperty("nameChangeFlag", out var ncf))
            result.NameChangeFlag = ncf.GetBoolean();
        if (root.TryGetProperty("percentCompleteForNextLevel", out var pcl))
            result.PercentCompleteForNextLevel = pcl.GetInt32();
        if (root.TryGetProperty("privacy", out var priv))
            result.Privacy = priv.GetString() ?? "";
        if (root.TryGetProperty("tagLine", out var tl))
            result.TagLine = tl.GetString() ?? "";
        if (root.TryGetProperty("unnamed", out var un))
            result.Unnamed = un.GetBoolean();
        if (root.TryGetProperty("xpSinceLastLevel", out var xpSl))
            result.XpSinceLastLevel = xpSl.GetInt64();
        if (root.TryGetProperty("xpUntilNextLevel", out var xpUl))
            result.XpUntilNextLevel = xpUl.GetInt64();

        if (root.TryGetProperty("rerollPoints", out var rp))
        {
            if (rp.TryGetProperty("currentPoints", out var cp))
                result.RerollPointsCurrent = cp.GetInt32();
            if (rp.TryGetProperty("maxRolls", out var mr))
                result.RerollPointsMax = mr.GetInt32();
            if (result.RerollPointsMax <= 0 && rp.TryGetProperty("pointsToRoll", out var ptr))
                result.RerollPointsMax = ptr.GetInt32();
        }
    }

    private static RankedQueueData? ParseQueueEntry(JsonElement entry)
    {
        var tier = entry.GetProperty("tier").GetString();
        if (string.IsNullOrEmpty(tier) || tier == "NONE")
            return null;

        var div = entry.GetProperty("division").GetString();
        return new RankedQueueData
        {
            Tier = tier,
            Rank = string.IsNullOrEmpty(div) || div is "NONE" or "NA" ? null : div,
            LeaguePoints = entry.GetProperty("leaguePoints").GetInt32(),
            Wins = entry.GetProperty("wins").GetInt32(),
            Losses = entry.GetProperty("losses").GetInt32(),
        };
    }

    private static async Task LoadRankedStatsAsync(HttpClient client, SummonerInfo result)
    {
        try
        {
            var rankJson = await client.GetStringAsync($"/lol-ranked/v1/ranked-stats/{result.Puuid}");
            using var rankDoc = JsonDocument.Parse(rankJson);
            var ranked = rankDoc.RootElement;
            if (ranked.TryGetProperty("queueMap", out var queueMap))
            {
                if (queueMap.TryGetProperty("RANKED_SOLO_5x5", out var solo))
                {
                    result.SoloQueue = ParseQueueEntry(solo);

                    if (solo.TryGetProperty("highestTier", out var ht))
                    {
                        var hts = ht.GetString();
                        result.HighestTier = string.IsNullOrEmpty(hts) || hts == "NONE" ? null : hts;
                    }
                    if (solo.TryGetProperty("highestDivision", out var hd))
                    {
                        var hds = hd.GetString();
                        result.HighestRank = string.IsNullOrEmpty(hds) || hds is "NONE" or "NA" ? null : hds;
                    }
                }

                if (queueMap.TryGetProperty("RANKED_FLEX_SR", out var flex))
                {
                    result.FlexQueue = ParseQueueEntry(flex);
                }
            }
        }
        catch
        {
            // not ranked
        }
    }

    public async Task<GameDetail?> GetGameDetailAsync(long gameId)
    {
        var cached = _cache.Get(gameId);
        if (cached is not null)
            return cached;

        using var client = CreateClient();
        try
        {
            var json = await client.GetStringAsync($"/lol-match-history/v1/games/{gameId}");
            var detail = JsonSerializer.Deserialize<GameDetail>(json, JsonOpts);
            if (detail is null) return null;

            ResolveParticipantIdentities(detail, null);
            detail.ApplyTeamHighlights();
            _cache.Set(gameId, detail);
            return detail;
        }
        catch
        {
            return null;
        }
    }

    public async Task<EntitlementsToken?> GetEntitlementsTokenAsync()
    {
        using var client = CreateClient();
        try
        {
            var json = await client.GetStringAsync("/entitlements/v1/token");
            return JsonSerializer.Deserialize<EntitlementsToken>(json, JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> GetLeagueSessionTokenAsync()
    {
        using var client = CreateClient();
        try
        {
            return await client.GetStringAsync("/lol-league-session/v1/league-session-token");
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<GameDetail>> GetMatchHistoryAsync(string puuid, int begIndex, int endIndex, string? queueParam = null)
    {
        using var client = CreateClient();
        var url = $"/lol-match-history/v1/products/lol/{puuid}/matches?begIndex={begIndex}&endIndex={endIndex}";

        var json = await client.GetStringAsync(url);
        var response = JsonSerializer.Deserialize<MatchHistoryResponse>(json, JsonOpts);
        if (response?.Games?.Games is null)
            return [];

        var result = new List<GameDetail>();
        foreach (var game in response.Games.Games)
        {
            ResolveParticipantIdentities(game, puuid);
            result.Add(game);
        }

        return result;
    }

    private static void ResolveParticipantIdentities(GameDetail game, string? currentPuuid)
    {
        var identityByPid = game.ParticipantIdentities
            .Where(id => id.Player is not null)
            .ToDictionary(id => id.ParticipantId, id => id.Player!);

        foreach (var p in game.Participants)
        {
            if (identityByPid.TryGetValue(p.ParticipantId, out var player))
            {
                p.SummonerName = player.SummonerName;
                p.SummonerTagline = player.TagLine;
                p.Puuid = player.Puuid;
                p.IsCurrentSummoner = currentPuuid is not null &&
                    string.Equals(player.Puuid, currentPuuid, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
