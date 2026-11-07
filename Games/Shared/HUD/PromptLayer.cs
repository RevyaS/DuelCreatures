using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Holds the HUD's prompt panels (ICollapsiblePrompt: card lists, the pinned skill details), full screen, over the
/// board. Swiping down on one of them slides the whole layer down, out of the way, to look at the board; a bar with
/// chevrons stays at the bottom. Swiping it up (or tapping it) brings the prompts back. Nothing is closed or answered by
/// collapsing: the prompts wait. Once nothing is showing, the layer is back up for the next prompt.
/// </summary>
public partial class PromptLayer : Control
{
    /// <summary>How tall the bar left at the bottom is when collapsed.</summary>
    [Export] public float HandleHeight { get; set; } = 90;
    /// <summary>Seconds to slide down or up.</summary>
    [Export] public float SlideTime { get; set; } = 0.2f;
    [Export] public Color HandleColor { get; set; } = new(0, 0, 0, 0.55f);

    readonly List<ICollapsiblePrompt> prompts = [];
    Control handle = null!;
    bool collapsed;
    Tween? sliding;

    public bool IsCollapsed => collapsed;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        handle = CreateHandle();
        foreach(var prompt in this.Descendants<ICollapsiblePrompt>()) Add(prompt);
    }

    /// <summary>Adds a prompt created in code (those in the scene are found when it's ready).</summary>
    public void Add(ICollapsiblePrompt prompt)
    {
        if(prompts.Contains(prompt)) return;
        prompts.Add(prompt);
        if(prompt.SwipeArea.HasNode("CollapseSwipes")) return;
        var swipes = new GestureDetector
        {
            Name = "CollapseSwipes",
            Swipes = true,
            HorizontalSwipes = false,
            VerticalSwipes = true,
            Drags = false,
            ScrollsParent = false,
        };
        swipes.Swiped += direction =>
        {
            if(direction == GestureDetector.SwipeDirection.Down) Collapse();
        };
        prompt.SwipeArea.AddChild(swipes);
    }

    public void Collapse()
    {
        if(collapsed || !HasContent) return;
        collapsed = true;
        handle.Visible = true;
        SlideTo(Size.Y - HandleHeight);
    }

    public void Expand()
    {
        if(!collapsed) return;
        collapsed = false;
        SlideTo(0, () => handle.Visible = false);
    }

    bool HasContent => prompts.Any(prompt => prompt.IsShowing);

    public override void _Process(double delta)
    {
        // Nothing to show any more: back up, ready for the next prompt
        if(collapsed && !HasContent)
        {
            sliding?.Kill();
            collapsed = false;
            handle.Visible = false;
            Position = Position with { Y = 0 };
        }
    }

    private void SlideTo(float y, System.Action? then = null)
    {
        sliding?.Kill();
        sliding = CreateTween();
        sliding.TweenProperty(this, "position:y", y, SlideTime).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        if(then is not null) sliding.TweenCallback(Callable.From(then));
    }

    // The bar at the layer's top edge: at the bottom of the screen once the layer slid down
    private Control CreateHandle()
    {
        var bar = new PanelContainer { Name = "CollapsedHandle", Visible = false, MouseFilter = MouseFilterEnum.Stop };
        bar.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = HandleColor });
        bar.AddChild(new ChevronIndicator { MouseFilter = MouseFilterEnum.Ignore });
        AddChild(bar);
        bar.SetAnchorsAndOffsetsPreset(LayoutPreset.TopWide);
        bar.OffsetBottom = HandleHeight;

        var swipes = new GestureDetector
        {
            Name = "ExpandSwipes",
            Swipes = true,
            HorizontalSwipes = false,
            VerticalSwipes = true,
            Drags = false,
            ScrollsParent = false,
        };
        swipes.Swiped += direction =>
        {
            if(direction == GestureDetector.SwipeDirection.Up) Expand();
        };
        swipes.Tapped += Expand;
        bar.AddChild(swipes);
        return bar;
    }
}
