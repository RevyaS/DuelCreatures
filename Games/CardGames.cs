using ArC.CardGames.Vanguard;
using ArC.CardGames.YuGiOh;

/// <summary>The card games this project shows. A new game adds its card type and its style resource here.</summary>
public static class CardGames
{
    public static void RegisterStyles()
    {
        CardStyles.Register<VanguardCard>("res://Games/Vanguard/VanguardStyle.tres");
        CardStyles.Register<YuGiOhCard>("res://Games/YuGiOh/YuGiOhStyle.tres");
    }
}
