using Godot;

/// <summary>
/// A HUD button an input strategy can offer (InputScope.Button): disabled unless the current scope allows it; pressing
/// it is a ButtonPressed gesture of its <see cref="Kind"/>. Put it on any Button in the HUD.
/// </summary>
public partial class BoardButtonView : Button
{
    [Export] public BoardButton Kind { get; set; }
}
