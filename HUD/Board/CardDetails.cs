using ArC.CardGames.Vanguard;
using Godot;

/// <summary>A card's name, stats and skill text, shared by the in-game <see cref="CardInfo"/> and the deck builder's <see cref="VanguardCardInfo"/>.</summary>
public partial class CardDetails : PanelContainer
{
    Label CardName = null!, Skill = null!;
    RichTextLabel Skills = null!;
    LabelValueContainer Grade = null!, Power = null!, Critical = null!, Guard = null!, UnitType = null!;

    /// <summary>The panel's rows, for adding controls under the skill text.</summary>
    public VBoxContainer Content { get; private set; } = null!;

    public override void _Ready()
    {
        Content = GetNode<VBoxContainer>($"%{nameof(Content)}");
        CardName = GetNode<Label>($"%{nameof(CardName)}");
        Grade = GetNode<LabelValueContainer>($"%{nameof(Grade)}");
        Power = GetNode<LabelValueContainer>($"%{nameof(Power)}");
        Critical = GetNode<LabelValueContainer>($"%{nameof(Critical)}");
        Guard = GetNode<LabelValueContainer>($"%{nameof(Guard)}");
        Skills = GetNode<RichTextLabel>($"%{nameof(Skills)}");
        UnitType = GetNode<LabelValueContainer>($"%{nameof(UnitType)}");
        Skill = GetNode<Label>($"%{nameof(Skill)}");
    }

    public void ShowCard(VanguardCard card, VanguardSkill[]? grantedSkills = null)
    {
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

        // AddText (not BBCode): skill text has brackets like [AUTO]
        Skills.Clear();
        Skills.AddText(card.Skills.Length == 0
            ? "This card has no skills"
            : VanguardSkillToStringInterpreter.ExtractSkills(card.Skills));

        if(grantedSkills is { Length: > 0 })
        {
            Skills.Newline();
            Skills.Newline();
            Skills.PushColor(PresentationConfig.GrantedSkillColor);
            Skills.AddText("Granted:");
            Skills.Newline();
            Skills.AddText(VanguardSkillToStringInterpreter.ExtractSkills(grantedSkills));
            Skills.Pop();
        }
    }
}
