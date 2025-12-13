using System.Collections.Generic;
using ArC.CardGames.Predefined.Vanguard;
using ArC.CardGames.Predefined.Vanguard.Flow;
using Godot;

public class GodotLogger : IVanguardLogger
{
    public bool Enabled { get; set; }

    public void AddLog(string log)
    {
        if(Enabled)
        {
            GD.Print(log);
        }
    }

    public void AddLogAttacks(VanguardPlayArea attackerPlayArea, UnitCircle attacker, VanguardPlayArea targetPlayArea, UnitCircle target)
    {
    }

    public void AddLogBoostedBy(VanguardPlayArea playArea, UnitCircle booster)
    {
    }

    public void AddLogCall(VanguardCard card, VanguardPlayArea playArea, UnitCircle placedAt)
    {
    }

    public void AddLogCurrentPower(VanguardPlayArea playArea, UnitCircle unitCircle)
    {
    }

    public void AddLogDamageChecked(VanguardCard card)
    {
    }

    public void AddLogDeckSize(int remainingSize)
    {
    }

    public void AddLogDriveChecked(VanguardCard card)
    {
    }

    public void AddLogGuards(List<VanguardCard> guardCards)
    {
    }

    public void AddLogHealedCard(VanguardCard card)
    {
    }

    public void AddLogOppCurrentDamage(VanguardPlayArea playArea)
    {
    }

    public void AddLogPhase(string phaseName)
    {
    }

    public void AddLogPlayerCurrentDamage(VanguardPlayArea playArea)
    {
    }

    public void AddLogProvidedTriggerCriticalTo(VanguardPlayArea playArea, UnitCircle unitCircle, int criticalAdded)
    {
    }

    public void AddLogProvidedTriggerPowerTo(VanguardPlayArea playArea, UnitCircle unitCircle, int powerAdded)
    {
    }

    public void AddLogRGSwap(VanguardPlayArea playArea, UnitCircle swap1, UnitCircle swap2)
    {
    }
}