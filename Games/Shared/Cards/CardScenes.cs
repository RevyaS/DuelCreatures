using System;
using ArC.CardGames.Components;
using static SceneHelper;

// How shared components make a card node for a game's card. A game sets Create once (e.g.
// VanguardPresentationConfig.Register uses Vanguard's card scene, with its stats); the default
// is the base card scene, which shows the card's art only.
public static class CardScenes
{
    public const string BaseCardScene = "res://Games/Shared/Cards/Card.tscn";

    public static Func<CardBase, Card> Create { get; set; } = CreateBase;

    public static Card CreateBase(CardBase card)
    {
        var scene = GetScene<Card>(BaseCardScene);
        scene.LoadCard(card);
        return scene;
    }
}
