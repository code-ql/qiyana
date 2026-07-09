namespace qiyana.Models.GameData;

public class ChampionSummary
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Alias { get; init; } = string.Empty;
    public string SquarePortraitPath { get; init; } = string.Empty;
    public string[] Roles { get; init; } = [];
}
