using ArC.CardGames.Vanguard;
using Godot;
using static SceneHelper;

public static class SceneFactory
{
    public static VanguardCardComponent CreateVanguardCard(VanguardCard card)
    {
        var scene = GetScene<VanguardCardComponent>("res://Games/Vanguard/Cards/VanguardCard.tscn");
        scene.LoadCard(card);
        return scene;
    }
}