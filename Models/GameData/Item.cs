namespace qiyana.Models.GameData;

public class Item
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool Active { get; init; }
    public bool InStore { get; init; }
    public int[] From { get; init; } = [];
    public int[] To { get; init; } = [];
    public string[] Categories { get; init; } = [];
    public int Price { get; init; }
    public int PriceTotal { get; init; }
    public string IconPath { get; init; } = string.Empty;
}
