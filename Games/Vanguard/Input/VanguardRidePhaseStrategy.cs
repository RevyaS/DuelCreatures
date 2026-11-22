using System.Threading.Tasks;
using ArC.CardGames.Actions;
using ArC.CardGames.Components;
using ArC.CardGames.Vanguard;

/// <summary>
/// The Ride Phase: drag a card from the hand onto the vanguard to ride it (only cards an offered RideRequest has can be
/// dragged), or Next Phase not to ride.
/// </summary>
public sealed class VanguardRidePhaseStrategy(VanguardInput input, Offered offered)
{
    public static class Text
    {
        public static string Ride = "Ride Phase: drag a card onto your vanguard to ride, or Next Phase";
    }

    public static class Choices
    {
        public static readonly DropChoice Ride = new("Ride");
    }

    VanguardPlayArea PlayArea => input.PlayArea;

    public async Task<Request> Answer()
    {
        using var scope = input.Board.Scope()
            .Drag(PlayArea.Hand, CanRide)
            .Drop(PlayArea.Vanguard, card => CanRide(card) ? [ Choices.Ride ] : [])
            .Button(BoardButton.NextPhase)
            .Hint(Text.Ride);

        switch(await scope.Next())
        {
            case CardDropped drop:
                return offered.One<RideRequest>(ride => ReferenceEquals(ride.Card, drop.Card), "Riding");
            default:
                input.Presentation.PlaySfx(VanguardSfx.PhaseChangeButton);
                return offered.EndPhase;
        }
    }

    private bool CanRide(CardBase card) => offered.Any<RideRequest>(ride => ReferenceEquals(ride.Card, card));
}
