using Godot;

/// <summary>
/// The root of Vanguard's play area scene: a <see cref="PlayAreaView"/> whose unit circles also flip their cards and
/// icons on the far side of the board.
/// </summary>
[Tool]
public partial class VanguardPlayAreaView : PlayAreaView
{
    protected override void Flip(bool flipped)
    {
        foreach(var circle in this.Descendants<UnitCircleZone>()) circle.FlippedAppearance = flipped;
    }
}
