using ArC.CardGames.Predefined.Vanguard;
using Godot;
using static SceneHelper;

public static class SceneFactory
{
    public static Card CreateVanguardCard(VanguardCard card)
    {
        var scene = GetScene<VanguardCardComponent>("res://Card/VanguardCard.tscn");
        scene.LoadVanguardCard(card);
        return scene;
    }

    public static Card CreateCard(Texture2D texture2D, bool isFront)
    {
        var scene = GetScene<Card>("res://Card/Card.tscn");
        scene.Texture = texture2D;
        scene.IsFront = isFront;
        return scene;
    }
}