using System;
using ArC.CardGames.Components;
using ArC.CardGames.Vanguard;
using Godot;

/// <summary>
/// The battle at a glance, for the trigger and guard screens: the attacking unit (%AttackerCard, a SelectableCard that
/// only shows; a tap is <see cref="CardPressed"/>), its power in this battle, boost included (%AttackerPower) and its
/// critical (%AttackerCritical), against the defender's overall power: its power plus the shield on its guardian circle
/// (%DefensePower). Every number is a StatView. It decides nothing: prompts show the battle and any would-be numbers.
/// Laid out in VanguardBattleSummary.tscn.
/// </summary>
public partial class VanguardBattleSummary : Control
{
    SelectableCard AttackerCard = null!;
    SelectableCard TargetCard = null!;
    StatView attackerPower = null!, attackerCritical = null!, defensePower = null!;

    /// <summary>The attacker's card was tapped: show its details.</summary>
    public event Action<CardBase>? CardPressed;

    public override void _Ready()
    {
        AttackerCard = GetNode<SelectableCard>($"%{nameof(AttackerCard)}");
        TargetCard = GetNode<SelectableCard>($"%{nameof(TargetCard)}");
        attackerPower = GetNode<StatView>("%AttackerPower");
        attackerCritical = GetNode<StatView>("%AttackerCritical");
        defensePower = GetNode<StatView>("%DefensePower");
        AttackerCard.Selectable = false;
        AttackerCard.Pressed += card => CardPressed?.Invoke(card.Card);
    }

    /// <summary>The battle as it is now: <paramref name="attacker"/> against <paramref name="target"/> of <paramref name="defenderArea"/>.</summary>
    public void Show(UnitCircle attacker, UnitCircle target, VanguardPlayArea defenderArea)
    {
        AttackerCard.Show(attacker.Card!);
        attackerPower.Value = attacker.GetOverallPower();
        attackerCritical.Value = attacker.GetOverallCritical();
        TargetCard.Show(target.Card!);
        DefensePower = target.GetOverallPower() + defenderArea.GuardianCircleShield;
        defensePower.Value = DefensePower;
        ClearPreview();
    }

    /// <summary>The attacker's power, critical and the defender's overall power as shown (not previewed).</summary>
    public int AttackerPower => attackerPower.Value;
    public int AttackerCritical => attackerCritical.Value;
    public int DefensePower { get; private set; }

    /// <summary>Would-be numbers (null: as shown).</summary>
    public void Preview(int? attackerPower = null, int? attackerCritical = null, int? defensePower = null)
    {
        this.attackerPower.Preview = attackerPower;
        this.attackerCritical.Preview = attackerCritical;
        this.defensePower.Preview = defensePower;
    }

    public void ClearPreview() => Preview();
}
