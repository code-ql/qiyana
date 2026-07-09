using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using qiyana.Models;
using qiyana.Models.MatchData;
using qiyana.Models.SgpData;

namespace qiyana.Services;

public class SgpApiClient
{
    private readonly LcuApiClient _lcuApi;
    private readonly HttpClient _http;
    private SgpServersConfig? _serverConfig;
    private string? _currentEntitlementToken;
    private string _sgpServerId = string.Empty;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SgpApiClient(LcuApiClient lcuApi)
    {
        _lcuApi = lcuApi;
        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(
            "LeagueOfLegendsClient/14.13.596.7996 (rcp-be-lol-match-history)");
        _http.Timeout = TimeSpan.FromSeconds(15);
    }

    public async Task<bool> TryInitializeAsync(LCUInfo? lcuInfo)
    {
        _serverConfig = await LoadServerConfigAsync();
        if (_serverConfig is null)
            return false;

        _sgpServerId = GetSgpServerId(lcuInfo);
        if (string.IsNullOrEmpty(_sgpServerId))
            return false;

        var server = GetServerEntry(_sgpServerId);
        if (server?.MatchHistory is null)
            return false;

        var token = await _lcuApi.GetEntitlementsTokenAsync();
        if (token?.AccessToken is null)
            return false;

        _currentEntitlementToken = token.AccessToken;

        return true;
    }

    public bool IsSupported() =>
        _serverConfig is not null &&
        !string.IsNullOrEmpty(_sgpServerId) &&
        GetServerEntry(_sgpServerId)?.MatchHistory is not null &&
        _currentEntitlementToken is not null;

    public void UpdateEntitlementToken(string accessToken)
    {
        _currentEntitlementToken = accessToken;
    }

    public async Task RefreshEntitlementTokenAsync()
    {
        var token = await _lcuApi.GetEntitlementsTokenAsync();
        if (token?.AccessToken is not null)
            _currentEntitlementToken = token.AccessToken;
    }

    public async Task<List<GameDetail>> GetMatchHistoryAsync(
        string puuid, int start, int count, string? tag, string? currentPuuid)
    {
        if (_currentEntitlementToken is null || _serverConfig is null)
            return [];

        var serverEntry = GetServerEntry(_sgpServerId);
        if (serverEntry?.MatchHistory is null)
            return [];

        var serverUrl = serverEntry.MatchHistory.TrimEnd('/');

        var url = $"{serverUrl}/match-history-query/v1/products/lol/player/{puuid}/SUMMARY" +
                  $"?startIndex={start}&count={count}";
        if (!string.IsNullOrEmpty(tag))
            url += $"&tag={tag}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _currentEntitlementToken);

        try
        {
            var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var sgpResponse = JsonSerializer.Deserialize<SgpMatchHistoryLol>(json, JsonOpts);

            if (sgpResponse?.Games is null || sgpResponse.Games.Count == 0)
                return [];

            var result = new List<GameDetail>();
            foreach (var sg in sgpResponse.Games)
            {
                if (sg.Json is null) continue;
                var detail = SgpDataMapper.ToGameDetail(sg.Json, currentPuuid);
                App.GameData.ResolveParticipantNames(detail);
                result.Add(detail);
            }

            return result;
        }
        catch
        {
            return [];
        }
    }

    private static string GetSgpServerId(LCUInfo? info)
    {
        if (info is null) return "";
        if (info.Region == "TENCENT" && !string.IsNullOrEmpty(info.RsoPlatformId))
            return $"TENCENT_{info.RsoPlatformId}";
        return info.Region;
    }

    private SgpServerEntry? GetServerEntry(string sgpServerId)
    {
        if (_serverConfig is null) return null;
        _serverConfig.Servers.TryGetValue(sgpServerId, out var server);
        return server;
    }

    private static async Task<SgpServersConfig?> LoadServerConfigAsync()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "qiyana.Assets.sgp.league-servers.json";
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null) return null;
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();
            return JsonSerializer.Deserialize<SgpServersConfig>(json, JsonOpts);
        }
        catch
        {
            return null;
        }
    }
}
