using System;
using System.Linq;
using ArC.CardGames.Components;
using Godot;

/// <summary>
/// A pile showing its top card and, with ShowCount, how many it holds (a deck, a graveyard, a drop zone, the soul).
/// With HideCards (a deck) the top card shows its back. Cards a presenter holds elsewhere (e.g. guards still shown in
/// the guard zone) aren't shown here until they are let go.
/// </summary>
[Tool]
public partial class StackZone : CardVerticalStack, IZoneView, IBindable<GroupSpace>, IShowsPartOfZone
{
    Presenter? presenter;
    ZoneSnapshot shownSnapshot = ZoneSnapshot.Empty;

    public ICardSpace? Space { get; private set; }

    /// <summary>Its cards are face-down, whatever their own facing (a deck).</summary>
    [Export] public bool HideCards { get; set; }

    /// <summary>Pressing it asks to show the whole zone (<see cref="InspectRequested"/>).</summary>
    [Export] public bool Inspectable { get; set; }

    public event Action<ZoneSnapshot>? Presented;
    public event Action<IZoneView>? InspectRequested;
    public event Action<IZoneView, CardBase?>? Clicked;

    [Export] public Color HighlightColor { get; set; } = new(1f, 0.85f, 0.2f);
    [Export] public float HighlightWidth { get; set; } = 4;
    [Export] public Color CardHighlightColor { get; set; } = new(1f, 0.85f, 0.2f);

    ReferenceRect? highlight;
    Func<CardBase, bool>? highlightedCards;

    public override void _Ready()
    {
        base._Ready();
        if(Engine.IsEditorHint()) return;
        highlight = ZoneInput.AddHighlight(this, HighlightColor, HighlightWidth);
    }

    public void SetHighlight(bool zone, Func<CardBase, bool>? cards)
    {
        if(highlight is not null) highlight.Visible = zone;
        highlightedCards = cards;
        OutlineTop();
    }

    private void OutlineTop()
    {
        if(VisiblePart(shownSnapshot).Top is { } top && FindCardNode(top.Card) is { } node)
        {
            node.Outline = highlightedCards?.Invoke(top.Card) == true ? CardHighlightColor : null;
        }
    }

    public void Bind(GroupSpace space)
    {
        Space = space;
        Render(ZoneSnapshot.Of(space));
    }

    public ZoneSnapshot Displayed
    {
        get
        {
            var top = VisiblePart(shownSnapshot).Top;
            if(top is null || FindCardNode(top.Value.Card) is not { } node) return ZoneSnapshot.Empty;
            return new([ new(node.CurrentCard, node.IsFront ? Facing.UP : Facing.DOWN) ], null);
        }
    }

    // Only the top card that isn't held elsewhere, as it is shown
    public ZoneSnapshot VisiblePart(ZoneSnapshot zone)
    {
        var top = zone.Cards.LastOrDefault(card => !IsHeld(card.Card));
        if(top.Card is null) return ZoneSnapshot.Empty;
        return new([ HideCards ? top with { Facing = Facing.DOWN } : top ], null);
    }

    public int? ShownTotal => ShowCount ? CountValue : null;

    public void Apply(ZoneChange change, PresentationContext? presentation)
    {
        presenter = presentation?.Presenter ?? presenter;
        switch(change)
        {
            case ZoneCardsArrived arrived:
                var arriving = arrived.Cards.Where(card => !IsHeld(card)).ToList();
                if(arriving.Count == 0)
                {
                    presentation.Present(() => Render(change.After));
                }
                foreach(var card in arriving)
                {
                    var snapshot = ZonePresentation.Find(change.After, card);
                    presentation.PresentArrival(card, arrived.Move, this, arrived.From, () => Render(change.After), !HideCards && snapshot.IsFaceUp);
                }
                break;

            case ZoneCardsLeft left:
                presentation.Present(() =>
                {
                    foreach(var leaving in left.Cards) presentation.Leaving(leaving, this);
                    Render(change.After);
                });
                break;

            case ZoneCardTurned turned:
                var after = ZonePresentation.Find(change.After, turned.Card);
                presentation.Present(async () =>
                {
                    if(!HideCards && FindCardNode(turned.Card) is { } node)
                    {
                        await ZonePresentation.Flip(presentation, node, after.IsFaceUp);
                    }
                    Render(change.After);
                });
                break;

            case ZoneShuffled:
                presentation.Present(async () =>
                {
                    var flights = presentation?.Presenter.Flights;
                    if(presentation is not null && flights is not null && Presenter.Animates(presentation.Side) && change.After.Count > 0)
                    {
                        await flights.Shuffle(GetCardTransform(null), SleeveInfo);
                    }
                    Render(change.After);
                });
                break;

            default:
                throw new NotSupportedException($"{Name} can't show a {change.GetType().Name}");
        }
        presentation.Present(() => Presented?.Invoke(change.After));
    }

    // A press is a click on its top card (the pile itself when empty); Inspectable also asks to show the whole pile
    protected override void OnPressed()
    {
        Clicked?.Invoke(this, VisiblePart(shownSnapshot).Top?.Card);
        if(Inspectable && Space is not null) InspectRequested?.Invoke(this);
    }

    private bool IsHeld(CardBase card) => presenter?.IsHeld(card) ?? false;

    private void Render(ZoneSnapshot snapshot)
    {
        shownSnapshot = snapshot;
        var visible = VisiblePart(snapshot);
        CountValue = snapshot.Count;

        if(visible.Top is not { } top)
        {
            ClearCard();
            return;
        }
        if(FindCardNode(top.Card) is null)
        {
            SetCard(top.Card);
        }
        FindCardNode(top.Card)!.IsFront = top.IsFaceUp;
        OutlineTop();
    }
}
