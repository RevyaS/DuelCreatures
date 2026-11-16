using ArC.CardGames.Rules;

/// <summary>A game's way of writing a skill as text, for the card details' skill parts.</summary>
public interface ISkillTextInterpreter
{
    string Describe(ISkill skill);
}
