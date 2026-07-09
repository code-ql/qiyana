using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using qiyana.Models;

namespace qiyana.Services;

public sealed record AiTeamReviewResult(string TeamReview, string WinRatePrediction, string FocusPlayer, string EnemyThreat = "-");

public sealed class AgnesAiTeamReviewService
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(20),
    };

    public async Task<AiTeamReviewResult> GenerateReviewAsync(
        IEnumerable<TeammateDisplayInfo> players,
        double predictedWinRate,
        double averageSkillScore,
        CancellationToken cancellationToken = default)
    {
        if (!AgnesAiConfig.HasApiKey)
            return Error("请先在 AgnesAiConfig.ApiKey 中填写 Agnes AI Key。");

        if (string.IsNullOrWhiteSpace(AgnesAiConfig.Endpoint) || string.IsNullOrWhiteSpace(AgnesAiConfig.Model))
            return Error("Agnes AI 配置不完整，请检查 Endpoint 和 Model。");

        var team = players.Where(p => p is not null).ToList();
        if (team.Count == 0)
            return Error("暂无队友数据，无法生成 AI 团队评价。");

        try
        {
            var requestBody = new ChatCompletionRequest(
                AgnesAiConfig.Model,
                [new ChatMessage("user", BuildPrompt(team, predictedWinRate, averageSkillScore))]);

            using var request = new HttpRequestMessage(HttpMethod.Post, AgnesAiConfig.Endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AgnesAiConfig.ApiKey.Trim());
            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            using var response = await HttpClient.SendAsync(request, cancellationToken);
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return Error($"AI 评价生成失败：{(int)response.StatusCode}");

            var completion = JsonSerializer.Deserialize<ChatCompletionResponse>(responseText);
            var content = completion?.Choices?.FirstOrDefault()?.Message?.Content;
            return string.IsNullOrWhiteSpace(content)
                ? Error("AI 未返回有效评价。")
                : ParseResult(content.Trim());
        }
        catch (OperationCanceledException)
        {
            return Error("AI 评价已取消。");
        }
        catch (Exception ex)
        {
            return Error($"AI 评价生成失败：{ex.Message}");
        }
    }

    public async Task<AiTeamReviewResult> GenerateMatchupReviewAsync(
        IEnumerable<TeammateDisplayInfo> myPlayers,
        IEnumerable<TeammateDisplayInfo> enemyPlayers,
        double predictedWinRate,
        double myAverageSkillScore,
        double enemyAverageSkillScore,
        CancellationToken cancellationToken = default)
    {
        return await GenerateMatchupReviewInternalAsync(myPlayers, enemyPlayers, predictedWinRate, myAverageSkillScore, enemyAverageSkillScore, null, cancellationToken);
    }

    public async Task<AiTeamReviewResult> GenerateMatchupReviewWithLiveDataAsync(
        IEnumerable<TeammateDisplayInfo> myPlayers,
        IEnumerable<TeammateDisplayInfo> enemyPlayers,
        double predictedWinRate,
        double myAverageSkillScore,
        double enemyAverageSkillScore,
        LiveAllGameData? liveData,
        CancellationToken cancellationToken = default)
    {
        return await GenerateMatchupReviewInternalAsync(myPlayers, enemyPlayers, predictedWinRate, myAverageSkillScore, enemyAverageSkillScore, liveData, cancellationToken);
    }

    private async Task<AiTeamReviewResult> GenerateMatchupReviewInternalAsync(
        IEnumerable<TeammateDisplayInfo> myPlayers,
        IEnumerable<TeammateDisplayInfo> enemyPlayers,
        double predictedWinRate,
        double myAverageSkillScore,
        double enemyAverageSkillScore,
        LiveAllGameData? liveData,
        CancellationToken cancellationToken = default)
    {
        if (!AgnesAiConfig.HasApiKey)
            return Error("请先在 AgnesAiConfig.ApiKey 中填写 Agnes AI Key。");

        var myTeam = myPlayers.Where(p => p is not null && !string.IsNullOrWhiteSpace(p.Puuid)).ToList();
        var enemyTeam = enemyPlayers.Where(p => p is not null && !string.IsNullOrWhiteSpace(p.Puuid)).ToList();
        if (myTeam.Count == 0)
            return Error("暂无己方数据，无法生成 AI 团队评价。");

        try
        {
            var prompt = BuildMatchupPrompt(myTeam, enemyTeam, predictedWinRate, myAverageSkillScore, enemyAverageSkillScore);
        if (liveData?.AllPlayers is not null)
                prompt += BuildLiveDataSection(liveData, myTeam, enemyTeam);

            var requestBody = new ChatCompletionRequest(
                AgnesAiConfig.Model,
                [new ChatMessage("user", prompt)]);

            using var request = new HttpRequestMessage(HttpMethod.Post, AgnesAiConfig.Endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AgnesAiConfig.ApiKey.Trim());
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            using var response = await HttpClient.SendAsync(request, cancellationToken);
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return Error($"AI 评价生成失败：{(int)response.StatusCode}");

            var completion = JsonSerializer.Deserialize<ChatCompletionResponse>(responseText);
            var content = completion?.Choices?.FirstOrDefault()?.Message?.Content;
            return string.IsNullOrWhiteSpace(content)
                ? Error("AI 未返回有效评价。")
                : ParseResult(content.Trim());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Error($"AI 评价生成失败：{ex.Message}");
        }
    }

    private static string BuildLiveDataSection(
        LiveAllGameData liveData,
        IReadOnlyCollection<TeammateDisplayInfo> myTeam,
        IReadOnlyCollection<TeammateDisplayInfo> enemyTeam)
    {
        var sb = new StringBuilder();
        var gameTime = liveData.GameData?.GameTime ?? 0;
        sb.Append(CultureInfo.InvariantCulture, $"[{(int)gameTime / 60}:{(int)gameTime % 60:D2}]");

        // Team total KDA from live data
        try
        {
            int orderK = 0, orderD = 0, orderA = 0, chaosK = 0, chaosD = 0, chaosA = 0;
            foreach (var p in liveData.AllPlayers ?? [])
            {
                if (p.Scores is null) continue;
                if (p.Team == "ORDER") { orderK += p.Scores.Kills; orderD += p.Scores.Deaths; orderA += p.Scores.Assists; }
                else if (p.Team == "CHAOS") { chaosK += p.Scores.Kills; chaosD += p.Scores.Deaths; chaosA += p.Scores.Assists; }
            }
            sb.Append(CultureInfo.InvariantCulture, $" 团队KDA {orderK}/{orderD}/{orderA} vs {chaosK}/{chaosD}/{chaosA}");
        }
        catch { }
        // Neutral resources summary (compact)
        try
        {
            int orderDragons = 0, chaosDragons = 0, orderBaron = 0, chaosBaron = 0, orderHerald = 0, chaosHerald = 0, orderTurrets = 0, chaosTurrets = 0;
            if (liveData.Events is not null)
            {
                foreach (var ev in liveData.Events.Events ?? new System.Collections.Generic.List<LiveEvent>())
                {
                    switch (ev.EventName)
                    {
                        case "DragonKill":
                            if (ev.KillerTeam == "ORDER") orderDragons++; else if (ev.KillerTeam == "CHAOS") chaosDragons++;
                            break;
                        case "BaronKill":
                            if (ev.KillerTeam == "ORDER") orderBaron++; else if (ev.KillerTeam == "CHAOS") chaosBaron++;
                            break;
                        case "HeraldKill":
                        case "RiftHeraldKill":
                            if (ev.KillerTeam == "ORDER") orderHerald++; else if (ev.KillerTeam == "CHAOS") chaosHerald++;
                            break;
                        case "TurretKilled":
                        case "TowerKilled":
                            if (ev.KillerTeam == "ORDER") orderTurrets++; else if (ev.KillerTeam == "CHAOS") chaosTurrets++;
                            break;
                    }
                }
            }

            sb.Append(CultureInfo.InvariantCulture, $" 龙{orderDragons}/{chaosDragons} 大龙{orderBaron}/{chaosBaron} 先锋{orderHerald}/{chaosHerald} 塔{orderTurrets}/{chaosTurrets}");
        }
        catch { }
        sb.AppendLine();
        return sb.ToString();
    }

    private static string BuildPrompt(
        IReadOnlyCollection<TeammateDisplayInfo> players,
        double predictedWinRate,
        double averageSkillScore)
    {
        var sb = new StringBuilder();
        sb.AppendLine("你是英雄联盟赛前分析助手。请根据己方5名玩家近10场摘要，生成结构化赛前分析。");
        sb.AppendLine("要求：只输出中文；简短直接；不要辱骂玩家；不要编造数据外的信息。AI胜率预测必须给出百分比和一句理由。围绕哪个玩家打必须从玩家摘要中选择1名玩家，并说明原因。");
        sb.AppendLine("输出格式必须严格如下：");
        sb.AppendLine("1) 团队评价：...");
        sb.AppendLine("2) AI胜率预测：...% ...");
        sb.AppendLine("3) 团队围绕哪个玩家打胜率最高：围绕【玩家名】打，原因：...");
        sb.AppendLine(CultureInfo.InvariantCulture, $"系统参考胜率：{predictedWinRate:F0}%");
        sb.AppendLine(CultureInfo.InvariantCulture, $"团队平均评分：{averageSkillScore:F0}");
        sb.AppendLine("玩家摘要：");

        foreach (var p in players)
        {
            sb.Append("- ");
            sb.Append(string.IsNullOrWhiteSpace(p.SummonerName) ? "未知玩家" : p.SummonerName);
            var pos = TextOrDash(p.PositionDisplay);
            sb.Append(CultureInfo.InvariantCulture, $"({pos},{TextOrDash(p.ChampionDisplay)},{p.Wins}W{p.Losses}L K{p.Kills}/{p.Deaths}/{p.Assists} 评分{p.SkillScoreText:F0})");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string BuildMatchupPrompt(
        IReadOnlyCollection<TeammateDisplayInfo> myPlayers,
        IReadOnlyCollection<TeammateDisplayInfo> enemyPlayers,
        double predictedWinRate,
        double myAverageSkillScore,
        double enemyAverageSkillScore)
    {
        var sb = new StringBuilder();
        sb.AppendLine("你是英雄联盟赛中/载入阶段分析助手。请根据己方和敌方共10名玩家近10场摘要，生成结构化对战分析。");
        sb.AppendLine("要求：只输出中文；简短直接；不要辱骂玩家；不要编造数据外的信息。胜率预测必须给出百分比和一句理由。围绕打法必须从己方玩家中选1名；警惕对象必须从敌方玩家中选1名。");
        sb.AppendLine("输出格式必须严格如下：");
        sb.AppendLine("1) 团队评价：...");
        sb.AppendLine("2) AI胜率预测：...% ...");
        sb.AppendLine("3) 团队围绕哪个玩家打胜率最高：围绕【己方玩家名】打，原因：...");
        sb.AppendLine("4) 需要警惕敌方的哪个选手：警惕【敌方玩家名】，原因：...");
        sb.AppendLine(CultureInfo.InvariantCulture, $"系统参考胜率：{predictedWinRate:F0}%");
        sb.AppendLine(CultureInfo.InvariantCulture, $"己方平均评分：{myAverageSkillScore:F0}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"敌方平均评分：{enemyAverageSkillScore:F0}");
        sb.AppendLine("己方玩家：");
        AppendPlayers(sb, myPlayers);
        sb.AppendLine("敌方玩家：");
        AppendPlayers(sb, enemyPlayers);
        return sb.ToString();
    }

    private static void AppendPlayers(StringBuilder sb, IEnumerable<TeammateDisplayInfo> players)
    {
        foreach (var p in players)
        {
            sb.Append("- ");
            sb.Append(string.IsNullOrWhiteSpace(p.SummonerName) ? "未知玩家" : p.SummonerName);
            var pos = TextOrDash(p.PositionDisplay);
            sb.Append(CultureInfo.InvariantCulture, $"({pos},{TextOrDash(p.ChampionDisplay)},{p.Wins}W{p.Losses}L K{p.Kills}/{p.Deaths}/{p.Assists} 评分{p.SkillScoreText:F0})");
            sb.AppendLine();
        }
    }

    private static AiTeamReviewResult ParseResult(string content)
    {
        var teamReview = ExtractSection(content, "1)", "2)");
        var winRate = ExtractSection(content, "2)", "3)");
        var focusPlayer = ExtractSection(content, "3)", "4)");
        var enemyThreat = ExtractSection(content, "4)", null);

        return new AiTeamReviewResult(
            CleanPrefix(teamReview, "团队评价"),
            CleanPrefix(winRate, "AI胜率预测"),
            CleanPrefix(focusPlayer, "团队围绕哪个玩家打胜率最高"),
            CleanPrefix(enemyThreat, "需要警惕敌方的哪个选手"));
    }

    private static string ExtractSection(string content, string start, string? end)
    {
        var startIndex = content.IndexOf(start, StringComparison.Ordinal);
        if (startIndex < 0)
            return content;

        startIndex += start.Length;
        var endIndex = end is null ? -1 : content.IndexOf(end, startIndex, StringComparison.Ordinal);
        return (endIndex < 0 ? content[startIndex..] : content[startIndex..endIndex]).Trim();
    }

    private static string CleanPrefix(string value, string title)
    {
        value = value.Trim();
        if (value.StartsWith(title, StringComparison.Ordinal))
            value = value[title.Length..].TrimStart('：', ':', ' ');
        return string.IsNullOrWhiteSpace(value) ? "-" : value;
    }

    private static AiTeamReviewResult Error(string message) => new(message, "-", "-", "-");

    private static string TextOrDash(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value;

    private sealed record ChatCompletionRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyList<ChatMessage> Messages);

    private sealed record ChatMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed class ChatCompletionResponse
    {
        [JsonPropertyName("choices")]
        public List<ChatChoice>? Choices { get; set; }
    }

    private sealed class ChatChoice
    {
        [JsonPropertyName("message")]
        public ChatMessage? Message { get; set; }
    }
}
