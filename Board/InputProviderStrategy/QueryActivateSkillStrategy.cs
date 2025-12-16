using System;
using System.Threading.Tasks;
using ArC.CardGames.Predefined.Vanguard;

public class QueryActivateSkillStrategy(CardList CardList) : IInputProviderStrategy, IQueryActivateSkill
{
    public async Task<bool> QueryActivateSkill(VanguardCard Invoker, VanguardAutomaticSkill Skill)
    {
        CardList.Show("Select Skill to Activate", [Invoker]);
        CardList.BaseDroppable = true;
        CardList.CardsDraggable = true;
        TaskCompletionSource<bool> completionSource = new TaskCompletionSource<bool>();

        Action cardDroppedHandler = () =>
        {
            completionSource.SetResult(true);
        };
        CardList.CardDroppedOutside += cardDroppedHandler;

        Action onClosedHandler = () =>
        {
            completionSource.SetResult(false);
        };
        CardList.OnClosed += onClosedHandler;

        var result = await completionSource.Task;

        CardList.CardDroppedOutside -= cardDroppedHandler;
        CardList.OnClosed -= onClosedHandler;
        CardList.BaseDroppable = false;
        CardList.CardsDraggable = false;
        CardList.Hide();
        return result;
    }
}