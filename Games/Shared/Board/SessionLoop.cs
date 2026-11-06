using System;
using System.Threading.Tasks;
using ArC.CardGames.Rules;
using Godot;

public static class SessionLoop
{
    /// <summary>
    /// Plays <paramref name="session"/> phase by phase until it's over, letting the board catch up after each phase.
    /// An exception from the game is reported with its stack trace (rather than lost in an async void _Ready) and ends
    /// the loop. Returns whether the game reached its end.
    /// </summary>
    public static async Task<bool> Run(GameSession session, Presenter presenter)
    {
        try
        {
            while(!session.IsGameOver())
            {
                await session.ExecutePhase();
                await presenter.WhenIdle();
            }
            return true;
        }
        catch(Exception e)
        {
            GD.PushError($"The game stopped in {session.CurrentPhase.GetType().Name}, turn {session.TurnCount}: {e}");
            return false;
        }
    }
}
