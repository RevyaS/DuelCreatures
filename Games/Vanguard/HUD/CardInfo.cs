using System;
using ArC.CardGames.Vanguard;
using Godot;

public partial class CardInfo : Control
{
    CardDetails CardDetails = null!;

    public bool ShowActivateButton { get; set; } = false;

    public override void _Ready()
    {
        CardDetails = GetNode<CardDetails>($"%{nameof(CardDetails)}");
        CardDetails.ActivationPressed += OnActivation;
    }

    public void Init(VanguardBoard vanguardBoard, IVanguardPlayerInputProvider inputProvider)
    {
        CardDetails.Init(vanguardBoard, inputProvider);
    }

    private void OnActivation(VanguardCard card, VanguardActivationSkill skill)
    {
        ActivationPressed?.Invoke(card, skill);
    }

    public override void _GuiInput(InputEvent e)
    {
        // Mouse click or touch
        if (e is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            Hide();
        }
    }

    public void Show(VanguardCard card, bool skillActivationWindow = false, VanguardSkill[]? grantedSkills = null)
    {
        CardDetails.ShowCard(card, skillActivationWindow, grantedSkills);
        Show();
    }

    public event Action<VanguardCard, VanguardActivationSkill>? ActivationPressed;
}
