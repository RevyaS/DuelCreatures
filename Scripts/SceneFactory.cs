using ArC.CardGames.Predefined.Vanguard;
using static SceneHelper;

public static class SceneFactory
{
    public static Card CreateVanguardCard(VanguardCard card)
    {
        var scene = GetScene<VanguardCardComponent>("res://Card/VanguardCard.tscn");
        scene.LoadVanguardCard(card);
        return scene;
    }
}