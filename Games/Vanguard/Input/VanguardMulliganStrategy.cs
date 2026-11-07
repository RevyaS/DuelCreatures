using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Components;

/// <summary>
/// The mulligan, as on the old board: drag the cards to redraw out of the hand into the selection panel (drag one back
/// out to keep it), then Confirm there. The hand shows the game again before answering; the game then puts the chosen
/// cards back into the deck.
/// </summary>
public sealed class VanguardMulliganStrategy(VanguardInput input, IVanguardAnswers below) : VanguardStrategy(input, below)
{
    public static class Text
    {
        public static string Title = "Select Cards to Mulligan";
    }

    public override async Task<List<CardBase>> SelectCardsFromHandRange(int minimum, int maximum)
    {
        var hand = (HandZone)Input.ViewOf(Input.PlayArea.Hand);
        var panel = Input.HandPicker;
        var confirmed = new TaskCompletionSource<List<CardBase>>();

        Action<CardBaseComponent> dragging = _ => panel.Droppable = true;
        Action<CardBaseComponent> dragCancelled = _ => panel.Droppable = false;
        // Into the panel: it leaves the hand's row
        Action<Card> selected = node =>
        {
            panel.Droppable = false;
            hand.RemoveCard(node);
        };
        // Dragged back out of the panel and let go: back into the hand
        Action<Card> returned = node =>
        {
            hand.AddCard(node);
            node.CurrentlyDragged = false;
        };
        Action<List<Card>> onConfirmed = nodes => confirmed.TrySetResult(nodes.Select(node => node.CurrentCard).ToList());

        hand.CardDragging += dragging;
        hand.CardDragCancelled += dragCancelled;
        panel.CardSelected += selected;
        panel.CardReturned += returned;
        panel.ConfirmedCards += onConfirmed;
        hand.SetDraggable(_ => true);
        panel.Activate(Text.Title, minimum, maximum);
        try
        {
            return await confirmed.Task;
        }
        finally
        {
            hand.CardDragging -= dragging;
            hand.CardDragCancelled -= dragCancelled;
            panel.CardSelected -= selected;
            panel.CardReturned -= returned;
            panel.ConfirmedCards -= onConfirmed;
            hand.SetDraggable(null);
            panel.Droppable = false;
            panel.Deactivate();
            // The chosen cards were only previewed in the panel: the hand shows the game again, then they move for real
            hand.Resync();
        }
    }
}
