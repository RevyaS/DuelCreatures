using System;
using System.Collections.Generic;
using System.Linq;
using ArC.CardGames.Components;
using DuelCreatures.Data;
using Godot;

/// <summary>
/// Each card game's look (its CardGameStyle resource), by the type of its cards: a Vanguard card is shown with
/// Vanguard's card scene, art folder and sleeves. The games are listed once, in CardGames.RegisterStyles. A card
/// whose type no game claims fails, naming the type.
/// </summary>
public static class CardStyles
{
    static readonly Dictionary<Type, CardGameStyle> byCardType = [];
    static bool registered;

    /// <summary>Makes <typeparamref name="TCard"/> and its subtypes shown with the style at <paramref name="path"/>.</summary>
    public static void Register<TCard>(string path) where TCard : CardBase
    {
        var style = ResourceLoader.Load<CardGameStyle>(path)
            ?? throw new InvalidOperationException($"No {nameof(CardGameStyle)} at {path} (for {typeof(TCard).Name})");
        if(string.IsNullOrWhiteSpace(style.Id)) throw new InvalidOperationException($"{path} has no {nameof(CardGameStyle.Id)}");
        if(byCardType.Values.FirstOrDefault(other => other.Id == style.Id && !ReferenceEquals(other, style)) is { } clash)
        {
            throw new InvalidOperationException($"{path} and {clash.ResourcePath} share the id \"{style.Id}\"");
        }
        if(style.DefaultSleeve is null) throw new InvalidOperationException($"{path} has no {nameof(CardGameStyle.DefaultSleeve)}");
        byCardType[typeof(TCard)] = style;
    }

    /// <summary>The style of <paramref name="card"/>'s game.</summary>
    public static CardGameStyle For(CardBase card) => For(card.GetType());

    /// <summary>The style of the game whose cards are <typeparamref name="TCard"/>.</summary>
    public static CardGameStyle For<TCard>() where TCard : CardBase => For(typeof(TCard));

    private static CardGameStyle For(Type cardType)
    {
        EnsureRegistered();
        for(var type = cardType; type is not null; type = type.BaseType)
        {
            if(byCardType.TryGetValue(type, out var style)) return style;
        }
        throw new InvalidOperationException($"No card game's style shows a {cardType.Name}: register its game in {nameof(CardGames)}.{nameof(CardGames.RegisterStyles)}");
    }

    private static void EnsureRegistered()
    {
        if(registered) return;
        registered = true;
        CardGames.RegisterStyles();
    }
}
