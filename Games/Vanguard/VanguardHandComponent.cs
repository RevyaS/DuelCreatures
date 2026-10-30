using Godot;

// The Vanguard hand: while guarding, its cards show their shield instead of their power
[Tool]
public partial class VanguardHandComponent : HandComponent
{
    private bool guardMode = false;

    public void SetGuardMode(bool value)
    {
        guardMode = value;
        Render();
    }

    protected override void ConfigureCard(Card card)
    {
        base.ConfigureCard(card);
        if(card is VanguardCardComponent vanguardCard)
        {
            vanguardCard.GuardMode = guardMode;
        }
    }
}
