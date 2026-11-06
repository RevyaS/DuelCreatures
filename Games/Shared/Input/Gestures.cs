using ArC.CardGames.Components;
using ArC.CardGames.Rules;

/// <summary>
/// Something the player did on the board, in game terms: ArC cards and zones, never nodes. Input strategies wait for
/// the ones their InputScope allows.
/// </summary>
public interface IGesture;

/// <summary>A card shown in <see cref="Zone"/> was clicked.</summary>
public sealed record CardClicked(CardBase Card, ICardSpace Zone) : IGesture;

/// <summary><see cref="Zone"/> itself was clicked (or a card in it, when the scope asked for the zone).</summary>
public sealed record ZoneClicked(ICardSpace Zone) : IGesture;

/// <summary>A HUD button was pressed (a BoardButtonView of that kind).</summary>
public sealed record ButtonPressed(BoardButton Button) : IGesture;

/// <summary>A skill's activation button in the card details was pressed.</summary>
public sealed record SkillActivated(CardBase Card, ICardSpace? From, ISkill Skill) : IGesture;

/// <summary>The HUD buttons a strategy can offer. Each is a BoardButtonView in the scene with this kind.</summary>
public enum BoardButton
{
    /// <summary>Move on: end the main phase, end the battle phase.</summary>
    NextPhase,
    /// <summary>Skip straight to the end phase.</summary>
    EndPhase,
    /// <summary>Every action possible now, as a list.</summary>
    Actions,
    /// <summary>Done choosing (e.g. several zones).</summary>
    Confirm,
}
