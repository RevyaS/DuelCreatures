using ArC.CardGames.Rules;
using ArC.CardGames.Vanguard;
using Godot;

/// <summary>Vanguard's card details: its parts read Vanguard cards, its skills read as Vanguard skills.</summary>
public partial class VanguardCardDetails : CardDetailsView
{
    protected override ISkillTextInterpreter SkillText => VanguardSkillTextInterpreter.Instance;
}
