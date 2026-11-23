using System.Collections.Generic;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Setup;

/// <summary>Any other pick of cards (searches, soul blast, discards, several places at once): the question's own prompt.</summary>
public sealed class DefaultCardSelection(VanguardInput input) : VanguardCardSelection(input)
{
    public override bool Handles(Choosing<CardBase> question) => true;
    public override Task<IReadOnlyList<CardBase>> Choose(Choosing<CardBase> question) => Ask(question, question.Prompt);
}
