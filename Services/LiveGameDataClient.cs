using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using qiyana.Models;

namespace qiyana.Services;

public class LiveGameDataClient
{
    private static readonly HttpClient _http;

    static LiveGameDataClient()
    {
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
        _http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://127.0.0.1:2999"),
            Timeout = TimeSpan.FromSeconds(5)
        };
    }

    public async Task<(LiveAllGameData? Data, int StatusCode, string? Error)> FetchAllGameDataWithStatusAsync()
    {
        try
        {
            using var resp = await _http.GetAsync("/liveclientdata/allgamedata");
            var status = (int)resp.StatusCode;
            var json = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
                return (null, status, json);
            var data = JsonSerializer.Deserialize<LiveAllGameData>(json);
            return (data, status, null);
        }
        catch (Exception ex)
        {
            return (null, 0, ex.Message);
        }
    }
}
