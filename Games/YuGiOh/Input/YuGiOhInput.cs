using System.Collections.Generic;
using System.Linq;
using ArC.CardGames.Components;
using ArC.CardGames.Skills;
using ArC.CardGames.YuGiOh;
using Godot;

/// <summary>Everything YuGiOh's strategies and prompts work with: the player, the board's input, the HUD's prompts, and the game going on.</summary>
public sealed class YuGiOhInput(
    YuGiOhInputProvider player,
    InputBoard board,
    Node dialogParent,
    CardList cardList,
    InputScreens screens,
    YuGiOhSwipeUpPicker swipeUp,
    YuGiOhCardTags cardTags,
    System.Action<CardDetail> showDetails)
{
    /// <summary>The player being asked.</summary>
    public YuGiOhInputProvider Player { get; } = player;
    public YuGiOhPlayArea PlayArea => Player.PlayArea;
    public YuGiOhPlayArea OpponentPlayArea => Player.OpponentPlayArea;

    public InputBoard Board { get; } = board;
    public Node DialogParent { get; } = dialogParent;
    /// <summary>The list of a listing zone's cards (e.g. the Extra Deck, where an Xyz Summon starts).</summary>
    public CardList CardList { get; } = cardList;
    /// <summary>The input screens: zones and field cards on the enlarged field, other cards on the card selection screen.</summary>
    public InputScreens Screens { get; } = screens;
    /// <summary>Where one effect or option is picked by swiping its card up, the chain shown above while responding.</summary>
    public YuGiOhSwipeUpPicker SwipeUp { get; } = swipeUp;
    /// <summary>The location tags on cards offered to pick.</summary>
    public YuGiOhCardTags CardTags { get; } = cardTags;
    /// <summary>Shows a card's details (e.g. the effect a card stands for on the swipe-up picker).</summary>
    public System.Action<CardDetail> ShowDetails { get; } = showDetails;

    /// <summary>The links of the chain building now, first to last (kept by the input provider from the chain events).</summary>
    public List<ChainLink> Chain { get; } = [];
    /// <summary>The skills being used now, any player's, oldest first (kept by the input provider from SkillUseStarted / SkillUseEnded).</summary>
    public List<SkillRun> Runs { get; } = [];

    public IZoneView ViewOf(ICardSpace zone) => Board.ViewOf(zone);

    /// <summary>
    /// The card of the skill a step belongs to (e.g. the card whose "Apply 1 of the following" is asking), among the
    /// skills being used, the latest first; else the latest skill's card.
    /// </summary>
    public CardBase? CardAsking(Step step)
    {
        var runs = Enumerable.Reverse(Runs).ToList();
        var owner = runs.FirstOrDefault(run => Contains(run.Skill.Steps, step));
        return (owner ?? runs.FirstOrDefault())?.Self;
    }

    private static bool Contains(IEnumerable<Step> steps, Step step) => steps.Any(candidate => ReferenceEquals(candidate, step)
        || candidate is OptionChoice choice && choice.Options.Any(option => Contains(option.Then, step)));
}
