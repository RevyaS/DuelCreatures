using System;
using System.Linq;
using ArC.CardGames.Components;
using Godot;

/// <summary>
/// Several cards side by side in a fixed number of slots (MaxCards), e.g. a damage zone or a banish row. Cards keep
/// their own facing, so one can be flipped face-down in place. More cards than slots is an error.
/// </summary>
[Tool]
public partial class LineZone : CardLineStatic, IZoneView, IBindable<GroupSpace>
{
    public ICardSpace? Space { get; private set; }

    /// <summary>Pressing a card asks to show the whole zone (<see cref="InspectRequested"/>).</summary>
    [Export] public bool Inspectable { get; set; }

    public event Action<ZoneSnapshot>? Presented;
    public event Action<IZoneView>? InspectRequested;
    public event Action<IZoneView, CardBase>? CardInspected;

    public override void _Ready()
    {
        base._Ready();
        if(Engine.IsEditorHint()) return;
        // Inspectable: a press shows the whole zone; otherwise the pressed card's details
        CardPressed += node =>
        {
            if(Inspectable && Space is not null) InspectRequested?.Invoke(this);
            else if(node.IsFront && node.CurrentCard is { } card) CardInspected?.Invoke(this, card);
        };
    }

    public void Bind(GroupSpace space)
    {
        Space = space;
        ClearCards();
        foreach(var card in ZoneSnapshot.Of(space).Cards)
        {
            Add(card);
        }
    }

    public ZoneSnapshot Displayed => new(GetNonEmptyCards()
        .Select(node => new CardSnapshot(node.CurrentCard, node.IsFront ? Facing.UP : Facing.DOWN))
        .ToList(), null);

    public void Apply(ZoneChange change, PresentationContext? presentation)
    {
        switch(change)
        {
            case ZoneCardsArrived arrived:
                foreach(var arriving in arrived.Cards)
                {
                    var card = ZonePresentation.Find(change.After, arriving);
                    presentation.PresentArrival(card.Card, arrived.Move, this, arrived.From, () => Add(card), card.IsFaceUp);
                }
                break;

            case ZoneCardsLeft left:
                presentation.Present(() =>
                {
                    foreach(var leaving in left.Cards)
                    {
                        presentation.Leaving(leaving, this);
                        RemoveCard(leaving);
                    }
                });
                break;

            case ZoneCardTurned turned:
                var after = ZonePresentation.Find(change.After, turned.Card);
                presentation.Present(async () =>
                {
                    var node = FindCardNode(turned.Card) ?? throw new InvalidOperationException($"{GetPath()} doesn't show {turned.Card.Name}, which was turned");
                    await ZonePresentation.Flip(presentation, node, after.IsFaceUp);
                });
                break;

            default:
                throw new NotSupportedException($"{Name} can't show a {change.GetType().Name}");
        }
        presentation.Present(() => Presented?.Invoke(change.After));
    }

    // The height it keeps when empty: a card at CardScale, plus this (the margins around the card row)
    private float _emptyHeightPadding = 4;
    [Export]
    public float EmptyHeightPadding
    {
        get => _emptyHeightPadding;
        set
        {
            _emptyHeightPadding = value;
            Render();
        }
    }

    protected override void RenderCore()
    {
        base.RenderCore();
        ZoneLayout.KeepCardHeight(this, CardScale, EmptyHeightPadding);
    }

    private void Add(CardSnapshot card)
    {
        var node = ZonePresentation.CreateNode(card);
        AddCard(node);
        // AddCard shows new cards face up; this zone shows the card's own facing
        node.IsFront = card.IsFaceUp;
    }
}
