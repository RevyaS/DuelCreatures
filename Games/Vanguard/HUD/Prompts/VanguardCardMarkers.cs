using Godot;

/// <summary>Vanguard's card markers (VanguardCardMarkers.tres): the states drawn over cards offered to pick.</summary>
[GlobalClass]
public partial class VanguardCardMarkers : CardIndicators
{
    /// <summary>A face-up damage card picked for a counter blast.</summary>
    [Export] public CardMarker WillTurnFaceDown { get; set; } = null!;
    /// <summary>A face-down damage card picked for a counter charge.</summary>
    [Export] public CardMarker WillTurnFaceUp { get; set; } = null!;
}
