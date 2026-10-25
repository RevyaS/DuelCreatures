using System;
using ArC.CardGames.Predefined.Vanguard;
using Godot;

public partial class CardInfo : Control
{
    CardDetails CardDetails = null!;
    Button ActivateButton = null!;
    VanguardCard CurrentCard = null!;

    public bool ShowActivateButton { get; set; } = false;

    public override void _Ready()
    {
        CardDetails = GetNode<CardDetails>($"%{nameof(CardDetails)}");
        ActivateButton = GetNode<Button>($"%{nameof(ActivateButton)}");
        // Sits under the skill text, inside the details panel
        ActivateButton.Reparent(CardDetails.Content);
        ActivateButton.Pressed += OnActivateButtonPressed;
    }

    private void OnActivateButtonPressed()
    {
        ActivationPressed?.Invoke(CurrentCard);
    }

    public override void _GuiInput(InputEvent e)
    {
        // Mouse click or touch
        if (e is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            Hide();
        }
    }

    public void Show(VanguardCard card, bool canActivate = false, VanguardSkill[]? grantedSkills = null)
    {
        CurrentCard = card;
        CardDetails.ShowCard(card, grantedSkills);

        ActivateButton.Visible = ShowActivateButton && card.HasActivationSkill;
        ActivateButton.Disabled = !canActivate;

        Show();
    }

    public event Action<VanguardCard>? ActivationPressed;
}
