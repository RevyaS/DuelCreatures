using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Vanguard;

/// <summary>
/// The Ride Phase, as on the old board: drag a card from the hand onto the vanguard to ride it (only cards that can
/// ride it can be dragged), or Next Phase not to ride.
/// </summary>
public sealed class VanguardRidePhaseStrategy(VanguardInput input, IVanguardAnswers below) : VanguardStrategy(input, below)
{
    public static class Text
    {
        public static string Ride = "Ride Phase: drag a card onto your vanguard to ride, or Next Phase";
    }

    public static class Choices
    {
        public static readonly DropChoice Ride = new("Ride");
    }

    VanguardPlayArea PlayArea => Input.PlayArea;

    public override async Task<CardBase?> SelectCardFromHandOrNot()
    {
        using var scope = Input.Board.Scope()
            .Drag(PlayArea.Hand, CanRide)
            .Drop(PlayArea.Vanguard, card => Refs.Has(PlayArea.Hand.Cards, card) && CanRide(card) ? [ Choices.Ride ] : [])
            .Button(BoardButton.NextPhase)
            .Hint(Text.Ride);

        switch(await scope.Next())
        {
            case CardDropped drop:
                return drop.Card;
            default:
                Input.Presentation.PlaySfx(VanguardSfx.PhaseChange);
                return null;
        }
    }

    private bool CanRide(CardBase card)
        => card is VanguardCard newVanguard && PlayArea.Vanguard.Card is { } current && VanguardGameRules.CanRide(current, newVanguard);
}
