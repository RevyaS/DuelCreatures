using Godot;

/// <summary>
/// A card rarity. <see cref="Id"/> is what the set json files use, <see cref="Tier"/> orders rarities (0 is the lowest).
/// More than one color makes the rarity cycle through them (eg; rainbow).
/// </summary>
public sealed record Rarity(string Id, string Name, int Tier, Color[] Colors, int DustTickets)
{
    public Color Color => Colors[0];

    public override string ToString() => Name;
}
