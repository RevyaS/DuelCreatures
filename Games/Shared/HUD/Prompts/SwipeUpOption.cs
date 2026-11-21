using ArC.CardGames.Components;

/// <summary>
/// One choice on a SwipeUpPicker: <paramref name="Card"/> stands for <paramref name="Item"/> (a skill's card for the
/// skill, the skill's card for each of its options), with a <paramref name="Tag"/> telling same-card choices apart (an
/// option's label).
/// </summary>
public sealed record SwipeUpOption<T>(CardBase Card, T Item, CardTag? Tag = null);
