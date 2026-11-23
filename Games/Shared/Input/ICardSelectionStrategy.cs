using System.Collections.Generic;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Setup;

/// <summary>
/// One kind of "pick these cards" question (a counter blast, a counter charge, a heal, ...), told apart by the question's
/// typed purpose: <see cref="Handles"/> says whether it is this kind, <see cref="Choose"/> asks the player on screen.
/// A game keeps them in order, the most specific first, and asks the first that handles the question.
/// </summary>
public interface ICardSelectionStrategy
{
    bool Handles(Choosing<CardBase> question);
    Task<IReadOnlyList<CardBase>> Choose(Choosing<CardBase> question);
}
