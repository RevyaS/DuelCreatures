using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Debug only: a board's layout must never change size. Watches every control under the given roots (cards and what
/// is inside card slots left out, since those move by design) and warns, with the node's path and both sizes, the
/// first time each one changes size after the first layout.
/// </summary>
public partial class LayoutWatch : Node
{
    readonly Dictionary<Control, Vector2> sizes = [];
    readonly HashSet<Control> reported = [];
    Control[] roots = [];

    public static LayoutWatch Watch(Node parent, params Control[] roots)
    {
        var watch = new LayoutWatch { roots = roots };
        parent.AddChild(watch);
        return watch;
    }

    public override async void _Ready()
    {
        // Let the board lay itself out first
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        foreach(var control in Watched()) sizes[control] = control.Size;
        GD.Print($"{nameof(LayoutWatch)}: watching {sizes.Count} controls");
    }

    public override void _Process(double delta)
    {
        foreach(var control in Watched())
        {
            if(!sizes.TryGetValue(control, out var before)) continue;
            if(control.Size == before || !reported.Add(control)) continue;
            GD.PushWarning($"{nameof(LayoutWatch)}: {control.GetPath()} ({control.GetType().Name}) changed size {before} -> {control.Size}");
        }
    }

    private IEnumerable<Control> Watched() => roots.SelectMany(root => Controls(root).Prepend(root));

    private static IEnumerable<Control> Controls(Node node)
    {
        foreach(var child in node.GetChildren())
        {
            // Cards, and whatever sits in a card slot, move and scale by design
            if(child is CardBaseComponent || node is CardContainer) continue;
            if(child is Control control) yield return control;
            foreach(var descendant in Controls(child)) yield return descendant;
        }
    }
}
