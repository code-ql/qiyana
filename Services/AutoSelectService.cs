using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace qiyana.Services;

public class AutoSelectService
{
    private readonly LcuWebSocketClient _ws;
    private readonly LcuApiClient _api;
    private readonly SettingsService _settings;

    private bool _inChampSelect;
    private readonly HashSet<int> _completedActionIds = [];

    public AutoSelectService(LcuWebSocketClient ws, LcuApiClient api, SettingsService settings)
    {
        _ws = ws;
        _api = api;
        _settings = settings;
    }

    public void Start()
    {
        _ws.GameflowPhaseChanged += OnGameflowPhaseChanged;
        _ws.ChampSelectSessionChanged += OnChampSelectSessionChanged;
    }

    public void Stop()
    {
        _ws.GameflowPhaseChanged -= OnGameflowPhaseChanged;
        _ws.ChampSelectSessionChanged -= OnChampSelectSessionChanged;
        _inChampSelect = false;
        _completedActionIds.Clear();
    }

    private void OnGameflowPhaseChanged(string phase)
    {
        _inChampSelect = phase == "ChampSelect";
        if (!_inChampSelect)
            _completedActionIds.Clear();
    }

    private async void OnChampSelectSessionChanged(JsonElement session)
    {
        if (!_inChampSelect)
        {
            if (!session.TryGetProperty("actions", out _))
                return;
            _inChampSelect = true;
        }

        try
        {
            await ProcessSessionAsync(session, _settings.Load());
        }
        catch
        {
        }
    }

    public async Task ProcessSessionAsync(JsonElement session, AppSettings settings)
    {
        session = session.Clone();

        if (!settings.AutoSelectEnabled)
            return;

        if (!session.TryGetProperty("localPlayerCellId", out var cellIdProp))
            return;
        var localCellId = cellIdProp.GetInt32();

        if (!session.TryGetProperty("actions", out var actionsProp))
            return;

        foreach (var actionGroup in actionsProp.EnumerateArray())
        {
            foreach (var action in actionGroup.EnumerateArray())
            {
                if (!action.TryGetProperty("actorCellId", out var actorProp))
                    continue;
                if (actorProp.GetInt32() != localCellId)
                    continue;

                if (!action.TryGetProperty("completed", out var completedProp))
                    continue;
                if (completedProp.GetBoolean())
                    continue;

                if (!action.TryGetProperty("id", out var idProp))
                    continue;
                var actionId = idProp.GetInt32();

                if (_completedActionIds.Contains(actionId))
                    continue;

                if (!action.TryGetProperty("type", out var typeProp))
                    continue;
                var type = typeProp.GetString();

                if (!action.TryGetProperty("isInProgress", out var inProgressProp))
                    continue;
                var isInProgress = inProgressProp.GetBoolean();

                var currentChampionId = action.TryGetProperty("championId", out var cidProp)
                    ? cidProp.GetInt32()
                    : 0;

                int[] preferredIds;
                if (type == "pick")
                    preferredIds = settings.AutoPickChampionIds;
                else if (type == "ban")
                    preferredIds = settings.AutoBanChampionIds;
                else
                    continue;

                var championId = FindAvailable(session, type, preferredIds);
                if (!championId.HasValue)
                    continue;

                if (currentChampionId == 0)
                {
                    var payload = type == "ban"
                        ? $"{{\"type\":\"ban\",\"championId\":{championId.Value},\"completed\":false}}"
                        : $"{{\"championId\":{championId.Value}}}";
                    await _api.PatchAsync(
                        $"/lol-champ-select/v1/session/actions/{actionId}",
                        payload);
                }
                else if (isInProgress)
                {
                    var payload =
                        $"{{\"type\":\"{type}\",\"championId\":{championId.Value},\"completed\":true}}";
                    var ok = await _api.PatchAsync(
                        $"/lol-champ-select/v1/session/actions/{actionId}",
                        payload);
                    if (ok)
                        _completedActionIds.Add(actionId);
                }
            }
        }
    }

    private static int? FindAvailable(JsonElement session, string type, int[] preferredIds)
    {
        var key = type == "ban" ? "bannableChampionIds" : "pickableChampionIds";
        var available = session.TryGetProperty(key, out var prop)
            ? prop.EnumerateArray().Select(p => p.GetInt32()).ToHashSet()
            : null;

        foreach (var id in preferredIds)
        {
            if (available is null || available.Contains(id))
                return id;
        }

        return null;
    }
}
