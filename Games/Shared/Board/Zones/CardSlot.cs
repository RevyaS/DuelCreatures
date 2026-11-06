using System;
using System.Linq;
using ArC.CardGames.Components;
using Godot;
using Orientation = ArC.CardGames.Components.Orientation;

/// <summary>
/// One card, in a space shaped like a card (e.g. a trigger zone, a field zone, a spell &amp; trap zone): its card is
/// never turned sideways, and being told to is an error naming the zone (show it with a <see cref="CardBox"/>
/// instead). Shows any one-card zone.
/// </summary>
[Tool]
public partial class CardSlot : CardArea, IZoneView, IBindable<ISingleSpace>
{
    public ICardSpace? Space { get; private set; }

    public event Action<ZoneSnapshot>? Presented;
    public event Action<IZoneView, CardBase>? CardInspected;

    public override void _Ready()
    {
        base._Ready();
        if(Engine.IsEditorHint()) return;
        CardPressed += node =>
        {
            if(node.IsFront && node.CurrentCard is { } card) CardInspected?.Invoke(this, card);
        };
    }

    /// <summary>Whether a card here may lie sideways. A <see cref="CardBox"/>'s square fits one; a card-shaped slot doesn't.</summary>
    protected virtual bool AllowsSideways => false;

    public void Bind(ISingleSpace space)
    {
        Space = space;
        var snapshot = ZoneSnapshot.Of(space);
        EnsureFits(snapshot);
        Render(snapshot);
    }

    public ZoneSnapshot Displayed => Container.CurrentCard is { CurrentCard: { } card } node
        ? new([ new(card, node.IsFront ? Facing.UP : Facing.DOWN) ], Container.CardOrientation)
        : new([], Container.CardOrientation);

    public void Apply(ZoneChange change, PresentationContext? presentation)
    {
        EnsureFits(change.After);
        switch(change)
        {
            case ZoneCardsArrived arrived:
                if(arrived.Cards.Count != 1) throw new InvalidOperationException($"{Name} shows one card, {arrived.Cards.Count} arrived");
                var card = ZonePresentation.Find(change.After, arrived.Cards[0]);
                presentation.PresentArrival(card.Card, arrived.Move, this, arrived.From, () => Render(change.After), card.IsFaceUp);
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
                    if(FindCardNode(turned.Card) is { } node)
                    {
                        await ZonePresentation.Flip(presentation, node, after.IsFaceUp);
                    }
                    Render(change.After);
                });
                break;

            default:
                throw new NotSupportedException($"{Name} can't show a {change.GetType().Name}");
        }
        presentation.Present(() => Presented?.Invoke(change.After));
    }

    // Shows the snapshot's card as it is, keeping the node already showing it
    private void Render(ZoneSnapshot snapshot)
    {
        if(snapshot.Top is not { } top)
        {
            ClearCard();
            return;
        }
        if(FindCardNode(top.Card) is { } node)
        {
            node.IsFront = top.IsFaceUp;
        }
        else
        {
            AddCard(ZonePresentation.CreateNode(top));
        }
        Container.ChangeOrientation(snapshot.Orientation ?? Orientation.VERTICAL);
    }

    private void EnsureFits(ZoneSnapshot snapshot)
    {
        if(AllowsSideways || snapshot.Count == 0 || snapshot.Orientation != Orientation.HORIZONTAL) return;
        throw new InvalidOperationException(
            $"{GetPath()} is a {nameof(CardSlot)}, which can't show a card sideways ({snapshot.Cards.First().Card.Name}): show this zone with a {nameof(CardBox)}");
    }
}
