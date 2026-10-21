using Godot;

public partial class GUIManager : Node
{
    public void PlayGame(PlayerDeck playerDeck, OpponentType opponentType)
    {
        PackedScene packedScene = GD.Load<PackedScene>("res://Board/Session.tscn");

        Session newScene = packedScene.Instantiate<Session>();
        newScene.Initialize(playerDeck, opponentType);
        
        Node currentScene = GetTree().CurrentScene;
        GetTree().Root.AddChild(newScene);
        GetTree().CurrentScene = newScene;
        currentScene.QueueFree();
    }
}
