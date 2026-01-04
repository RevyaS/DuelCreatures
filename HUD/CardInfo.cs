using System;
using ArC.CardGames.Predefined.Vanguard;
using Godot;

public partial class CardInfo : Control
{
    Label CardName = null!, Skill = null!;
    Button ActivateButton = null!;
    RichTextLabel Skills = null!;
    LabelValueContainer Grade = null!, Power = null!, Critical = null!, Guard = null!, UnitType = null!;
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
        UnitType = GetNode<LabelValueContainer>($"%{nameof(UnitType)}");
        Skill = GetNode<Label>($"%{nameof(Skill)}");
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

        UnitType.Label = card.Trigger == VanguardTrigger.NONE ? "Normal Unit" : "Trigger Unit";
        UnitType.Value = card.Trigger switch
        {
            VanguardTrigger.HEAL => "Heal",
            VanguardTrigger.DRAW => "Draw",
            VanguardTrigger.CRITICAL => "Critical",
            VanguardTrigger.STAND => "Stand",
            _ => string.Empty
        };
        Skill.Text = card.Skill switch
        {
            VanguardCardSkill.INTERCEPT => "Intercept",
            VanguardCardSkill.BOOST => "Boost",
            VanguardCardSkill.TWIN_DRIVE => "Twin Drive",
            _ => string.Empty,
        };

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