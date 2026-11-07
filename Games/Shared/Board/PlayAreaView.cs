using Godot;

/// <summary>
/// The root of a play area scene (put it on the scene's root, any Control). <see cref="FlippedAppearance"/> marks the
/// instance on the far side of the board (the one the board turns half around): every QuarterTurnContainer in it with
/// FacesOwner then turns back, so labels and piles in it read upright for the player across the table. The zones are
/// bound by name (ArcDriver); anything else in the scene that shows the play area (IBindable of it, e.g. a life points
/// label) is bound to it too, wherever it is in the scene.
/// </summary>
[Tool]
public partial class PlayAreaView : Control
{
    private bool _flippedAppearance;
    /// <summary>On the far side of the board: things that face their owner turn back to read upright from there.</summary>
    [Export]
    public bool FlippedAppearance
    {
        get => _flippedAppearance;
        set
        {
            _flippedAppearance = value;
            Render();
        }
    }

    public override void _Ready() => Render();

    private void Render()
    {
        if(!IsNodeReady()) return;
        foreach(var container in this.Descendants<QuarterTurnContainer>())
        {
            if(container.FacesOwner) container.OnFarSide = FlippedAppearance;
        }
        Flip(FlippedAppearance);
    }

    /// <summary>What else a game turns on the far side (e.g. Vanguard's circle icons).</summary>
    protected virtual void Flip(bool flipped) {}
}
