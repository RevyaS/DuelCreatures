using System;
using System.Collections.Generic;
using System.Text;
using ArC.CardGames.Predefined.Vanguard;
using ArC.CardGames.Predefined.Vanguard.Skill;
using Godot;

public partial class CardInfo : Control
{
    Label CardName = null!;
    RichTextLabel Skills = null!;
    LabelValueContainer Power = null!, Critical = null!, Guard = null!;
    public override void _Ready()
    {
        CardName = GetNode<Label>($"%{nameof(CardName)}");
        Power = GetNode<LabelValueContainer>($"%{nameof(Power)}");
        Critical = GetNode<LabelValueContainer>($"%{nameof(Critical)}");
        Guard = GetNode<LabelValueContainer>($"%{nameof(Guard)}");
        Skills = GetNode<RichTextLabel>($"%{nameof(Skills)}");
    }

    public override void _GuiInput(InputEvent e)
    {
        // Mouse click or touch
        if (e is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            Hide();
        }

        if (e is InputEventScreenTouch st && st.Pressed)
        {
            Hide();
        }
    }

    public void Show(VanguardCard card)
    {
        CardName.Text = card.Name;
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

        Show();
    }
    
}