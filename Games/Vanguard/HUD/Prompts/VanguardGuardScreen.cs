using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Vanguard;
using Godot;

/// <summary>
/// Guarding: the battle on top (%BattleSummary), then one %Grid of what can guard: the hand cards that can be called
/// to the guardian circle (Hand tag, their shield shown) and the front rear-guards that can intercept (Intercept tag).
/// A tap picks one (numbered) and shows its details, again drops it; the defender's overall power previews the picked
/// shields on every pick. %Buttons: OK (Guard) answers the picks, Cancel (No Guard) answers none. Nothing moves until
/// then. Laid out in VanguardGuardScreen.tscn.
/// </summary>
public partial class VanguardGuardScreen : InputScreen
{
    VanguardBattleSummary summary = null!;
    PromptHeader Prompt = null!;
    SelectionGrid grid = null!;
    PromptButtons buttons = null!;

    /// <summary>A card was tapped: show its details.</summary>
    public event Action<CardBase>? CardPressed;

    public override void _Ready()
    {
        base._Ready();
        summary = GetNode<VanguardBattleSummary>("%BattleSummary");
        Prompt = GetNode<PromptHeader>($"%{nameof(Prompt)}");
        grid = GetNode<SelectionGrid>("%Grid");
        buttons = GetNode<PromptButtons>("%Buttons");
        grid.CardPressed += card => CardPressed?.Invoke(card.Card);
        summary.CardPressed += card => CardPressed?.Invoke(card);
    }

    /// <summary>
    /// The guardians (from <paramref name="guardians"/>, hand cards) and interceptors (from <paramref name="interceptors"/>,
    /// rear-guards) picked against <paramref name="attacker"/>'s attack on <paramref name="target"/>; none for No Guard.
    /// </summary>
    public async Task<(List<VanguardCard> Guardians, List<RearGuard> Interceptors)> Stage(string hint, UnitCircle attacker, UnitCircle target,
        VanguardPlayArea defenderArea, IReadOnlyList<VanguardCard> guardians, IReadOnlyList<RearGuard> interceptors, VanguardCardTags tags)
    {
        Open(hint);
        var interceptingUnits = interceptors.Select(circle => (CardBase)(circle.Card ?? throw new InvalidOperationException($"An empty {circle} was offered to intercept"))).ToList();
        var offered = guardians.Cast<CardBase>().Concat(interceptingUnits).ToList();
        var selection = new CardSelection<CardBase>(offered, 0, offered.Count);
        void PreviewShield() => summary.Preview(defensePower: selection.Picks.Count == 0 ? null
            : summary.DefensePower + selection.Picks.Cast<VanguardCard>().Sum(card => card.Guard));
        try
        {
            summary.Show(attacker, target, defenderArea);
            Prompt.Hint = hint;
            Prompt.HideCount();
            buttons.ShowsOk = true;
            buttons.CanCancel = true;
            buttons.CanConfirm = true;
            var items = grid.Show(selection, card => card);
            foreach(var item in items)
            {
                item.Tag = Refs.Has(interceptingUnits, item.Card) ? tags.Intercept : tags.Hand;
                if(item.CardNode is VanguardCardComponent card) card.GuardMode = true;
            }
            selection.Changed += PreviewShield;

            if(await buttons.NextPress() == PromptButton.Cancel) return ([], []);
            var picked = selection.Picks;
            return (picked.Where(card => !Refs.Has(interceptingUnits, card)).Cast<VanguardCard>().ToList(),
                interceptors.Where(circle => Refs.Has(picked, circle.Card!)).ToList());
        }
        finally
        {
            selection.Changed -= PreviewShield;
            summary.ClearPreview();
            grid.Clear();
            Close();
        }
    }
}
