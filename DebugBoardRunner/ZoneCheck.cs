using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Bots;
using ArC.CardGames.Components;
using ArC.CardGames.Rules;
using ArC.CardGames.Setup;
using ArC.CardGames.YuGiOh;
using ArC.CardGames.YuGiOh.Bots;
using Godot;
using Orientation = ArC.CardGames.Components.Orientation;

/// <summary>
/// Plays a scripted YuGiOh sequence (real ArC moves and turns) through the generic zone scenes, then checks after every
/// step that each zone shows what its ArC zone holds (ZoneDesync), plus what the step is about. Run it with F6; the
/// results are printed and shown on screen. A stand-in for ArcDriver (plan step 5): it turns ArC's CardsMoved and
/// CardTurned into zone changes the same way.
/// </summary>
public partial class ZoneCheck : Control
{
    record ZoneCheckMove : IMoveReason;

    static readonly CardMove CheckMove = new(nameof(CheckMove));

    readonly Presenter presenter = new();
    readonly Dictionary<ICardSpace, IZoneView> views = new(ReferenceEqualityComparer.Instance);
    readonly List<string> results = [];
    int failures;

    StackZone Deck = null!, Graveyard = null!;
    HandZone Hand = null!;
    CardBox MonsterZone1 = null!;
    CardSlot SpellAndTrapZone1 = null!;
    LineZone BanishZone = null!;
    Label Results = null!;
    PresentationContext presentation = null!;

    public override async void _Ready()
    {
        Deck = GetNode<StackZone>($"%{nameof(Deck)}");
        Graveyard = GetNode<StackZone>($"%{nameof(Graveyard)}");
        Hand = GetNode<HandZone>($"%{nameof(Hand)}");
        MonsterZone1 = GetNode<CardBox>($"%{nameof(MonsterZone1)}");
        SpellAndTrapZone1 = GetNode<CardSlot>($"%{nameof(SpellAndTrapZone1)}");
        BanishZone = GetNode<LineZone>($"%{nameof(BanishZone)}");
        Results = GetNode<Label>($"%{nameof(Results)}");
        presenter.Flights = GetNode<CardFlightLayer>("%CardFlights");
        presentation = new(presenter, new SfxBus(), BoardSide.Player);

        try
        {
            await Run();
        }
        catch(Exception e)
        {
            Fail($"Stopped: {e}");
        }

        Report(failures == 0 ? "ZONE CHECK PASSED" : $"ZONE CHECK FAILED ({failures})");
    }

    private async Task Run()
    {
        // Cards without skills, so nothing triggers and asks for input
        var monsters = Enumerable.Range(1, 6).Select(i => (CardBase)new YuGiOhNormalMonsterCard(4, $"Zone Check Monster {i}", [], 1000, 1000, [ MonsterType.EFFECT ], [])).ToList();
        var spells = Enumerable.Range(1, 4).Select(i => (CardBase)new YuGiOhSpellCard($"Zone Check Spell {i}", SpellProperty.CONTINUOUS, [], CardScopedRestriction.UNLIMITED, [])).ToList();
        var deck = DeckBuilder.Create();
        foreach(var card in monsters.Concat(spells)) deck.AddCards(card);

        // Bots: the skill service looks up the players on every event, but with skill-less cards it never asks them anything
        var (session, services) = YuGiOhSessionBuilder.Create()
            .Player1(new(deck, DeckBuilder.Create()), s => new ExosisterTacticalTryDeckBot(s.Game.Player1, s.DataQuery, s.SkillService, s.GameContext, BotDifficulty.MASTER))
            .Player2(new(DeckBuilder.Create().AddCards(monsters[0]), DeckBuilder.Create()), s => new ExosisterTacticalTryDeckBot(s.Game.Player2, s.DataQuery, s.SkillService, s.GameContext, BotDifficulty.MASTER))
            .Build();
        session.StartGame();
        var area = services.Game.Board.Player1Area;

        // Binding goes through Rev-Godot-Core, as the boards will
        var report = new BindingReport();
        Bind(Deck, area.Deck, report);
        Bind(Hand, area.Hand, report);
        Bind(MonsterZone1, area.MonsterZone1, report);
        Bind(SpellAndTrapZone1, area.SpellAndTrapZone1, report);
        Bind(Graveyard, area.Graveyard, report);
        Bind(BanishZone, area.BanishZone, report);
        report.ThrowIfAny();

        services.EventBus.Subscribe<CardsMoved>(OnCardsMoved);
        services.EventBus.Subscribe<CardTurned>(OnCardTurned);

        await Step("Bound: the deck shows 10 face-down", () => Task.CompletedTask,
            () => Expect(Deck.CountValue == 10 && Deck.Displayed.Top is { IsFaceUp: false }, "deck count 10, top face-down"));

        await Step("Draw 8 into the hand", () => area.Draw(8),
            () => Expect(Hand.Displayed.Count == 8 && Deck.CountValue == 2, "8 in hand, 2 in the deck"));

        var monster = area.Hand.Cards.OfType<YuGiOhNormalMonsterCard>().First();
        await Step("Summon into the CardBox", () => area.SummonCard(monster, area.Hand, area.MonsterZone1),
            () => Expect(Shows(MonsterZone1, monster) && Hand.Displayed.Count == 7, "the monster is in the box, 7 in hand"));

        await Step("Turn it sideways (Defense Position)", () => area.Turn(area.MonsterZone1, orientation: Orientation.HORIZONTAL),
            () => Expect(MonsterZone1.Displayed.Orientation == Orientation.HORIZONTAL, "shown sideways"));

        await Step("Flip it face-down", () => area.Turn(area.MonsterZone1, Facing.DOWN),
            () => Expect(MonsterZone1.Displayed.Top is { IsFaceUp: false }, "shown face-down"));

        await Step("Send it to the graveyard", () => area.Move(monster, area.MonsterZone1, area.Graveyard, new ZoneCheckMove()),
            () => Expect(MonsterZone1.Displayed.Count == 0 && Graveyard.Displayed.Top?.Card == monster, "the box is empty, the graveyard shows it on top"));

        var spell = area.Hand.Cards.OfType<YuGiOhSpellCard>().First();
        await Step("Activate a spell into the CardSlot", () => area.ActivateCard(spell, area.Hand, area.SpellAndTrapZone1),
            () => Expect(Shows(SpellAndTrapZone1, spell), "the spell is in the slot"));

        var banished = area.Hand.Cards.OfType<YuGiOhNormalMonsterCard>().Take(2).Cast<CardBase>().ToList();
        await Step("Banish two together into the LineZone", () => area.MoveCards(banished, area.Hand, area.BanishZone, new ZoneCheckMove()),
            () => Expect(BanishZone.Displayed.Count == 2, "two in the line"));

        await Step("Flip one banished card face-down in place", () => area.Turn(area.BanishZone, banished[1], Facing.DOWN),
            () => Expect(BanishZone.Displayed.Cards[1] is { IsFaceUp: false } && BanishZone.Displayed.Cards[0].IsFaceUp, "only the second is face-down"));

        await Step("Return a banished card to the hand", () => area.Move(banished[0], area.BanishZone, area.Hand, new ZoneCheckMove()),
            () => Expect(BanishZone.Displayed.Count == 1 && Hand.Displayed.Cards.Any(card => card.Card == banished[0]), "back in hand"));

        // A CardSlot refuses a sideways card, naming itself
        try
        {
            SpellAndTrapZone1.Apply(new ZoneCardTurned(spell, new([ new(spell, Facing.UP) ], Orientation.HORIZONTAL)), null);
            Fail("A CardSlot showed a sideways card");
        }
        catch(InvalidOperationException e) when (e.Message.Contains(nameof(CardBox)))
        {
            Pass("A CardSlot refuses a sideways card");
        }
    }

    private void Bind(Node view, ICardSpace space, BindingReport report)
    {
        Bindings.TryBind(view, space, Name, view.Name, report);
        views[space] = (IZoneView)view;
    }

    // What ArcDriver will do: one ArC event, one change per shown zone, with each zone's content as of the event
    private void OnCardsMoved(CardsMoved moved)
    {
        var from = views.GetValueOrDefault(moved.From);
        var to = views.GetValueOrDefault(moved.To);
        from?.Apply(new ZoneCardsLeft(moved.Cards, to, ZoneSnapshot.Of(moved.From)), presentation);
        to?.Apply(new ZoneCardsArrived(moved.Cards, from, CheckMove, ZoneSnapshot.Of(moved.To)), presentation);
    }

    private void OnCardTurned(CardTurned turned)
    {
        views.GetValueOrDefault(turned.Space)?.Apply(new ZoneCardTurned(turned.Card, ZoneSnapshot.Of(turned.Space)), presentation);
    }

    private async Task Step(string name, Func<Task> act, Func<string?> check)
    {
        await act();
        await presenter.WhenIdle();

        var problems = ZoneDesync.Find(views.Values).ToList();
        if(check() is { } failed) problems.Add(failed);

        if(problems.Count == 0) Pass(name);
        else Fail($"{name}: {string.Join("; ", problems)}");
    }

    private static string? Expect(bool holds, string what) => holds ? null : $"expected {what}";

    private static bool Shows(IZoneView view, CardBase card) => view.Displayed.Cards.Any(shown => ReferenceEquals(shown.Card, card));

    private void Pass(string what) => Report($"PASS  {what}");

    private void Fail(string what)
    {
        failures++;
        GD.PushError($"ZoneCheck: {what}");
        Report($"FAIL  {what}");
    }

    private void Report(string line)
    {
        GD.Print(line);
        results.Add(line);
        Results.Text = string.Join("\n", results);
    }
}
