using System.Linq;
using ArC.CardGames.Rules;
using Godot;

/// <summary>
/// The card's own skills as text (the game's ISkillTextInterpreter, from the details root). Skills the player activates
/// are left to an ActivationListPart, unless <see cref="IncludeActivatedSkills"/>.
/// </summary>
[Tool]
public partial class SkillTextPart : RichTextLabel, ICardDetailPart
{
    [Export] public string NoSkillsText { get; set; } = "This card has no skills";
    [Export] public bool IncludeActivatedSkills { get; set; }

    public void Show(CardDetail detail)
    {
        var interpreter = this.DependOn<ISkillTextInterpreter>(SkillKindInterpreter.Instance);
        var skills = detail.AllSkills.Except(detail.GrantedSkills)
            .Where(skill => IncludeActivatedSkills || skill is not IManuallyActivatedSkill)
            .ToList();
        var hasOwnSkills = detail.AllSkills.Except(detail.GrantedSkills).Any();

        Clear();
        // AddText (not BBCode): skill text has brackets like [AUTO]
        AddText(skills.Count > 0
            ? string.Join("\n", skills.Select(interpreter.Describe))
            : hasOwnSkills ? string.Empty : NoSkillsText);
        Visible = skills.Count > 0 || !hasOwnSkills;
    }
}
