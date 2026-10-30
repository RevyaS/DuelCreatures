using System;
using ArC.CardGames.Vanguard;
using Godot;

public partial class GUIManager : Node
{
    public static GUIManager Get(Node node) => node.GetTree().Root.GetNode<GUIManager>(nameof(GUIManager));

    public void PlayGame(VanguardDeckBuilder playerDeck, OpponentType opponentType)
    {
        ChangeScene<Session>("res://Games/Vanguard/Board/Session.tscn", session => session.Initialize(playerDeck, opponentType));
    }

    public void OpenMainScreen()
    {
        ChangeScene<MainScreen>("res://Screens/MainScreen.tscn");
    }

    /// <summary>A null index makes the deck a new one in the profile when saved.</summary>
    public void OpenDeckBuilder(DeckInfo deck, int? deckIndex)
    {
        ChangeScene<VanguardDeckBuilderScreen>("res://Screens/DeckBuilder/VanguardDeckBuilderScreen.tscn", screen => screen.Initialize(deck, deckIndex));
    }

    public void OpenGacha()
    {
        ChangeScene<GachaScreen>("res://Screens/Gacha/Gacha.tscn");
    }

    private void ChangeScene<T>(string path, Action<T>? initialize = null) where T : Node
    {
        T newScene = GD.Load<PackedScene>(path).Instantiate<T>();
        initialize?.Invoke(newScene);

        Node currentScene = GetTree().CurrentScene;
        GetTree().Root.AddChild(newScene);
        GetTree().CurrentScene = newScene;
        currentScene.QueueFree();
    }
}
