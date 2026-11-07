using System.Threading.Tasks;
using ArC.CardGames.Vanguard;
using Godot;

/// <summary>
/// Shows a unit's power and critical with every effect on it, under a unit circle zone (bound to its UnitCircle like
/// any zone extension). Effects change without an event of their own (e.g. "+4000 while your hand has 4+ cards"), so
/// it reads them each time the game settles and shows them in step with the board, counting to the new values.
/// </summary>
public partial class UnitStats : Node, IBindable<UnitCircle>
{
    UnitCircle? circle;
    CardArea zone = null!;
    PresentationContext? presentation;

    public void Bind(UnitCircle circle)
    {
        Unbind();
        zone = GetParent() as CardArea ?? throw new System.InvalidOperationException($"{GetPath()}: {nameof(UnitStats)} goes directly under a unit circle zone");
        presentation = this.DependOn<PresentationContext>();
        this.circle = circle;
        circle.EventBus.Settled += OnSettled;
    }

    private Task OnSettled()
    {
        // Read now: the game runs ahead of what is shown
        if(circle?.Card is not { } card) return Task.CompletedTask;
        var power = circle.GetOverallPower();
        var critical = circle.GetOverallCritical();
        presentation.Present(() => Show(card, power, critical));
        return Task.CompletedTask;
    }

    private async Task Show(VanguardCard card, int power, int critical)
    {
        if(zone.FindCardNode(card) is not VanguardCardComponent node) return;
        var oldPower = node.Power;
        var oldCritical = node.Critical;
        if(oldPower == power && oldCritical == critical) return;

        if(power > oldPower) presentation.PlaySfx(VanguardSfx.PowerUp);
        if(critical > oldCritical) presentation.PlaySfx(VanguardSfx.CriticalUp);

        if(presentation?.Presenter.Flights is { } flights && Presenter.Animates(presentation.Side))
        {
            var panel = node.StatsPanel;
            panel.PivotOffset = panel.Size / 2;
            await Task.WhenAll(
                flights.PlayKeyframes(panel, PresentationConfig.StatChangePulse),
                flights.Animate(PresentationConfig.StatChange, weight =>
                {
                    if(!IsInstanceValid(node)) return;
                    node.Power = Mathf.RoundToInt(Mathf.Lerp(oldPower, power, weight));
                    node.Critical = Mathf.RoundToInt(Mathf.Lerp(oldCritical, critical, weight));
                }));
        }
        if(IsInstanceValid(node))
        {
            node.Power = power;
            node.Critical = critical;
        }
    }

    private void Unbind()
    {
        if(circle is not null) circle.EventBus.Settled -= OnSettled;
        circle = null;
    }

    public override void _ExitTree() => Unbind();
}
