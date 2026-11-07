using System.Collections.Generic;
using System.Linq;
using Godot;

namespace DuelCreatures.Data;

/// <summary>
/// How one card game's cards look, in one resource to edit in the inspector (e.g. Games/Vanguard/VanguardStyle.tres):
/// the scene a card is shown with in game, the folder its art is in, its default sleeve and the sleeves a player may
/// pick from. CardStyles finds a card's style from the card's type.
/// </summary>
[GlobalClass]
[Tool]
public partial class CardGameStyle : Resource
{
    /// <summary>The game's key in a profile (the player's sleeve per game). Keep it once players have profiles.</summary>
    [Export] public string Id { get; set; } = string.Empty;

    /// <summary>The scene a card is shown with in game (its root a Card, e.g. with stats on it); none: the base card, art only.</summary>
    [Export] public PackedScene? CardScene { get; set; }

    /// <summary>The folder holding the game's card art, one image per card: its name without spaces and punctuation, as a .png.</summary>
    [Export(PropertyHint.Dir)] public string ArtFolder { get; set; } = "res://Assets/Cards";

    /// <summary>The sleeve a player has until they pick one, and the opponent's.</summary>
    [Export] public SleeveInfo? DefaultSleeve { get; set; }

    /// <summary>The other sleeves a player may pick for this game.</summary>
    [Export] public Godot.Collections.Array<SleeveInfo> Sleeves { get; set; } = [];

    /// <summary>Every sleeve a player may pick: the default first, then the others.</summary>
    public IReadOnlyList<SleeveInfo> SleeveChoices => new[] { DefaultSleeve }.Concat(Sleeves)
        .OfType<SleeveInfo>()
        .DistinctBy(sleeve => sleeve.ResourcePath)
        .ToList();

    readonly HashSet<string> missingArtWarned = [];

    /// <summary>
    /// The art of the card named <paramref name="cardName"/>, or null (with a warning once per card) while the game's
    /// art isn't all in the project yet.
    /// </summary>
    public Texture2D? ArtOf(string cardName)
    {
        var path = $"{ArtFolder.TrimEnd('/')}/{ArtFileName(cardName)}.png";
        if(ResourceLoader.Exists(path)) return ResourceLoader.Load<Texture2D>(path);
        // Load would log an error for every card node and every flight
        if(missingArtWarned.Add(path)) GD.PushWarning($"No card art for \"{cardName}\" ({path}); the card is shown without it");
        return null;
    }

    /// <summary>The art's file name for a card: its name without spaces and punctuation ("Blaster Blade" -> "BlasterBlade").</summary>
    public static string ArtFileName(string cardName) => cardName.Replace(" ", "")
        .Replace(",", "")
        .Replace(".", "")
        .Replace("&", "")
        .Replace("-", "")
        .Replace("\"", "")
        .Replace("'", "");
}
