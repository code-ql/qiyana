using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Platform;
using qiyana.Models.GameData;
using qiyana.Models.MatchData;

namespace qiyana.Services;

public class GameDataService
{
    private readonly LcuApiClient _api;

    private static readonly HttpClient _externalHttp = new();

    public GameDataService(LcuApiClient api)
    {
        _api = api;
    }

    public IReadOnlyDictionary<int, ChampionSummary> Champions { get; private set; } = new Dictionary<int, ChampionSummary>();
    public IReadOnlyDictionary<int, SummonerSpell> Spells { get; private set; } = new Dictionary<int, SummonerSpell>();
    public IReadOnlyDictionary<int, Item> Items { get; private set; } = new Dictionary<int, Item>();
    public IReadOnlyDictionary<int, Perk> Perks { get; private set; } = new Dictionary<int, Perk>();
    public IReadOnlyDictionary<int, PerkStyle> Styles { get; private set; } = new Dictionary<int, PerkStyle>();
    public IReadOnlyDictionary<int, QueueInfo> Queues { get; private set; } = new Dictionary<int, QueueInfo>();
    public IReadOnlyDictionary<int, GameMap> Maps { get; private set; } = new Dictionary<int, GameMap>();

    public bool IsLoaded { get; private set; }
    public event Action? Loaded;

    private readonly ConcurrentDictionary<int, ChampionDetail> _championDetails = new();
    private readonly ConcurrentDictionary<int, Item> _itemDetails = new();
    private readonly ConcurrentDictionary<int, SummonerSpell> _spellDetails = new();
    private readonly ConcurrentDictionary<int, Perk> _perkDetails = new();
    private readonly ConcurrentDictionary<int, PerkStyle> _perkStyleDetails = new();
    private readonly ConcurrentDictionary<int, ProfileIconMeta> _profileIconMetas = new();

    private bool _isLoading;

    public async Task LoadAsync()
    {
        if (_isLoading || IsLoaded) return;
        _isLoading = true;
        try
        {
            await Task.WhenAll(
                LoadChampionsAsync(),
                LoadSpellsAsync(),
                LoadItemsAsync(),
                LoadPerksAsync(),
                LoadStylesAsync(),
                LoadQueuesAsync(),
                LoadMapsAsync()
            );
            IsLoaded = true;
            Loaded?.Invoke();
        }
        finally
        {
            _isLoading = false;
        }
    }

    public async Task<byte[]?> LoadIconBytesAsync(string? path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        var cached = LcuImageCache.Get(path);
        if (cached is not null) return cached;

        byte[]? bytes;
        if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                bytes = await _externalHttp.GetByteArrayAsync(path);
            }
            catch
            {
                return null;
            }
        }
        else if (path.StartsWith("avares://"))
        {
            try
            {
                using var stream = AssetLoader.Open(new Uri(path));
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                bytes = ms.ToArray();
            }
            catch
            {
                return null;
            }
        }
        else
        {
            bytes = await _api.GetAssetBytesAsync(path);
        }

        if (bytes is not null)
            LcuImageCache.Set(path, bytes);
        return bytes;
    }

    public byte[]? GetCachedIconBytes(string path)
    {
        return LcuImageCache.Get(path);
    }

    public static string GetChampionIconUrl(int championId)
        => $"/lol-game-data/assets/v1/champion-icons/{championId}.png";

    private static readonly Dictionary<int, string> SpellIconFallback = new()
    {
        [1] = "/lol-game-data/assets/DATA/Spells/Icons2D/Summoner_boost.png",
        [3] = "/lol-game-data/assets/DATA/Spells/Icons2D/Summoner_exhaust.png",
        [4] = "/lol-game-data/assets/DATA/Spells/Icons2D/Summoner_flash.png",
        [6] = "/lol-game-data/assets/DATA/Spells/Icons2D/Summoner_haste.png",
        [7] = "/lol-game-data/assets/DATA/Spells/Icons2D/Summoner_heal.png",
        [11] = "/lol-game-data/assets/DATA/Spells/Icons2D/Summoner_smite.png",
        [12] = "/lol-game-data/assets/DATA/Spells/Icons2D/Summoner_Teleport_New.png",
        [13] = "/lol-game-data/assets/DATA/Spells/Icons2D/SummonerMana.png",
        [14] = "/lol-game-data/assets/DATA/Spells/Icons2D/SummonerIgnite.png",
        [21] = "/lol-game-data/assets/DATA/Spells/Icons2D/SummonerBarrier.png",
        [32] = "/lol-game-data/assets/DATA/Spells/Icons2D/Summoner_Mark.png",
    };

    public static string GetSpellIconUrl(int spellId)
    {
        var spell = App.GameData.Spells.GetValueOrDefault(spellId);
        return spell?.IconPath
            ?? SpellIconFallback.GetValueOrDefault(spellId)
            ?? $"/lol-game-data/assets/v1/spell-icons/{spellId}.png";
    }

    public static string GetProfileIconUrl(int profileIconId)
        => $"/lol-game-data/assets/v1/profile-icons/{profileIconId}.jpg";

    public void ResolveParticipantNames(GameDetail detail)
    {
        foreach (var p in detail.Participants)
        {
            if (p.ChampionId > 0 && string.IsNullOrEmpty(p.ChampionName))
            {
                if (Champions.TryGetValue(p.ChampionId, out var champion))
                    p.ChampionName = champion.Name;
            }
        }
    }

    public static string GetRankedEmblemUrl(string tier)
        => $"avares://qiyana/Assets/RankedEmblems/{tier.ToLowerInvariant()}.png";

    public async Task<ChampionDetail?> GetChampionDetailAsync(int championId)
    {
        if (_championDetails.TryGetValue(championId, out var cached))
            return cached;

        var json = await _api.GetJsonStringAsync($"/lol-game-data/assets/v1/champions/{championId}.json");
        if (json is null) return null;

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var result = new ChampionDetail
        {
            Id = root.GetProperty("id").GetInt32(),
            Name = root.GetProperty("name").GetString() ?? "",
            Alias = root.GetProperty("alias").GetString() ?? "",
            Title = root.GetProperty("title").GetString() ?? "",
            ShortBio = root.GetProperty("shortBio").GetString() ?? "",
            SquarePortraitPath = root.GetProperty("squarePortraitPath").GetString() ?? "",
            Passive = ParseAbility(root.GetProperty("passive"), "P"),
            Spells = root.GetProperty("spells").EnumerateArray()
                .Select(s => ParseAbility(s, s.GetProperty("spellKey").GetString() ?? "")).ToList()
        };

        _championDetails[championId] = result;
        return result;
    }

    public async Task<Item?> GetItemDetailAsync(int itemId)
    {
        if (_itemDetails.TryGetValue(itemId, out var cached))
            return cached;

        var json = await _api.GetJsonStringAsync($"/lol-game-data/assets/v1/items/{itemId}.json");
        if (json is null) return null;

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var result = ParseItem(root);
        _itemDetails[itemId] = result;
        return result;
    }

    public async Task<SummonerSpell?> GetSpellDetailAsync(int spellId)
    {
        if (_spellDetails.TryGetValue(spellId, out var cached))
            return cached;

        var json = await _api.GetJsonStringAsync($"/lol-game-data/assets/v1/summoner-spells/{spellId}.json");
        if (json is null) return null;

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var result = ParseSpell(root);
        _spellDetails[spellId] = result;
        return result;
    }

    public async Task<Perk?> GetPerkDetailAsync(int perkId)
    {
        if (_perkDetails.TryGetValue(perkId, out var cached))
            return cached;

        var json = await _api.GetJsonStringAsync($"/lol-game-data/assets/v1/perks/{perkId}.json");
        if (json is null) return null;

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var result = ParsePerk(root);
        _perkDetails[perkId] = result;
        return result;
    }

    public async Task<PerkStyle?> GetPerkStyleDetailAsync(int styleId)
    {
        if (_perkStyleDetails.TryGetValue(styleId, out var cached))
            return cached;

        var json = await _api.GetJsonStringAsync($"/lol-game-data/assets/v1/perk-styles/{styleId}.json");
        if (json is null) return null;

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var result = new PerkStyle
        {
            Id = root.GetProperty("id").GetInt32(),
            Name = root.GetProperty("name").GetString() ?? "",
            Tooltip = root.GetProperty("tooltip").GetString() ?? "",
            IconPath = root.GetProperty("iconPath").GetString() ?? ""
        };
        _perkStyleDetails[styleId] = result;
        return result;
    }

    public async Task<ProfileIconMeta?> GetProfileIconMetaAsync(int profileIconId)
    {
        if (_profileIconMetas.TryGetValue(profileIconId, out var cached))
            return cached;

        var json = await _api.GetJsonStringAsync($"/lol-game-data/assets/v1/profile-icons/{profileIconId}.json");
        if (json is null) return null;

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var result = new ProfileIconMeta
        {
            Id = root.GetProperty("id").GetInt32(),
            Title = root.TryGetProperty("title", out var t) ? t.GetString() ?? "" : ""
        };
        _profileIconMetas[profileIconId] = result;
        return result;
    }

    private static Ability ParseAbility(JsonElement el, string spellKey)
    {
        return new Ability
        {
            SpellKey = spellKey,
            Name = el.GetProperty("name").GetString() ?? "",
            Description = el.GetProperty("description").GetString() ?? "",
            IconPath = el.GetProperty("iconPath").GetString() ?? "",
            Cooldown = el.TryGetProperty("cooldown", out var cd) ? cd.GetString() ?? "" : "",
            Cost = el.TryGetProperty("cost", out var cost) ? cost.GetString() ?? "" : ""
        };
    }

    private static Item ParseItem(JsonElement e)
    {
        return new Item
        {
            Id = e.GetProperty("id").GetInt32(),
            Name = e.GetProperty("name").GetString() ?? "",
            Description = e.GetProperty("description").GetString() ?? "",
            Active = e.GetProperty("active").GetBoolean(),
            InStore = e.GetProperty("inStore").GetBoolean(),
            From = e.TryGetProperty("from", out var from)
                ? from.EnumerateArray().Select(f => f.GetInt32()).ToArray() : [],
            To = e.TryGetProperty("to", out var to)
                ? to.EnumerateArray().Select(t => t.GetInt32()).ToArray() : [],
            Categories = e.TryGetProperty("categories", out var cats)
                ? cats.EnumerateArray().Select(c => c.GetString() ?? "").ToArray() : [],
            Price = e.GetProperty("price").GetInt32(),
            PriceTotal = e.GetProperty("priceTotal").GetInt32(),
            IconPath = e.GetProperty("iconPath").GetString() ?? ""
        };
    }

    private static SummonerSpell ParseSpell(JsonElement e)
    {
        return new SummonerSpell
        {
            Id = e.GetProperty("id").GetInt32(),
            Name = e.GetProperty("name").GetString() ?? "",
            Description = e.GetProperty("description").GetString() ?? "",
            SummonerLevel = e.GetProperty("summonerLevel").GetInt32(),
            Cooldown = e.GetProperty("cooldown").GetInt32(),
            GameModes = e.GetProperty("gameModes").EnumerateArray().Select(m => m.GetString() ?? "").ToArray(),
            IconPath = e.GetProperty("iconPath").GetString() ?? ""
        };
    }

    private static Perk ParsePerk(JsonElement e)
    {
        return new Perk
        {
            Id = e.GetProperty("id").GetInt32(),
            Name = e.GetProperty("name").GetString() ?? "",
            IconPath = e.GetProperty("iconPath").GetString() ?? "",
            Tooltip = e.GetProperty("tooltip").GetString() ?? "",
            ShortDesc = e.GetProperty("shortDesc").GetString() ?? "",
            LongDesc = e.GetProperty("longDesc").GetString() ?? ""
        };
    }

    private async Task LoadChampionsAsync()
    {
        try
        {
            var json = await _api.GetJsonStringAsync("/lol-game-data/assets/v1/champion-summary.json");
            if (json is null) return;
            using var doc = JsonDocument.Parse(json);
            Champions = doc.RootElement.EnumerateArray().Select(e => new ChampionSummary
            {
                Id = e.GetProperty("id").GetInt32(),
                Name = e.GetProperty("name").GetString() ?? "",
                Description = e.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "",
                Alias = e.GetProperty("alias").GetString() ?? "",
                SquarePortraitPath = e.GetProperty("squarePortraitPath").GetString() ?? "",
                Roles = e.GetProperty("roles").EnumerateArray().Select(r => r.GetString() ?? "").ToArray()
            }).ToDictionary(c => c.Id);
        }
        catch
        {
            // best-effort
        }
    }

    private async Task LoadSpellsAsync()
    {
        try
        {
            var json = await _api.GetJsonStringAsync("/lol-game-data/assets/v1/summoner-spells.json");
            if (json is null) return;
            using var doc = JsonDocument.Parse(json);
            Spells = doc.RootElement.EnumerateArray().Select(e => new SummonerSpell
            {
                Id = (int)e.GetProperty("id").GetUInt32(),
                Name = e.GetProperty("name").GetString() ?? "",
                Description = e.GetProperty("description").GetString() ?? "",
                SummonerLevel = e.GetProperty("summonerLevel").GetInt32(),
                Cooldown = e.GetProperty("cooldown").GetInt32(),
                GameModes = e.GetProperty("gameModes").EnumerateArray().Select(m => m.GetString() ?? "").ToArray(),
                IconPath = e.GetProperty("iconPath").GetString() ?? ""
            }).GroupBy(s => s.Id).ToDictionary(g => g.Key, g => g.Last());
        }
        catch
        {
            // best-effort
        }
    }

    private async Task LoadItemsAsync()
    {
        try
        {
            var json = await _api.GetJsonStringAsync("/lol-game-data/assets/v1/items.json");
            if (json is null) return;
            using var doc = JsonDocument.Parse(json);
            Items = doc.RootElement.EnumerateArray().Select(e => new Item
            {
                Id = e.GetProperty("id").GetInt32(),
                Name = e.GetProperty("name").GetString() ?? "",
                Description = e.GetProperty("description").GetString() ?? "",
                Active = e.GetProperty("active").GetBoolean(),
                InStore = e.GetProperty("inStore").GetBoolean(),
                From = e.TryGetProperty("from", out var from)
                    ? from.EnumerateArray().Select(f => f.GetInt32()).ToArray() : [],
                To = e.TryGetProperty("to", out var to)
                    ? to.EnumerateArray().Select(t => t.GetInt32()).ToArray() : [],
                Categories = e.TryGetProperty("categories", out var cats)
                    ? cats.EnumerateArray().Select(c => c.GetString() ?? "").ToArray() : [],
                Price = e.GetProperty("price").GetInt32(),
                PriceTotal = e.GetProperty("priceTotal").GetInt32(),
                IconPath = e.GetProperty("iconPath").GetString() ?? ""
            }).ToDictionary(i => i.Id);
        }
        catch
        {
            // best-effort
        }
    }

    private async Task LoadPerksAsync()
    {
        try
        {
            var json = await _api.GetJsonStringAsync("/lol-game-data/assets/v1/perks.json");
            if (json is null) return;
            using var doc = JsonDocument.Parse(json);
            Perks = doc.RootElement.EnumerateArray().Select(e => new Perk
            {
                Id = e.GetProperty("id").GetInt32(),
                Name = e.GetProperty("name").GetString() ?? "",
                IconPath = e.GetProperty("iconPath").GetString() ?? "",
                Tooltip = e.GetProperty("tooltip").GetString() ?? "",
                ShortDesc = e.GetProperty("shortDesc").GetString() ?? "",
                LongDesc = e.GetProperty("longDesc").GetString() ?? ""
            }).ToDictionary(p => p.Id);
        }
        catch
        {
            // best-effort
        }
    }

    private async Task LoadStylesAsync()
    {
        try
        {
            var json = await _api.GetJsonStringAsync("/lol-game-data/assets/v1/perkstyles.json");
            if (json is null) return;
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            Styles = root.GetProperty("styles").EnumerateArray().Select(e => new PerkStyle
            {
                Id = e.GetProperty("id").GetInt32(),
                Name = e.GetProperty("name").GetString() ?? "",
                Tooltip = e.GetProperty("tooltip").GetString() ?? "",
                IconPath = e.GetProperty("iconPath").GetString() ?? ""
            }).ToDictionary(s => s.Id);
        }
        catch
        {
            // best-effort
        }
    }

    private async Task LoadQueuesAsync()
    {
        try
        {
            var json = await _api.GetJsonStringAsync("/lol-game-queues/v1/queues");
            if (json is null) return;
            using var doc = JsonDocument.Parse(json);
            Queues = doc.RootElement.EnumerateArray().Select(e => new QueueInfo
            {
                Id = e.GetProperty("id").GetInt32(),
                Name = e.GetProperty("name").GetString() ?? "",
                ShortName = e.TryGetProperty("shortName", out var sn) ? sn.GetString() ?? "" : "",
                Description = e.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
                DetailedDescription = e.TryGetProperty("detailedDescription", out var dd) ? dd.GetString() ?? "" : ""
            }).ToDictionary(q => q.Id);
        }
        catch
        {
            // best-effort
        }
    }

    private async Task LoadMapsAsync()
    {
        try
        {
            var json = await _api.GetJsonStringAsync("/lol-maps/v2/maps");
            if (json is null) return;
            using var doc = JsonDocument.Parse(json);
            Maps = doc.RootElement.EnumerateArray().Select(e => new GameMap
            {
                Id = e.GetProperty("id").GetInt32(),
                Name = e.GetProperty("name").GetString() ?? "",
                Description = e.GetProperty("description").GetString() ?? "",
                MapStringId = e.GetProperty("mapStringId").GetString() ?? ""
            }).ToDictionary(m => m.Id);
        }
        catch
        {
            // best-effort
        }
    }
}
