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
    SelectFromCardList cardPicker,
    CardList effectPicker,
    CardListPanel chainView,
    System.Action<CardDetail> showDetails)
{
    /// <summary>The player being asked.</summary>
    public YuGiOhInputProvider Player { get; } = player;
    public YuGiOhPlayArea PlayArea => Player.PlayArea;
    public YuGiOhPlayArea OpponentPlayArea => Player.OpponentPlayArea;

    public InputBoard Board { get; } = board;
    public Node DialogParent { get; } = dialogParent;
    /// <summary>Picking cards from a list, then Confirm.</summary>
    public SelectFromCardList CardPicker { get; } = cardPicker;
    /// <summary>Where effects and cards are picked: one card per choice, pressed to read it, dragged out to pick it (Prompts.DragOutOfCardList).</summary>
    public CardList EffectPicker { get; } = effectPicker;
    /// <summary>Where the chain being responded to is shown, card by card, to read beside the responses.</summary>
    public CardListPanel ChainView { get; } = chainView;
    /// <summary>Shows a card's details (e.g. the effect a card stands for in the effect picker).</summary>
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
