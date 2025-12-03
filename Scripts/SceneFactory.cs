using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Vanguard;
using Godot;
using static SceneHelper;

public static class SceneFactory
{
    public static VanguardCardComponent CreateVanguardCard(VanguardCard card)
    {
        var scene = GetScene<VanguardCardComponent>("res://Card/VanguardCard.tscn");
        scene.LoadVanguardCard(card);
        return scene;
    }

    public static Card CreateCard(CardBase card)
    {
        var scene = GetScene<Card>("res://Card/Card.tscn");
        scene.LoadVanguardCard(card);
        return scene;
    }
}