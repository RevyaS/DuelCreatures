using ArC.CardGames.Rules;
using ArC.CardGames.YuGiOh;
using Godot;

/// <summary>YuGiOh's card details: its parts read YuGiOh cards, its skills read as YuGiOh skills.</summary>
public partial class YuGiOhCardDetails : CardDetailsView
{
    protected override ISkillTextInterpreter SkillText => YuGiOhSkillTextInterpreter.Instance;

    /// <summary>
    /// A spell's or trap's card activation has a button only while the card is Set (activated from its zone); from the
    /// hand it's activated by dropping the card on a zone, so it reads as text. Other effects the player activates
    /// (ignition, quick) have buttons.
    /// </summary>
    public override bool HasButton(ISkill skill, CardDetail detail) => skill switch
    {
        YuGiOhCardActivationSkill => detail.From is SpellAndTrapZone && !detail.Card.IsFaceUp,
        _ => base.HasButton(skill, detail),
    };
}
