using ArC.CardGames.Vanguard;
using Godot;

/// <summary>Everything Vanguard's strategies and prompts work with: the player, the board's input, and the HUD's prompts.</summary>
public sealed class VanguardInput(
    VanguardInputProvider player,
    InputBoard board,
    PresentationContext presentation,
    Node dialogParent,
    SelectFromCardList cardPicker,
    SelectCardsComponent handPicker,
    CardList effectPicker,
    FieldSelectScreen fieldSelect,
    CardSelectScreen cardSelect,
    SwipeUpPicker swipeUp,
    VanguardTriggerSelectScreen triggerSelect,
    VanguardGuardScreen guardScreen,
    System.Func<VanguardGameSession> session,
    ResolvingSkills resolvingSkills,
    VanguardCardTags cardTags,
    System.Action<CardDetail> showDetails)
{
    /// <summary>The player being asked.</summary>
    public VanguardInputProvider Player { get; } = player;
    public VanguardPlayArea PlayArea => Player.PlayArea;
    public VanguardPlayArea OpponentPlayArea => Player.OpponentPlayArea;

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
    /// <summary>Where circles are picked and cards placed on them, on the enlarged field.</summary>
    public FieldSelectScreen FieldSelect { get; } = fieldSelect;
    /// <summary>Where cards are picked from anywhere but the circles (soul, drop zone, deck, damage, hand).</summary>
    public CardSelectScreen CardSelect { get; } = cardSelect;
    /// <summary>Where one skill or option is picked by swiping its card up out of a row.</summary>
    public SwipeUpPicker SwipeUp { get; } = swipeUp;
    /// <summary>Where a trigger's unit is picked, with the battle on top.</summary>
    public VanguardTriggerSelectScreen TriggerSelect { get; } = triggerSelect;
    /// <summary>Where guardians and interceptors are picked.</summary>
    public VanguardGuardScreen GuardScreen { get; } = guardScreen;
    /// <summary>The game being played (e.g. the battle and the trigger check being resolved: its Actions.Current).</summary>
    public System.Func<VanguardGameSession> Session { get; } = session;
    /// <summary>The player's skills being used (e.g. the card a skill's options are shown on).</summary>
    public ResolvingSkills ResolvingSkills { get; } = resolvingSkills;
    /// <summary>The location tags on cards offered to pick.</summary>
    public VanguardCardTags CardTags { get; } = cardTags;
    /// <summary>Shows a card's details (e.g. the skill a card stands for in the effect picker).</summary>
    public System.Action<CardDetail> ShowDetails { get; } = showDetails;

    /// <summary>The view showing <paramref name="zone"/> (e.g. to preview guards on the guardian circle).</summary>
    public IZoneView ViewOf(ArC.CardGames.Components.ICardSpace zone) => Board.ViewOf(zone);
}
