using System.Threading.Tasks;
using ArC.CardGames.YuGiOh;
using Godot;

/// <summary>
/// A player's life points. ArC has no life point event: the label reads them each time the game settles and shows the
/// new value in step with the board (the PresentationContext an ancestor provides), counting to it.
/// </summary>
public partial class LifePointsLabel : Label, IBindable<YuGiOhPlayArea>
{
    YuGiOhPlayArea? playArea;
    PresentationContext? presentation;
    int shown;
    int lastRead;
    Tween? counting;

    public void Bind(YuGiOhPlayArea playArea)
    {
        Unbind();
        this.playArea = playArea;
        presentation = this.DependOn<PresentationContext>();
        playArea.EventBus.Settled += OnSettled;
        lastRead = shown = playArea.LifePoints;
        Text = shown.ToString();
    }

    private Task OnSettled()
    {
        if(playArea is null) return Task.CompletedTask;
        // Read now: the game runs ahead of what is shown
        var lifePoints = playArea.LifePoints;
        if(lifePoints == lastRead) return Task.CompletedTask;
        lastRead = lifePoints;

        presentation.Present(() => CountTo(lifePoints));
        return Task.CompletedTask;
    }

    private void CountTo(int lifePoints)
    {
        counting?.Kill();
        var from = shown;
        shown = lifePoints;
        var timing = PresentationConfig.StatChange;
        if(presentation is null || !Presenter.Animates(presentation.Side) || timing.Duration <= 0)
        {
            Text = lifePoints.ToString();
            return;
        }
        counting = CreateTween();
        counting.TweenMethod(Callable.From<float>(weight => Text = Mathf.RoundToInt(Mathf.Lerp(from, lifePoints, weight)).ToString()), 0f, 1f, timing.Duration)
            .SetTrans(timing.Ease.ToTransitionType())
            .SetEase(timing.Ease.ToEaseType());
    }

    private void Unbind()
    {
        if(playArea is not null) playArea.EventBus.Settled -= OnSettled;
        playArea = null;
    }

    public override void _ExitTree()
    {
        Unbind();
    }
}
