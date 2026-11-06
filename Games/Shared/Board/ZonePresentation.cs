using System;
using System.Threading.Tasks;
using ArC.CardGames.Components;

// What every zone view does the same way when presenting a change
public static class ZonePresentation
{
    /// <summary>Turns <paramref name="node"/> over, animated when the context's side animates.</summary>
    public static async Task Flip(PresentationContext? presentation, Card node, bool faceUp)
    {
        if(node.IsFront == faceUp) return;
        var flights = presentation?.Presenter.Flights;
        if(presentation is not null && flights is not null && Presenter.Animates(presentation.Side))
        {
            await flights.Flip(node, faceUp);
        }
        else
        {
            node.IsFront = faceUp;
        }
    }

    /// <summary>The snapshot of <paramref name="card"/> in <paramref name="snapshot"/>; fails when the change's snapshot doesn't hold it.</summary>
    public static CardSnapshot Find(ZoneSnapshot snapshot, CardBase card)
    {
        foreach(var shown in snapshot.Cards)
        {
            if(ReferenceEquals(shown.Card, card)) return shown;
        }
        throw new InvalidOperationException($"{card.Name} is not in the zone's snapshot: the change and its snapshot disagree");
    }

    /// <summary>A new card node for <paramref name="card"/>, facing as given.</summary>
    public static Card CreateNode(CardSnapshot card)
    {
        var node = CardScenes.Create(card.Card);
        node.IsFront = card.IsFaceUp;
        return node;
    }
}
