using ArC.CardGames.Components;

public interface ICardSpaceBindable<CardSpace> where CardSpace : ICardSpace
{
    void Bind(CardSpace cardSpace);
}