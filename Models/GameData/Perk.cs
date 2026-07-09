namespace qiyana.Models.GameData;

public class Perk
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string IconPath { get; init; } = string.Empty;
    public string Tooltip { get; init; } = string.Empty;
    public string ShortDesc { get; init; } = string.Empty;
    public string LongDesc { get; init; } = string.Empty;
}
