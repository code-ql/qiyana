using System.Collections.Generic;

namespace qiyana.Models.GameData;

public class ChampionDetail
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Alias { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string ShortBio { get; init; } = string.Empty;
    public string SquarePortraitPath { get; init; } = string.Empty;
    public Ability? Passive { get; init; }
    public List<Ability> Spells { get; init; } = [];
}

public class Ability
{
    public string SpellKey { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string IconPath { get; init; } = string.Empty;
    public string Cooldown { get; init; } = string.Empty;
    public string Cost { get; init; } = string.Empty;
}
