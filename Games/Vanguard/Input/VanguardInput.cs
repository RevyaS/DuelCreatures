using System.Linq;
using ArC.CardGames;
using ArC.CardGames.Flow;
using ArC.CardGames.Vanguard;
using Godot;

/// <summary>Everything Vanguard's answers and strategies work with: the player, the board's input, and the HUD's prompts.</summary>
public sealed class VanguardInput(
    IVanguardPlayerInputProvider player,
    GameContext game,
    InputBoard board,
    PresentationContext presentation,
    Node dialogParent,
    SelectFromCardList cardPicker,
    SelectCardsComponent handPicker,
    CardList effectPicker,
    System.Action<CardDetail> showDetails)
{
    /// <summary>The player being asked (for the rules that need to know who asks).</summary>
    public IVanguardPlayerInputProvider Player { get; } = player;
    public VanguardPlayArea PlayArea => Player.PlayArea;
    public VanguardPlayArea OpponentPlayArea => Player.OpponentPlayArea;
    /// <summary>The game's states: what the game is asking for (e.g. GuardState, TriggerPowerState).</summary>
    public GameContext Game { get; } = game;

    public InputBoard Board { get; } = board;
    /// <summary>The board's presentation, e.g. to play a sound as the player acts.</summary>
    public PresentationContext Presentation { get; } = presentation;
    public Node DialogParent { get; } = dialogParent;
    /// <summary>Picking cards from a list, then Confirm (a heal, a soul blast, cards from the hand for a skill).</summary>
    public SelectFromCardList CardPicker { get; } = cardPicker;
    /// <summary>Picking cards by dragging them out of the hand into a panel (the mulligan).</summary>
    public SelectCardsComponent HandPicker { get; } = handPicker;
    /// <summary>Where skills and cards are picked by dragging one out (Prompts.DragOutOfCardList).</summary>
    public CardList EffectPicker { get; } = effectPicker;
    /// <summary>Shows a card's details (e.g. the skill a card stands for in the effect picker).</summary>
    public System.Action<CardDetail> ShowDetails { get; } = showDetails;

    /// <summary>Answers known ahead, given before anyone is asked.</summary>
    public PreAnswers Answers { get; } = new();
    /// <summary>What the next prompt for a question may offer, once.</summary>
    public Narrowing Narrowing { get; } = new();

    /// <summary>The innermost game state of type <typeparamref name="TState"/>, if any.</summary>
    public TState? State<TState>() where TState : class, IGameState => Game.States.OfType<TState>().FirstOrDefault();

    /// <summary>The view showing <paramref name="zone"/> (e.g. to preview guards on the guardian circle).</summary>
    public IZoneView ViewOf(ArC.CardGames.Components.ICardSpace zone) => Board.ViewOf(zone);

    /// <summary>The battle the player is guarding in, if it's one of their units being attacked.</summary>
    public GuardState? OwnGuard => State<GuardState>() is { } guard && PlayArea.OwnsUnitCircle(guard.TargetCircle) ? guard : null;
}
