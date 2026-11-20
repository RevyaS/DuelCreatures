using Godot;

/// <summary>
/// A state drawn over a card offered to pick (e.g. counter blast's "will turn face down"): its scene is instanced over
/// the card (SelectableCard's %Marker) while the card is in that state.
/// </summary>
[GlobalClass]
public partial class CardMarker : Resource
{
    [Export] public PackedScene Scene { get; set; } = null!;
}
