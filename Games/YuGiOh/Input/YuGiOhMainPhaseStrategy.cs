using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Actions;
using ArC.CardGames.Components;
using ArC.CardGames.YuGiOh;
using ArC.CardGames.Skills;

/// <summary>
/// The Main Phase by touch, for the turn player:
/// <list type="bullet">
/// <item>drag a monster from the hand onto a monster zone, on Summon or Set (a tribute summon then asks for its tributes: tap them, Confirm);</item>
/// <item>drag a spell onto a spell &amp; trap zone (or a field spell onto the Field Zone), on Activate or Set; a trap, on Set;</item>
/// <item>swipe a monster left for Defense Position, right for Attack Position (a face-down one is Flip Summoned);</item>
/// <item>tap a card for its details: activate an effect there, Activate a Set card, or Xyz Summon a monster from the
/// Extra Deck's list (then tap its materials, Confirm);</item>
/// <item>Next Phase, or End Phase.</item>
/// </list>
/// Only what an offered request uses can be dragged, dropped, swiped or pressed.
/// </summary>
public sealed class YuGiOhMainPhaseStrategy(YuGiOhInput input, Offered offered)
{
    public static class Text
    {
        public static string Play = "Your Main Phase: drag a card to play it, tap a card for its effects, swipe a monster to change its position";
        public static string Tributes = "Tribute";
        public static string Materials = "Xyz Materials";
        public static string Zone = "Tap the zone to summon it to";
    }

    /// <summary>The regions a zone splits into while a card from the hand hovers over it.</summary>
    public static class Choices
    {
        public static readonly DropChoice Summon = new("Summon");
        public static readonly DropChoice Set = new("Set");
        public static readonly DropChoice Activate = new("Activate");
    }

    /// <summary>What the card details offer beside effects.</summary>
    public static class Commands
    {
        public static readonly CardCommand XyzSummon = new("Xyz Summon");
        public static readonly CardCommand ActivateSet = new("Activate");
    }

    YuGiOhPlayArea PlayArea => input.PlayArea;

    public async Task<Request> Answer()
    {
        IGesture gesture;
        using(var scope = input.Board.Scope()
            .Drag(PlayArea.Hand, CanPlayFromHand)
            .Activatable(offered.CanActivate)
            .Commands(CommandsFor)
            .Button(BoardButton.NextPhase)
            .Hint(Text.Play))
        {
            if(offered.Any<SkipToEndPhaseRequest>(_ => true)) scope.Button(BoardButton.EndPhase);
            foreach(var zone in PlayArea.MonsterZones) scope.Drop(zone, card => MonsterChoices(card, zone));
            foreach(var zone in PlayArea.SpellAndTrapZones.Append<SpellAndTrapSpace>(PlayArea.FieldZone)) scope.Drop(zone, card => SpellTrapChoices(card, zone));
            foreach(var zone in PlayArea.ControlledMonsterZones) scope.Swipe(zone, direction => SwipeAction(zone, direction) is not null);
            gesture = await scope.Next();
        }

        switch(gesture)
        {
            case ButtonPressed { Button: BoardButton.NextPhase }:
                return offered.EndPhase;
            case ButtonPressed { Button: BoardButton.EndPhase }:
                return offered.Of<SkipToEndPhaseRequest>().Single();

            case CardDropped { Card: var monster, To: MonsterZone zone } drop:
                return await Summon(monster, zone, setting: ReferenceEquals(drop.Choice, Choices.Set));
            case CardDropped { Card: var card, To: SpellAndTrapSpace zone } drop when ReferenceEquals(drop.Choice, Choices.Activate):
                return offered.One<PlayAbilityRequest>(play => ReferenceEquals(play.Card, card) && ReferenceEquals(play.To, zone), $"Activating {card.Name}");
            case CardDropped { Card: var card, To: SpellAndTrapSpace zone }:
                return offered.One<SetCardRequest>(set => ReferenceEquals(set.Card, card) && ReferenceEquals(set.To, zone), $"Setting {card.Name}");

            case ZoneSwiped { Zone: MonsterZone zone } swipe:
                return SwipeAction(zone, swipe.Direction)!;

            case CommandPressed { Command: var command, Card: var xyz } when ReferenceEquals(command, Commands.XyzSummon):
                input.CardList.Hide();
                return await XyzSummon(xyz);
            case CommandPressed { Command: var command, From: SpellAndTrapZone zone } when ReferenceEquals(command, Commands.ActivateSet):
                return offered.One<ActivateSetRequest>(activation => ReferenceEquals(activation.Zone, zone), "Activating a Set card");

            case SkillActivated activation:
                input.CardList.Hide();
                return offered.Activation(activation.Card, activation.From, activation.Skill);

            case var other:
                throw new InvalidOperationException($"{nameof(YuGiOhMainPhaseStrategy)} allowed {other} but doesn't handle it");
        }
    }

    #region Summons
    // What a monster from the hand can be dropped on this zone as: the offered summons and sets with that monster and zone
    private IReadOnlyList<DropChoice> MonsterChoices(CardBase card, MonsterZone zone)
    {
        List<DropChoice> choices = [];
        if(offered.Any<NormalSummonRequest>(summon => IsFor(summon, card, zone))) choices.Add(Choices.Summon);
        if(offered.Any<SetMonsterRequest>(set => IsFor(set, card, zone))) choices.Add(Choices.Set);
        return choices;
    }

    private static bool IsFor(NormalSummonOrSetRequest request, CardBase card, MonsterZone zone)
        => ReferenceEquals(request.Monster, card) && ReferenceEquals(request.Zone, zone);

    // The offered summon (or set) of the monster to the zone; when several fit, they differ by their tributes: tap them
    private async Task<Request> Summon(CardBase monster, MonsterZone zone, bool setting)
    {
        var fits = offered.Of<NormalSummonOrSetRequest>()
            .Where(request => (request is SetMonsterRequest) == setting && IsFor(request, monster, zone))
            .ToList();
        if(fits.Count == 0) throw new InvalidOperationException($"No offered {(setting ? "Set" : "Normal Summon")} of {monster.Name} to that zone. Offered: {offered.Describe()}");
        if(fits.Count == 1) return fits[0];
        var tributed = await PickZonesOf(fits, request => request.Tributes, Text.Tributes);
        return tributed.Single();
    }

    // The offered Xyz Summons of the monster: tap its materials, then (when they could go to several zones) its zone
    private async Task<Request> XyzSummon(CardBase xyz)
    {
        var fits = offered.Of<XyzSummonRequest>().Where(summon => ReferenceEquals(summon.Monster, xyz)).ToList();
        var withMaterials = await PickZonesOf(fits, summon => summon.Materials, Text.Materials);
        if(withMaterials.Count == 1) return withMaterials[0];
        var zone = (await YuGiOhChoices.ChooseZones(input, withMaterials.Select(summon => (ICardSpace)summon.Zone).Distinct(ReferenceEqualityComparer.Instance).Cast<ICardSpace>().ToList(), 1, 1, Text.Zone)).Single();
        return withMaterials.First(summon => ReferenceEquals(summon.Zone, zone));
    }

    /// <summary>
    /// Tap the zones one of <paramref name="fits"/> names (tributes, materials): only zones that some request still fits
    /// with can be tapped, badges number them, a tap on a chosen one drops it, and Confirm shows once the chosen zones
    /// are exactly some request's. Returns the requests with exactly those zones.
    /// </summary>
    private async Task<List<TRequest>> PickZonesOf<TRequest>(List<TRequest> fits, Func<TRequest, IReadOnlyList<MonsterZone>> zonesOf, string hint) where TRequest : Request
    {
        List<MonsterZone> chosen = [];
        bool Covers(TRequest request, IEnumerable<MonsterZone> zones) => zones.All(zone => Refs.Has(zonesOf(request), zone));
        bool IsExactly(TRequest request) => zonesOf(request).Count == chosen.Count && Covers(request, chosen);

        while(true)
        {
            var exact = fits.Where(IsExactly).ToList();
            var next = fits.Where(request => Covers(request, chosen))
                .SelectMany(zonesOf)
                .Where(zone => !Refs.Has(chosen, zone))
                .Distinct(ReferenceEqualityComparer.Instance)
                .Cast<ICardSpace>()
                .ToList();

            using var scope = input.Board.Scope().Hint(hint).Numbered(chosen).Zones(next);
            foreach(var zone in chosen) scope.Cards(zone);
            if(exact.Count > 0) scope.Button(BoardButton.Confirm);

            switch(await scope.Next())
            {
                case ButtonPressed:
                    return exact;
                case ZoneClicked { Zone: MonsterZone zone }:
                    chosen.Add(zone);
                    break;
                case CardClicked { Zone: MonsterZone zone }:
                    Refs.Remove(chosen, zone);
                    break;
            }
        }
    }
    #endregion

    // A spell can be activated or Set, a trap only Set: as the offered requests say for this card and zone
    private IReadOnlyList<DropChoice> SpellTrapChoices(CardBase card, SpellAndTrapSpace zone)
    {
        List<DropChoice> choices = [];
        if(offered.Any<PlayAbilityRequest>(play => ReferenceEquals(play.Card, card) && ReferenceEquals(play.To, zone))) choices.Add(Choices.Activate);
        if(offered.Any<SetCardRequest>(set => ReferenceEquals(set.Card, card) && ReferenceEquals(set.To, zone))) choices.Add(Choices.Set);
        return choices;
    }

    private bool CanPlayFromHand(CardBase card)
        => offered.Any<NormalSummonOrSetRequest>(request => ReferenceEquals(request.Monster, card))
        || offered.Any<PlayAbilityRequest>(play => ReferenceEquals(play.Card, card))
        || offered.Any<SetCardRequest>(set => ReferenceEquals(set.Card, card));

    // Left: to Defense Position. Right: to Attack Position, or a Flip Summon of a face-down monster. The direction is read
    // from the board; whether it's allowed, from the offered requests
    private Request? SwipeAction(MonsterZone zone, GestureDetector.SwipeDirection direction)
    {
        if(zone.Card is null) return null;
        var change = offered.Of<ChangePositionRequest>().FirstOrDefault(request => ReferenceEquals(request.Zone, zone));
        var flip = offered.Of<FlipSummonRequest>().FirstOrDefault(request => ReferenceEquals(request.Zone, zone));
        return direction switch
        {
            GestureDetector.SwipeDirection.Left when zone.IsInAttackPosition => change,
            GestureDetector.SwipeDirection.Right when !zone.Card.IsFaceUp => flip,
            GestureDetector.SwipeDirection.Right when !zone.IsInAttackPosition => change,
            _ => null,
        };
    }

    // An Xyz monster in the player's Extra Deck that an offered Xyz Summon brings out; a Set card an offered activation turns face up
    private IEnumerable<CardCommand> CommandsFor(CardBase card, ICardSpace? from)
    {
        if(ReferenceEquals(from, PlayArea.ExtraMonsterDeck) && offered.Any<XyzSummonRequest>(summon => ReferenceEquals(summon.Monster, card)))
        {
            yield return Commands.XyzSummon;
        }
        if(from is SpellAndTrapZone zone && !card.IsFaceUp && offered.Any<ActivateSetRequest>(activation => ReferenceEquals(activation.Zone, zone)))
        {
            yield return Commands.ActivateSet;
        }
    }
}
