using System.Collections.Generic;
using DuelCreatures.Data;
using Godot;

/// <summary>
/// The look of one Vanguard play area scene, set on its root: the sleeve its face-down cards show, and whether it's
/// the far side of the board (turned around, so its circles' icons, deck and drop zone are flipped back to read
/// upright). The zones themselves are bound by name (ArcDriver); this only draws.
/// </summary>
[Tool]
public partial class VanguardPlayAreaView : Control
{
    private bool _flippedAppearance;
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

    private SleeveInfo? _sleeveInfo;
    [Export]
    public SleeveInfo? SleeveInfo
    {
        get => _sleeveInfo;
        set
        {
            _sleeveInfo = value;
            Render();
        }
    }

    /// <summary>Containers turned around with the play area, so what they hold reads upright from its own side.</summary>
    [Export] public Godot.Collections.Array<QuarterTurnContainer> TurnedWithArea { get; set; } = [];

    public override void _Ready() => Render();

    private void Render()
    {
        if(!IsNodeReady()) return;
        foreach(var node in Descendants(this))
        {
            switch(node)
            {
                case UnitCircleZone circle:
                    circle.FlippedAppearance = FlippedAppearance;
                    break;
                case CardLine line when SleeveInfo is not null:
                    line.SleeveInfo = SleeveInfo;
                    break;
                case CardVerticalStack stack when SleeveInfo is not null:
                    stack.SleeveInfo = SleeveInfo;
                    break;
            }
        }
        var turns = FlippedAppearance ? QuarterTurnContainer.QuarterTurn.Deg180 : QuarterTurnContainer.QuarterTurn.Deg0;
        foreach(var container in TurnedWithArea)
        {
            if(container is not null) container.Turns = turns;
        }
    }

    private static IEnumerable<Node> Descendants(Node node)
    {
        foreach(var child in node.GetChildren())
        {
            yield return child;
            foreach(var descendant in Descendants(child)) yield return descendant;
        }
    }
}
