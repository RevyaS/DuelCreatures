using ArC.CardGames.Components;
using ArC.CardGames.Vanguard;

public interface ICardSpaceBindable<CardSpace> where CardSpace : ICardSpace
{
    void Bind(CardSpace cardSpace);
}

public interface IPlayAreaBindable
{
    void Bind(VanguardPlayArea playArea);
}