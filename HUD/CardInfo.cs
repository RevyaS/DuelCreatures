using System;
using ArC.CardGames.Predefined.Vanguard;
using Godot;

public partial class CardInfo : Control
{
    Label CardName = null!;
    Button ActivateButton = null!;
    RichTextLabel Skills = null!;
    LabelValueContainer Grade = null!, Power = null!, Critical = null!, Guard = null!;
    VanguardCard CurrentCard = null!;

    public bool ShowActivateButton { get; set; } = false;

    public override void _Ready()
    {
        CardName = GetNode<Label>($"%{nameof(CardName)}");
        Grade = GetNode<LabelValueContainer>($"%{nameof(Grade)}");
        Power = GetNode<LabelValueContainer>($"%{nameof(Power)}");
        Critical = GetNode<LabelValueContainer>($"%{nameof(Critical)}");
        Guard = GetNode<LabelValueContainer>($"%{nameof(Guard)}");
        Skills = GetNode<RichTextLabel>($"%{nameof(Skills)}");
        ActivateButton = GetNode<Button>($"%{nameof(ActivateButton)}");
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

    public void Show(VanguardCard card, bool canActivate = false)
    {
        CurrentCard = card;
        CardName.Text = card.Name;
        Grade.Value = card.Grade.ToString();
        Power.Value = card.Power.ToString();
        Critical.Value = card.Critical.ToString();
        Guard.Value = card.Guard.ToString();
        if(card.Skills.Length == 0)
        {
            Skills.Text = "This card has no skills";
        } else
        {
            Skills.Text = VanguardSkillToStringInterpreter.ExtractSkills(card.Skills);
        }
        ActivateButton.Visible = ShowActivateButton && card.HasActivationSkill;
        ActivateButton.Disabled = !canActivate;

        Show();
    }

    public event Action<VanguardCard>? ActivationPressed;
}