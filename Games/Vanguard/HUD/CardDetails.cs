using System;
using System.Linq;
using ArC.CardGames.Vanguard;
using Godot;

/// <summary>A card's name, stats and skill text, shared by the in-game <see cref="CardInfo"/> and the deck builder's <see cref="VanguardCardInfo"/>.</summary>
public partial class CardDetails : PanelContainer
{
    Label CardName = null!, Skill = null!;
    RichTextLabel Skills = null!, GrantedSkills = null!;
    VBoxContainer ActivationSkills = null!;
    LabelValueContainer Grade = null!, Power = null!, Critical = null!, Guard = null!, UnitType = null!;

    /// <summary>The panel's rows, for adding controls under the skill text.</summary>
    public VBoxContainer Content { get; private set; } = null!;
    VanguardBoard _board = null!;
    VanguardBoard Board => _board ?? throw new Exception("Board is not setup, please call CardDetails.Init() before any operations");
    IVanguardPlayerInputProvider _inputProvider = null!;
    IVanguardPlayerInputProvider InputProvider => _inputProvider ?? throw new Exception("InputProvider is not setup, please call CardDetails.Init() before any operations");

    public bool ReadOnlyMode { get; set; } = false;

    public event Action<VanguardCard, VanguardActivationSkill>? ActivationPressed;

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
        GrantedSkills = GetNode<RichTextLabel>($"%{nameof(GrantedSkills)}");
        ActivationSkills = GetNode<VBoxContainer>($"%{nameof(ActivationSkills)}");
    }

    public void Init(VanguardBoard board, IVanguardPlayerInputProvider inputProvider)
    {
        _board = board;
        _inputProvider = inputProvider;
    }

    private bool IsSkillManuallyActivatable(VanguardCard card, VanguardActivationSkill skill)
    {
        // From Unit Circle
        if(Board.IsCardInPlayerUnitCircle(card))
        {
            var unitCircle = Board.GetPlayerUnitCircleComponent(card).UnitCircle;
            return VanguardGameRules.UnitCircleCanActivateSkill(InputProvider, unitCircle, skill);
        } else
        {
            // From hand
            return VanguardGameRules.HandCardCanActivateSkill(InputProvider, Board.PlayerHand.Hand, card, skill);
        }
    }

    private void populateReadOnlyMode(VanguardCard card)
    {
        Skills.AddText(card.Skills.Length == 0
            ? "This card has no skills"
            : VanguardSkillToStringInterpreter.ExtractSkills(card.Skills));
    }
    private void populateActivatableMode(VanguardCard card, bool skillActivationWindow = false)
    {
        // AddText (not BBCode): skill text has brackets like [AUTO]

        // Write the skills that are not manually activated
        var manuallyActivatedSkills = card.Skills.OfType<VanguardActivationSkill>();
        var automaticSkill = card.Skills.Where(x => x is not VanguardActivationSkill).ToArray();
        if (card.Skills.Length == 0)
        {
            Skills.AddText("This card has no skills");
        }
        else
        {
            Skills.AddText(VanguardSkillToStringInterpreter.ExtractSkills(automaticSkill));

            foreach (var manualActivationSkill in manuallyActivatedSkills)
            {
                var skillBtn = new Button();
                skillBtn.Text = VanguardSkillToStringInterpreter.ExtractSkill(manualActivationSkill);
                skillBtn.Pressed += () => ActivationPressed?.Invoke(card, manualActivationSkill);
                skillBtn.Disabled = !skillActivationWindow || !IsSkillManuallyActivatable(card, manualActivationSkill);
                skillBtn.AddThemeFontSizeOverride("font_size", 32);
                skillBtn.ClipText = true;
                skillBtn.AutowrapMode = TextServer.AutowrapMode.WordSmart;
                skillBtn.Alignment = HorizontalAlignment.Left;
                ActivationSkills.AddChild(skillBtn);
            }
        }
    }

    public void ShowCard(VanguardCard card, bool skillActivationWindow = false, VanguardSkill[]? grantedSkills = null)
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

        Skills.Clear();
        ActivationSkills.ClearChildren();
        GrantedSkills.Clear();

        if(ReadOnlyMode)
        {
            populateReadOnlyMode(card);
        }
        else
        {
            populateActivatableMode(card, skillActivationWindow);
        }

        if (grantedSkills is { Length: > 0 })
        {
            GrantedSkills.PushColor(VanguardPresentationConfig.GrantedSkillColor);
            GrantedSkills.AddText("Granted:");
            GrantedSkills.Newline();
            GrantedSkills.AddText(VanguardSkillToStringInterpreter.ExtractSkills(grantedSkills));
            GrantedSkills.Pop();
        }
    }
}
