using System.Collections.Generic;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Vanguard;

public interface IInputProviderStrategy
{
    Task<List<CardBase>> SelectCardsFromHand();
    Task<CardBase?> SelectCardFromHandOrNot(VanguardCard currentVanguard);
}