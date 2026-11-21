using System;
using System.Collections.Generic;
using ArC.CardGames.Components;

/// <summary>
/// What a CardSelectScreen asks: pick between <paramref name="Minimum"/> and <paramref name="Maximum"/> of
/// <paramref name="Cards"/> (in this order on screen). <paramref name="TagOf"/> gives each card's location tag;
/// <paramref name="FacingOf"/>, when set, how a card will face, given whether it's picked (counter blast / charge).
/// <paramref name="CanCancel"/> shows Cancel, which answers nothing.
/// </summary>
public sealed record CardSelectRequest(
    IReadOnlyList<CardBase> Cards,
    int Minimum,
    int Maximum,
    string Hint,
    bool CanCancel,
    Func<CardBase, CardTag?> TagOf,
    Func<CardBase, bool, FacingMark>? FacingOf = null);
