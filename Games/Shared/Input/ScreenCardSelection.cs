using System.Collections.Generic;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Setup;

/// <summary>
/// A kind of "pick these cards" question asked on the card selection screen (CardSelectScreen), for any game: each card
/// is tagged with where it is (<see cref="TagOf"/>, the game's tags), and may show how it will face once answered
/// (<see cref="FacingOf"/>). A game's strategies derive from it and say which questions they take and with what hint.
/// </summary>
public abstract class ScreenCardSelection(CardSelectScreen screen) : ICardSelectionStrategy
{
    public abstract bool Handles(Choosing<CardBase> question);
    public abstract Task<IReadOnlyList<CardBase>> Choose(Choosing<CardBase> question);

    /// <summary>The location tag on <paramref name="card"/>: the place holding it (the game's tags resource).</summary>
    protected abstract CardTag TagOf(CardBase card);

    /// <summary>The indicator over <paramref name="card"/>, picked or not. None by default.</summary>
    protected virtual FacingMark FacingOf(CardBase card, bool picked) => FacingMark.None;

    /// <summary>Asks <paramref name="question"/> on the screen with <paramref name="hint"/> as its prompt.</summary>
    protected Task<IReadOnlyList<CardBase>> Ask(Choosing<CardBase> question, string hint)
        => screen.ChooseCards(new CardSelectRequest(question.Offered, question.Minimum, question.Maximum, hint, question.CanCancel, TagOf, FacingOf));
}
