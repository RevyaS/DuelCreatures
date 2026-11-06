using System;
using System.Linq;
using ArC.CardGames.Components;
using Godot;

/// <summary>
/// A labelled value of the card (a LabelValueContainer: its label, font size and layout are set in the scene). A game
/// derives one per property, reading it in <see cref="Format"/>; it hides for cards that don't have the property.
/// </summary>
[Tool]
public partial class CardValuePart : LabelValueContainer, ICardDetailPart
{
    public void Show(CardDetail detail)
    {
        var value = Format(detail.Card);
        Visible = value is not null;
        if(value is not null) Value = value;
    }

    /// <summary>The value to show, or null when the card doesn't have it (the part hides).</summary>
    protected virtual string? Format(CardBase card) => null;

    /// <summary><paramref name="format"/> of the card when it's a <typeparamref name="TCard"/>, else null: <c>For&lt;YuGiOhMonsterCard&gt;(card, monster => monster.Attack.ToString())</c>.</summary>
    protected static string? For<TCard>(CardBase card, Func<TCard, string?> format) where TCard : CardBase
        => card is TCard typed ? format(typed) : null;

    /// <summary>An enum value in words: QUICK_PLAY -> "Quick Play".</summary>
    protected static string Words(Enum value) => string.Join(' ', value.ToString().Split('_', StringSplitOptions.RemoveEmptyEntries)
        .Select(word => word.Length == 0 ? word : char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant()));
}
