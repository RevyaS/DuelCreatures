using ArC.CardGames.Vanguard.Flow;
using Godot;

public class GodotLogger : VanguardConsoleLogger
{
    public override void AddLog(string log)
    {
        if(Enabled)
        {
            GD.Print(log);
        }
    }
}