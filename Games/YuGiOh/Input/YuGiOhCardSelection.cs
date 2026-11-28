using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Setup;

/// <summary>
/// YuGiOh's pick of cards from anywhere but the field (hand, Deck, Extra Deck, GY, banished, materials, or several at
/// once), on the card selection screen with the question's own prompt, each card tagged with where it is.
/// </summary>
public sealed class YuGiOhCardSelection(YuGiOhInput input) : ScreenCardSelection(input.Screens.CardSelect)
{
    public override bool Handles(Choosing<CardBase> question) => true;
    public override Task<IReadOnlyList<CardBase>> Choose(Choosing<CardBase> question) => Ask(question, question.Prompt);

    protected override CardTag TagOf(CardBase card)
        => input.CardTags.For(card, input.PlayArea) ?? input.CardTags.For(card, input.OpponentPlayArea)
            ?? throw new InvalidOperationException($"{card.Name} was offered to pick, but no place of either play area holds it");
}
