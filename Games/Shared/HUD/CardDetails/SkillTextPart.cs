using System.Linq;
using ArC.CardGames.Rules;
using Godot;

/// <summary>
/// The card's own skills as text (the game's ISkillTextInterpreter, from the details root). Skills activated with a
/// button where the card is now (ISkillButtons, from the details root) are left to an ActivationListPart while an
/// activation gate is provided (in a game); read-only (no gate, e.g. the deck builder) or with
/// <see cref="IncludeActivatedSkills"/>, they're text here too.
/// </summary>
[Tool]
public partial class SkillTextPart : RichTextLabel, ICardDetailPart
{
    [Export] public string NoSkillsText { get; set; } = "This card has no skills";
    [Export] public bool IncludeActivatedSkills { get; set; }

    public void Show(CardDetail detail)
    {
        var interpreter = this.DependOn<ISkillTextInterpreter>(SkillKindInterpreter.Instance);
        if(detail.Focus is { } focus)
        {
            // One effect, whatever its kind (e.g. the effect a card stands for in a list to pick from)
            Clear();
            AddText(interpreter.Describe(focus));
            Visible = true;
            return;
        }
        // Effects with a button (the ActivationListPart's) aren't repeated here; read-only, there are no buttons
        var readOnly = ActivationGates.ProvidedTo(this) is null;
        var buttons = this.DependOn<ISkillButtons>(SkillButtons.ManuallyActivated);
        var skills = detail.AllSkills.Except(detail.GrantedSkills)
            .Where(skill => IncludeActivatedSkills || readOnly || !buttons.HasButton(skill, detail))
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
