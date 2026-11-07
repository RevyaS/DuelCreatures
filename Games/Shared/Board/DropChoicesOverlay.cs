using System;
using System.Collections.Generic;
using System.Linq;
using ArC.CardGames.Components;
using Godot;

/// <summary>
/// Lets a zone take dropped cards, split into regions while a card it takes hovers over it (e.g. "Summon" | "Set"):
/// the region under the pointer is highlighted, and dropping there picks its choice. Any zone can have one: place a
/// DropChoicesOverlay in the zone's scene to style it (colors, font), or the zone adds a default one when it first
/// offers a drop. It covers the zone but never takes the mouse.
/// </summary>
public partial class DropChoicesOverlay : Control, ICardDropTarget
{
    [Export] public Color RegionColor { get; set; } = new(0f, 0f, 0f, 0.55f);
    [Export] public Color HoveredRegionColor { get; set; } = new(1f, 0.85f, 0.2f, 0.75f);
    [Export] public Color FontColor { get; set; } = Colors.White;
    [Export] public int FontSize { get; set; } = 26;
    /// <summary>Space between regions.</summary>
    [Export] public int Separation { get; set; } = 4;

    DropOffer? offer;
    Control zone = null!;
    CardBase? candidate;
    IReadOnlyList<DropChoice> shownChoices = [];
    readonly List<(PanelContainer Region, StyleBoxFlat Style, DropChoice Choice)> regions = [];
    HBoxContainer row = null!;

    /// <summary>A card was dropped on the zone, on <see cref="DropChoice"/>'s region.</summary>
    public event Action<CardBase, DropChoice>? Dropped;

    /// <summary>The overlay of <paramref name="zone"/>: the one placed in its scene, or a new default one.</summary>
    public static DropChoicesOverlay For(Control zone)
    {
        var overlay = zone.GetChildren().OfType<DropChoicesOverlay>().FirstOrDefault();
        if(overlay is null)
        {
            overlay = new DropChoicesOverlay { Name = nameof(DropChoicesOverlay) };
            zone.AddChild(overlay);
        }
        overlay.zone = zone;
        return overlay;
    }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        Visible = false;
        row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("separation", Separation);
        AddChild(row);
        row.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        SetProcess(false);
    }

    /// <summary>Takes the cards <paramref name="next"/> offers choices for; null takes none.</summary>
    public void Offer(DropOffer? next)
    {
        offer = next;
        if(offer is null)
        {
            CardDrag.Unregister(this);
            SetCardHovering(false);
        }
        else
        {
            CardDrag.Register(this);
        }
    }

    public override void _ExitTree() => CardDrag.Unregister(this);

    // The whole zone takes the drop (this overlay is hidden until a card hovers)
    public Control DropRegion => zone;

    public bool AcceptsCard(Card card)
    {
        if(offer is null || card.CurrentCard is not { } dragged) return false;
        if(offer.ChoicesFor(dragged).Count == 0) return false;
        candidate = dragged;
        return true;
    }

    public void SetCardHovering(bool hovering)
    {
        if(hovering && offer is not null && candidate is not null)
        {
            ShowRegions(offer.ChoicesFor(candidate));
            Visible = true;
            SetProcess(true);
        }
        else
        {
            Visible = false;
            SetProcess(false);
        }
    }

    public void DropCard(Card card)
    {
        var choice = ChoiceUnderPointer() ?? shownChoices.FirstOrDefault();
        SetCardHovering(false);
        if(choice is not null && card.CurrentCard is { } dropped) Dropped?.Invoke(dropped, choice);
    }

    public override void _Process(double delta)
    {
        var hovered = ChoiceUnderPointer();
        foreach(var (_, style, choice) in regions)
        {
            style.BgColor = ReferenceEquals(choice, hovered) ? HoveredRegionColor : RegionColor;
        }
    }

    private DropChoice? ChoiceUnderPointer()
    {
        var pointer = GetGlobalMousePosition();
        return regions.FirstOrDefault(region => UnderPointer.Contains(region.Region, pointer)).Choice;
    }

    private void ShowRegions(IReadOnlyList<DropChoice> choices)
    {
        if(ReferenceEquals(choices, shownChoices) || choices.SequenceEqual(shownChoices)) return;
        shownChoices = choices;
        row.FreeChildren();
        regions.Clear();
        foreach(var choice in choices)
        {
            var style = new StyleBoxFlat { BgColor = RegionColor };
            var region = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            region.AddThemeStyleboxOverride("panel", style);
            var label = new Label
            {
                Text = choice.Label,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            label.AddThemeFontSizeOverride("font_size", FontSize);
            label.AddThemeColorOverride("font_color", FontColor);
            region.AddChild(label);
            row.AddChild(region);
            regions.Add((region, style, choice));
        }
    }
}
