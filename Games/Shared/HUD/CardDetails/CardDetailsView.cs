using System.Collections.Generic;
using ArC.CardGames.Rules;
using Godot;

/// <summary>
/// The root of a card details scene. Each game has its own scene, made of parts (ICardDetailPart) placed and styled
/// freely; <see cref="Show"/> passes the card to every part under it. The game's subclass provides how its skills read
/// (<see cref="SkillText"/>) to the skill parts.
/// </summary>
public partial class CardDetailsView : Control, IProvide<ISkillTextInterpreter>, IProvide<ISkillButtons>, ISkillButtons
{
    /// <summary>How this game's skills read; kinds of skills ("Ignition Skill") unless the game says.</summary>
    protected virtual ISkillTextInterpreter SkillText => SkillKindInterpreter.Instance;

    /// <summary>
    /// Whether <paramref name="skill"/> is activated with a button in these details, for the card where it is now
    /// (<paramref name="detail"/>); otherwise it reads as text. Every effect the player activates, unless the game says.
    /// </summary>
    public virtual bool HasButton(ISkill skill, CardDetail detail) => SkillButtons.ManuallyActivated.HasButton(skill, detail);

    ISkillButtons IProvide<ISkillButtons>.Value() => this;

    ISkillTextInterpreter IProvide<ISkillTextInterpreter>.Value() => SkillText;

    /// <summary>How this game's skills read, for other UI (e.g. prompts listing skills).</summary>
    public ISkillTextInterpreter SkillTextInterpreter() => SkillText;

    public CardDetail? Current { get; private set; }

    public void Show(CardDetail detail)
    {
        Current = detail;
        foreach(var part in Parts(this)) part.Show(detail);
        Show();
    }

    private static IEnumerable<ICardDetailPart> Parts(Node node)
    {
        foreach(var child in node.GetChildren())
        {
            if(child is ICardDetailPart part) yield return part;
            foreach(var nested in Parts(child)) yield return nested;
        }
    }
}
