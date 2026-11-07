using Godot;

/// <summary>
/// A HUD button an input strategy can offer (InputScope.Button): disabled unless the current scope allows it; pressing
/// it is a ButtonPressed gesture of its <see cref="Kind"/>. Put it on any Button in the HUD.
/// </summary>
public partial class BoardButtonView : Button
{
    [Export] public BoardButton Kind { get; set; }

    /// <summary>Hidden while no scope offers it (e.g. buttons sharing one place on the board), instead of disabled.</summary>
    [Export] public bool HideWhenUnavailable { get; set; }

    /// <summary>Whether the current scope offers it: enabled, and shown when <see cref="HideWhenUnavailable"/>.</summary>
    public bool Available
    {
        get => !Disabled;
        set
        {
            Disabled = !value;
            if(HideWhenUnavailable) Visible = value;
        }
    }
}
