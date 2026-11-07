using System;
using System.Collections.Generic;
using ArC.CardGames.Components;
using ArC.CardGames.Rules;

/// <summary>
/// Something the player did on the board, in game terms: ArC cards and zones, never nodes. Input strategies wait for
/// the ones their InputScope allows.
/// </summary>
public interface IGesture;

/// <summary>A card shown in <see cref="Zone"/> was clicked (tapped).</summary>
public sealed record CardClicked(CardBase Card, ICardSpace Zone) : IGesture;

/// <summary><see cref="Zone"/> itself was clicked (or a card in it, when the scope asked for the zone).</summary>
public sealed record ZoneClicked(ICardSpace Zone) : IGesture;

/// <summary>A HUD button was pressed (a BoardButtonView of that kind).</summary>
public sealed record ButtonPressed(BoardButton Button) : IGesture;

/// <summary>A skill's activation button in the card details was pressed.</summary>
public sealed record SkillActivated(CardBase Card, ICardSpace? From, ISkill Skill) : IGesture;

/// <summary>A card command's button in the card details was pressed (e.g. "Summon" on an Xyz monster in the Extra Deck).</summary>
public sealed record CommandPressed(CardBase Card, ICardSpace? From, CardCommand Command) : IGesture;

/// <summary><see cref="Card"/> was dragged from <see cref="From"/> and dropped on <see cref="To"/>, on the region of <see cref="Choice"/>.</summary>
public sealed record CardDropped(CardBase Card, ICardSpace? From, ICardSpace To, DropChoice Choice) : IGesture;

/// <summary><see cref="Zone"/> was swiped (a quick flick) in <see cref="Direction"/>.</summary>
public sealed record ZoneSwiped(ICardSpace Zone, GestureDetector.SwipeDirection Direction) : IGesture;

/// <summary>A drag from <see cref="From"/> was let go on <see cref="Target"/> (e.g. an attack).</summary>
public sealed record Aimed(ICardSpace From, AimTarget Target) : IGesture;

/// <summary>What a drag from a zone was let go on (<see cref="Aimed"/>).</summary>
public abstract record AimTarget;
/// <summary>Another zone.</summary>
public sealed record ZoneTarget(ICardSpace Zone) : AimTarget;
/// <summary>A play area, outside its zones (e.g. a direct attack on the opponent).</summary>
public sealed record AreaTarget(PlayAreaBase Area) : AimTarget;

/// <summary>The steps of a drag from a zone (ArcDriver.Aiming).</summary>
public enum AimStep { Start, Move, End, Cancel }

/// <summary>The HUD buttons a strategy can offer. Each is a BoardButtonView in the scene with this kind.</summary>
public enum BoardButton
{
    /// <summary>Move on: end the main phase, end the battle phase.</summary>
    NextPhase,
    /// <summary>Skip straight to the end phase.</summary>
    EndPhase,
}

/// <summary>
/// One region a zone splits into while a card is dragged over it (e.g. "Summon" | "Set"): dropping the card on that
/// region is a CardDropped with this choice. A game declares its choices once and matches them by reference; the label
/// is what the region shows.
/// </summary>
public sealed class DropChoice(string label)
{
    public string Label => label;
    public override string ToString() => label;
}

/// <summary>What a zone takes when a card is dropped on it: the choices for each card (none: it doesn't take that card).</summary>
public sealed record DropOffer(Func<CardBase, IReadOnlyList<DropChoice>> ChoicesFor);

/// <summary>
/// Something a card's details can offer to do with the card, beside its skills (e.g. "Summon" an Xyz monster from the
/// Extra Deck): a button whose press is a CommandPressed. Declared once per game, matched by reference.
/// </summary>
public sealed class CardCommand(string label)
{
    public string Label => label;
    public override string ToString() => label;
}
