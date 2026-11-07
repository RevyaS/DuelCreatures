using Godot;

/// <summary>
/// A prompt panel in a <see cref="PromptLayer"/> (a card list, the pinned skill details): swiping down on its
/// <see cref="SwipeArea"/> collapses the layer to show the board. While one is visible, the layer has something to show.
/// </summary>
public interface ICollapsiblePrompt
{
    /// <summary>Where a swipe down collapses the layer (e.g. the panel around a list, not the whole screen behind it).</summary>
    Control SwipeArea { get; }

    /// <summary>Whether it's showing something now.</summary>
    bool IsShowing { get; }
}
