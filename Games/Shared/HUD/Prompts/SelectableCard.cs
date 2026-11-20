using System;
using ArC.CardGames.Components;
using Godot;

/// <summary>
/// One card offered on an input screen, enlarged: the game's card scene (CardScenes.Create) fitted into %CardSlot (its
/// size set in the scene), with its pick number (%Badge, a SelectionBadge), where it is or what it's for (%Tag, a
/// CardTagView), a state drawn over it (%Marker, a CardMarker's scene) and the button picking it (%SelectButton, any
/// BaseButton in toggle mode). A tap on the card is <see cref="Pressed"/> (its details); the button is
/// <see cref="SelectPressed"/>. The card shows what it's told: whoever owns the selection sets <see cref="PickNumber"/>
/// and <see cref="CanSelect"/>, the button never picks by itself. Laid out in SelectableCard.tscn.
/// </summary>
public partial class SelectableCard : Control
{
    Control? cardSlot, markerSlot;
    BaseButton? selectButton;
    SelectionBadge? badge;
    CardTagView? tagView;
    Control CardSlot => cardSlot ??= GetNode<Control>("%CardSlot");
    Control MarkerSlot => markerSlot ??= GetNode<Control>("%Marker");
    BaseButton SelectButton => selectButton ??= GetNode<BaseButton>("%SelectButton");
    SelectionBadge Badge => badge ??= GetNode<SelectionBadge>("%Badge");
    CardTagView TagView => tagView ??= GetNode<CardTagView>("%Tag");

    Card? cardNode;
    CardBase? card;
    int? pickNumber;
    CardMarker? marker;

    /// <summary>The select button (picks or drops the card).</summary>
    public event Action<SelectableCard>? SelectPressed;
    /// <summary>A tap on the card: show its details.</summary>
    public event Action<SelectableCard>? Pressed;

    public override void _Ready()
    {
        GetNode<GestureDetector>("%Gestures").Tapped += () => Pressed?.Invoke(this);
        SelectButton.ToggleMode = true;
        SelectButton.Pressed += OnSelectButtonPressed;
        CardSlot.Resized += FitCard;
        Badge.Number = pickNumber;
    }

    /// <summary>The card shown.</summary>
    public CardBase Card => card ?? throw new InvalidOperationException($"{GetPath()} shows no card yet (Show it one)");

    /// <summary>Shows <paramref name="shown"/>, face up (the player is offered it, so they may read it), in its game's card scene.</summary>
    public void Show(CardBase shown)
    {
        cardNode?.QueueFree();
        card = shown;
        cardNode = CardScenes.Create(shown);
        cardNode.IsFront = true;
        CardSlot.AddChild(cardNode);
        // This view takes every press; the card only shows
        cardNode.IgnoreMouse();
        FitCard();
    }

    /// <summary>Picked as number N (the badge shows it, the button is down), or not picked (null: no badge, button up).</summary>
    public int? PickNumber
    {
        get => pickNumber;
        set
        {
            pickNumber = value;
            Badge.Number = value;
            SelectButton.SetPressedNoSignal(value is not null);
        }
    }

    /// <summary>The select button can be pressed (false: shown disabled, e.g. the maximum is picked).</summary>
    public bool CanSelect
    {
        get => !SelectButton.Disabled;
        set => SelectButton.Disabled = !value;
    }

    /// <summary>Shows the select button at all (false where the card is only to read).</summary>
    public bool Selectable
    {
        get => SelectButton.Visible;
        set => SelectButton.Visible = value;
    }

    /// <summary>Where the card is or what it's for (icon and/or text), or none.</summary>
    public CardTag? Tag
    {
        get => TagView.Tag;
        set => TagView.Tag = value;
    }

    /// <summary>A state drawn over the card (its scene instanced into %Marker), or none.</summary>
    public CardMarker? Marker
    {
        get => marker;
        set
        {
            if(ReferenceEquals(marker, value)) return;
            marker = value;
            foreach(var shown in MarkerSlot.GetChildren()) shown.QueueFree();
            if(value is null) return;
            var scene = value.Scene ?? throw new InvalidOperationException($"The card marker {value.ResourcePath} has no {nameof(CardMarker.Scene)}");
            var node = scene.Instantiate();
            MarkerSlot.AddChild(node);
            if(node is Control control) control.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            node.IgnoreMouse();
        }
    }

    // The button only reports the press: the selection decides, and PickNumber puts the button back as it says
    private void OnSelectButtonPressed()
    {
        SelectButton.SetPressedNoSignal(pickNumber is not null);
        // The release of a drag scrolling the list isn't a press
        if(ScrollingList() is { LastGestureWasDrag: true }) return;
        SelectPressed?.Invoke(this);
    }

    private DragScrollContainer? ScrollingList()
    {
        for(var node = GetParent(); node is not null; node = node.GetParent())
        {
            if(node is DragScrollContainer list) return list;
        }
        return null;
    }

    // The card scene is centered on the slot and scales around its center: as large as fits the slot
    private void FitCard()
    {
        if(cardNode is null) return;
        var cardSize = cardNode.CustomMinimumSize;
        if(cardSize.X <= 0 || cardSize.Y <= 0) cardSize = cardNode.Size;
        if(cardSize.X <= 0 || cardSize.Y <= 0 || CardSlot.Size.X <= 0 || CardSlot.Size.Y <= 0) return;
        var scale = Mathf.Min(CardSlot.Size.X / cardSize.X, CardSlot.Size.Y / cardSize.Y);
        cardNode.Scale = new Vector2(scale, scale);
    }
}
