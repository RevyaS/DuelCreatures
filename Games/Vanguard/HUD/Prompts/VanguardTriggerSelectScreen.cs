using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Vanguard;
using Godot;

/// <summary>
/// Picking the unit a trigger goes to (its power, its critical, the rear-guard it stands), on the enlarged field like
/// any circle pick, with the battle on top: %BattleSummary (the attacker against the defender's overall power), the
/// revealed trigger (%TriggerCard, a tap shows its details) and what it gives (%TriggerGives, worded from the exported
/// texts). A tap picks a unit and previews what the trigger would do to the battle (power or critical to the attacker,
/// power to the defender); OK answers (set ConfirmSinglePick in the scene).
/// </summary>
public partial class VanguardTriggerSelectScreen : FieldSelectScreen
{
    /// <summary>What a power trigger gives: {0} is the amount.</summary>
    [Export] public string PowerText { get; set; } = "+{0} power";
    /// <summary>What a critical trigger gives: {0} is the amount.</summary>
    [Export] public string CriticalText { get; set; } = "+{0} critical";
    [Export] public string StandText { get; set; } = "Stand a rear-guard";

    VanguardBattleSummary summary = null!;
    SelectableCard triggerCard = null!;
    Label triggerGives = null!;

    public override void _Ready()
    {
        base._Ready();
        summary = GetNode<VanguardBattleSummary>("%BattleSummary");
        triggerCard = GetNode<SelectableCard>("%TriggerCard");
        triggerGives = GetNode<Label>("%TriggerGives");
        triggerCard.Selectable = false;
        triggerCard.Pressed += card => ShowDetailsOf(card.Card);
        summary.CardPressed += ShowDetailsOf;
    }

    /// <summary>
    /// The unit <paramref name="trigger"/>'s <paramref name="purpose"/> goes to, of <paramref name="candidates"/>, during
    /// <paramref name="battle"/> (whose target belongs to <paramref name="defenderArea"/>).
    /// </summary>
    public async Task<ICardSpace> ChooseTriggerTarget(TriggerQuestion purpose, IReadOnlyList<ICardSpace> candidates, string hint,
        AttackState battle, VanguardCard trigger, VanguardPlayArea defenderArea)
    {
        var attacker = battle.AttackingCircle!;
        var target = battle.TargetCircle!;
        summary.Show(attacker, target, defenderArea);
        triggerCard.Show(trigger);
        triggerGives.Text = purpose switch
        {
            TriggerQuestion.Power => string.Format(PowerText, trigger.TriggerPowerBonus),
            TriggerQuestion.Critical => string.Format(CriticalText, trigger.TriggerCriticalBonus),
            _ => StandText,
        };

        void Preview(IReadOnlyList<ICardSpace> picks)
        {
            summary.ClearPreview();
            if(picks.FirstOrDefault() is not { } pick) return;
            bool toAttacker = ReferenceEquals(pick, attacker), toDefender = ReferenceEquals(pick, target);
            switch(purpose)
            {
                case TriggerQuestion.Power when toAttacker:
                    summary.Preview(attackerPower: summary.AttackerPower + trigger.TriggerPowerBonus);
                    break;
                case TriggerQuestion.Power when toDefender:
                    summary.Preview(defensePower: summary.DefensePower + trigger.TriggerPowerBonus);
                    break;
                case TriggerQuestion.Critical when toAttacker:
                    summary.Preview(attackerCritical: summary.AttackerCritical + trigger.TriggerCriticalBonus);
                    break;
            }
        }

        PicksChanged += Preview;
        try
        {
            return (await ChooseZones(candidates, 1, 1, hint, canCancel: false)).Single();
        }
        finally
        {
            PicksChanged -= Preview;
            summary.ClearPreview();
        }
    }
}
