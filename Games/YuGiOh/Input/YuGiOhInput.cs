using System.Collections.Generic;
using System.Linq;
using ArC.CardGames;
using ArC.CardGames.Flow;
using ArC.CardGames.Rules;
using ArC.CardGames.YuGiOh;
using Godot;

/// <summary>Everything YuGiOh's answers and strategies work with: the player, the board's input, and the HUD's prompts.</summary>
public sealed class YuGiOhInput(
    IYuGiOhPlayerInputProvider player,
    InputBoard board,
    Node dialogParent,
    SelectFromCardList cardPicker,
    CardList effectPicker,
    CardListPanel chainView,
    System.Action<CardDetail> showDetails,
    ISkillTextInterpreter skillText)
{
    /// <summary>Where effects are picked: one card per effect, pressed to read it, dragged out to pick it (Prompts.DragOutOfCardList).</summary>
    public CardList EffectPicker { get; } = effectPicker;
    /// <summary>Where the chain being responded to is shown, card by card, to read beside the responses.</summary>
    public CardListPanel ChainView { get; } = chainView;
    /// <summary>Shows a card's details (e.g. the effect a card stands for in the effect picker).</summary>
    public System.Action<CardDetail> ShowDetails { get; } = showDetails;

    /// <summary>The player being asked (for the rules that need to know who asks).</summary>
    public IYuGiOhPlayerInputProvider Player { get; } = player;
    public YuGiOhPlayArea PlayArea => Player.PlayArea;
    public YuGiOhPlayArea OpponentPlayArea => Player.OpponentPlayArea;
    /// <summary>The game's states: what a skill is selecting for (e.g. SearchDeckState's filter).</summary>
    public GameContext Game => Player.SkillService.Context;

    public InputBoard Board { get; } = board;
    public Node DialogParent { get; } = dialogParent;
    public SelectFromCardList CardPicker { get; } = cardPicker;
    public ISkillTextInterpreter SkillText { get; } = skillText;

    /// <summary>Answers known ahead, given before anyone is asked.</summary>
    public PreAnswers Answers { get; } = new();
    /// <summary>What the next prompt for a question may offer, once.</summary>
    public Narrowing Narrowing { get; } = new();

    /// <summary>The innermost game state of type <typeparamref name="TState"/>, if any.</summary>
    public TState? State<TState>() where TState : class, IGameState => Game.States.OfType<TState>().FirstOrDefault();

    public bool IsOwnZone(IYuGiOhZone zone)
        => PlayArea.ControlledMonsterZones.Cast<IYuGiOhZone>().Concat(PlayArea.SpellAndTrapZones).Append(PlayArea.FieldZone)
            .Contains(zone, ReferenceEqualityComparer.Instance);
}
