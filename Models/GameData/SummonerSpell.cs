namespace qiyana.Models.GameData;

public class SummonerSpell
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int SummonerLevel { get; init; }
    public int Cooldown { get; init; }
    public string[] GameModes { get; init; } = [];
    public string IconPath { get; init; } = string.Empty;
}
