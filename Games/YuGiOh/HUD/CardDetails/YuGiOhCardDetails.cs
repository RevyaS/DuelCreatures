using ArC.CardGames.Rules;
using ArC.CardGames.YuGiOh;
using Godot;

/// <summary>YuGiOh's card details: its parts read YuGiOh cards, its skills read as YuGiOh skills.</summary>
public partial class YuGiOhCardDetails : CardDetailsView
{
    protected override ISkillTextInterpreter SkillText => YuGiOhSkillTextInterpreter.Instance;
}
