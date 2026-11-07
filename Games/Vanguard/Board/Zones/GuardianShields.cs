using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Flow;
using ArC.CardGames.Vanguard;
using Godot;

/// <summary>
/// Shows each guardian's shield (printed, plus effects, e.g. +5000 from an interceptor's skill) instead of its power,
/// under the guardian circle's LineZone (bound to its GuardZone like any zone extension). Reads the shields each time
/// the game settles (effects have no event of their own) and shows them in step with the board.
/// </summary>
public partial class GuardianShields : Node, IBindable<GuardZone>
{
    GuardZone? guardZone;
    EventBus? eventBus;
    CardLine line = null!;
    PresentationContext? presentation;
    VanguardEffectService effects = null!;
    readonly Dictionary<CardBase, int> shields = new(ReferenceEqualityComparer.Instance);

    public void Bind(GuardZone guardZone)
    {
        Unbind();
        line = GetParent() as CardLine ?? throw new System.InvalidOperationException($"{GetPath()}: {nameof(GuardianShields)} goes directly under the guardian circle's zone");
        presentation = this.DependOn<PresentationContext>();
        var session = this.DependOn<VanguardGameSession>();
        effects = session.EffectService;
        eventBus = session.EventBus;
        this.guardZone = guardZone;
        eventBus.Settled += OnSettled;

        // Every card shown here is a guardian: its shield as last read (printed until then)
        line.DecorateCard = node =>
        {
            if(node is not VanguardCardComponent guardian || guardian.CurrentCard is not VanguardCard card) return;
            guardian.GuardMode = true;
            guardian.Guard = shields.TryGetValue(card, out var shield) ? shield : card.Guard;
        };
    }

    private Task OnSettled()
    {
        if(guardZone is null) return Task.CompletedTask;
        // Read now: the game runs ahead of what is shown
        var read = guardZone.Cards.Select(card => (card, shield: ((VanguardCard)card).Guard + effects.GetEffectTotalShield(card))).ToList();
        presentation.Present(() =>
        {
            shields.Clear();
            foreach(var (card, shield) in read) shields[card] = shield;
            line.Refresh();
        });
        return Task.CompletedTask;
    }

    private void Unbind()
    {
        if(eventBus is not null) eventBus.Settled -= OnSettled;
        eventBus = null;
        guardZone = null;
    }

    public override void _ExitTree() => Unbind();
}
