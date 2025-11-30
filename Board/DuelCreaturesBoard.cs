using ArC.CardGames;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Predefined.Vanguard;
using Godot;


public partial class DuelCreaturesBoard : Control
{
    VanguardGameSession _gameSession;

    string mulliganPhase = "Mulligan Phase";

    public override void _Ready()
    {
        SetComponents();
    }

    public void ApplySession(VanguardGameSession gameSession)
    {
        _gameSession = gameSession;
        _gameSession.OnPhaseChanged += OnPhaseChanged;

        Reset();

        OnPhaseChanged(gameSession.CurrentPhase);
    }

    private void OnPhaseChanged(IPhase phase)
    {
        switch(phase)
        {
            case MulliganPhase:
                OppPhaseIndicator.Text = mulliganPhase;
                PlayerPhaseIndicator.Text = mulliganPhase;
                break;
        };
    }

    public void Reset()
    {
        // Disable Extra fields
        foreach(var extrafield in AllExtraFields)
        {
            extrafield.Hide();
        }

        foreach(var field in AllFields)
        {
            field.ClearCard();
        }

        foreach(var damageZone in AllDamageZones)
        {
            damageZone.ClearCards();
        }

        foreach(var hand in AllHands)
        {
            hand.ClearCards();
        }

        PlayerDropZone.ClearCard();
        OppDropZone.ClearCard();
    }
}
