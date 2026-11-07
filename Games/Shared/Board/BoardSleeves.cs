using ArC.CardGames.Components;
using DuelCreatures.Data;

/// <summary>
/// The sleeves a board shows face-down cards in: the player's (their pick for the game, DataManager.BoardSleevesFor)
/// and the opponent's. <see cref="Defaults{TCard}"/> is the game's default for both (e.g. a bot-vs-bot debug scene).
/// </summary>
public sealed record BoardSleeves(SleeveInfo Player, SleeveInfo Opponent)
{
    public static BoardSleeves Defaults<TCard>() where TCard : CardBase
    {
        var sleeve = CardStyles.For<TCard>().DefaultSleeve!;
        return new(sleeve, sleeve);
    }

    /// <summary>The sleeve of the cards on <paramref name="side"/> (a zone both share, like the guardian circle, takes the player's).</summary>
    public SleeveInfo For(BoardSide side) => side == BoardSide.Opponent ? Opponent : Player;
}
