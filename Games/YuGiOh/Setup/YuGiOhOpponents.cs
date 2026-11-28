using System;
using System.Collections.Generic;
using ArC.CardGames.Bots;
using ArC.CardGames.Setup;
using ArC.CardGames.YuGiOh;
using ArC.CardGames.YuGiOh.Bots;
using ArC.Common.Utils;

/// <summary>The YuGiOh opponents the main screen offers; each is a bot playing its own deck.</summary>
public enum YuGiOhOpponentType
{
    ExosisterTacticalTryDeckBot,
}

/// <summary>
/// How player 2 is played and which deck it plays for each <see cref="YuGiOhOpponentType"/>, given the game's services
/// (YuGiOhSessionBuilder.Player2). The session builder creates the bot once, the first time the game asks.
/// </summary>
public static class YuGiOhOpponents
{
    static readonly YuGiOhOpponentType[] all = Enum.GetValues<YuGiOhOpponentType>();

    /// <summary>Any one of the opponents (the Random choice).</summary>
    public static YuGiOhOpponentType Random() => RNGUtils.SelectRandom(all);

    /// <summary>The deck <paramref name="opponent"/> plays: a fresh one each call.</summary>
    public static YuGiOhDeckBuilder DeckOf(YuGiOhOpponentType opponent) => opponent switch
    {
        YuGiOhOpponentType.ExosisterTacticalTryDeckBot => YuGiOhDeckFactory.ExosisterTacticalTryDeck,
        _ => throw new NotSupportedException($"{opponent} is not a supported opponent"),
    };

    public static IPlayerInput Create(YuGiOhOpponentType opponent, BotDifficulty difficulty, YuGiOhServices services) => opponent switch
    {
        YuGiOhOpponentType.ExosisterTacticalTryDeckBot => new ExosisterTacticalTryDeckBot(services.Game.Player2, services.DataQuery, difficulty),
        _ => throw new NotSupportedException($"{opponent} is not a supported opponent"),
    };
}

/// <summary>A deck the player can pick for a YuGiOh game: its name, and a fresh copy each game.</summary>
public sealed record YuGiOhDeckChoice(string Label, Func<YuGiOhDeckBuilder> Build)
{
    /// <summary>The decks the main screen lists.</summary>
    public static IReadOnlyList<YuGiOhDeckChoice> All { get; } =
    [
        new("Exosister Tactical Try Deck", () => YuGiOhDeckFactory.ExosisterTacticalTryDeck),
    ];
}
