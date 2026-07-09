using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using qiyana.Models;

namespace qiyana.Services;

public class LcuWebSocketClient
{
    private readonly ILcuDiscoveryService _discovery;
    private ClientWebSocket? _ws;
    private CancellationTokenSource? _cts;

    public event Action<string, string, string>? RawEventReceived; // uri, rawJson, dataJson
    public event Action<string>? GameflowPhaseChanged;
    public event Action<JsonElement>? GameflowSessionChanged;
    public event Action<JsonElement>? ChampSelectSessionChanged;
    public event Action<JsonElement>? HonorBallotChanged;
    public event Action<JsonElement>? PreEndOfGameEventChanged;
    public event Action? Reconnected;
    public event Action<string>? EntitlementTokenUpdated;
    public event Action<JsonElement>? SummonerChanged;

    public bool IsConnected => _ws?.State == WebSocketState.Open;

    public LcuWebSocketClient(ILcuDiscoveryService discovery)
    {
        _discovery = discovery;
    }

    public Task StartAsync()
    {
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;
        _ = RunAsync(ct);
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        _cts?.Cancel();
        if (_ws is not null)
        {
            try
            {
                if (_ws.State is WebSocketState.Open or WebSocketState.CloseReceived)
                    await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }
            catch { }
            _ws.Dispose();
            _ws = null;
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await ConnectAsync(ct);
                if (!ct.IsCancellationRequested)
                    await Task.Delay(3000, ct);
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task ConnectAsync(CancellationToken ct)
    {
        var info = _discovery.GetLcuInfo();
        if (info is null) return;

        _ws?.Dispose();
        _ws = new ClientWebSocket();
        _ws.Options.RemoteCertificateValidationCallback = (_, _, _, _) => true;

        var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"riot:{info.Token}"));
        _ws.Options.SetRequestHeader("Authorization", $"Basic {auth}");

        try
        {
            await _ws.ConnectAsync(new Uri($"wss://127.0.0.1:{info.Port}/"), ct);

            await SubscribeAsync("OnJsonApiEvent", ct);

            Reconnected?.Invoke();
            await ReceiveLoopAsync(ct);
        }
        catch
        {
            _ws?.Dispose();
            _ws = null;
        }
    }

    private async Task SubscribeAsync(string eventName, CancellationToken ct)
    {
        var msg = JsonSerializer.Serialize(new object[] { 5, eventName });
        var bytes = Encoding.UTF8.GetBytes(msg);
        await _ws!.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        var buffer = new byte[65536];
        var messageBuffer = new List<byte>();

        while (!ct.IsCancellationRequested && _ws?.State == WebSocketState.Open)
        {
            messageBuffer.Clear();

            WebSocketReceiveResult result;
            do
            {
                result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                messageBuffer.AddRange(new ArraySegment<byte>(buffer, 0, result.Count));
            } while (!result.EndOfMessage);

            if (result.MessageType == WebSocketMessageType.Close)
                break;

            var rawJson = Encoding.UTF8.GetString(messageBuffer.ToArray());
            ProcessMessage(rawJson);
        }
    }

    private void ProcessMessage(string rawJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() < 3)
                return;

            var eventType = root[1].GetString() ?? "";
            if (eventType != "OnJsonApiEvent")
                return;

            var eventData = root[2];
            if (!eventData.TryGetProperty("uri", out var uriProp))
                return;
            if (!eventData.TryGetProperty("data", out var data))
                return;

            var uri = uriProp.GetString() ?? "";
            var dataJson = data.ToString();

            RawEventReceived?.Invoke(uri, rawJson, dataJson);

            switch (uri)
            {
                case "/lol-gameflow/v1/gameflow-phase":
                    GameflowPhaseChanged?.Invoke(data.GetString() ?? "");
                    break;
                case "/lol-gameflow/v1/session":
                    GameflowSessionChanged?.Invoke(data);
                    break;
                case "/lol-champ-select/v1/session":
                    ChampSelectSessionChanged?.Invoke(data);
                    break;
                case "/lol-honor-v2/v1/ballot":
                    HonorBallotChanged?.Invoke(data);
                    break;
                case "/lol-pre-end-of-game/v1/currentSequenceEvent":
                    PreEndOfGameEventChanged?.Invoke(data);
                    break;
                case "/lol-summoner/v1/current-summoner":
                    SummonerChanged?.Invoke(data);
                    break;
                case "/entitlements/v1/token":
                    HandleEntitlementTokenEvent(data);
                    break;
            }
        }
        catch
        {
            // ignore malformed messages
        }
    }

    private void HandleEntitlementTokenEvent(JsonElement data)
    {
        if (data.ValueKind == JsonValueKind.Object &&
            data.TryGetProperty("accessToken", out var tokenProp))
        {
            EntitlementTokenUpdated?.Invoke(tokenProp.GetString() ?? "");
        }
    }
}
