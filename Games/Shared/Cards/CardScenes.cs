using ArC.CardGames.Components;
using Godot;
using static SceneHelper;

/// <summary>
/// Makes card nodes. <see cref="Create"/> uses the card's game's card scene (its CardGameStyle, e.g. Vanguard's with
/// power and critical); <see cref="CreateBase"/> the base card scene, art only (e.g. a deck list).
/// </summary>
public static class CardScenes
{
    public const string BaseCardScene = "res://Games/Shared/Cards/Card.tscn";

    /// <summary>A node showing <paramref name="card"/> as its game shows cards in game.</summary>
    public static Card Create(CardBase card)
    {
        var scene = CardStyles.For(card).CardScene;
        if(scene is null) return CreateBase(card);
        var node = scene.Instantiate<Card>();
        node.LoadCard(card);
        return node;
    }

    /// <summary>A node showing <paramref name="card"/>'s art only.</summary>
    public static Card CreateBase(CardBase card)
    {
        var scene = GetScene<Card>(BaseCardScene);
        scene.LoadCard(card);
        return scene;
    }
}
