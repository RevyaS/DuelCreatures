using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Rules;

/// <summary>
/// What the player may do for one question, in game terms (ArC cards and zones): which cards and zones they can tap,
/// which cards they can drag where (and with which choices), which zones they can swipe or aim from, which HUD buttons
/// they can press, and what the card details may do. What is allowed is highlighted and switched on while the scope
/// lasts; <see cref="Next"/> waits for an allowed gesture. Disposing it switches it all off.
/// <code>
/// using var scope = Board.Scope()
///     .Drag(PlayArea.Hand, card => card is YuGiOhSpellCard)
///     .Drop(PlayArea.SpellAndTrapZone1, card => [ Activate, Set ])
///     .Swipe(PlayArea.MonsterZone1, direction => direction == Left)
///     .Aim(PlayArea.MonsterZone1, target => target is ZoneTarget)
///     .Button(BoardButton.NextPhase);
/// var gesture = await scope.Next();
/// </code>
/// A tap on a card in a zone the scope offers as a zone (but not that card) counts as tapping the zone.
/// </summary>
public sealed class InputScope : InteractionScope
{
    readonly InputBoard board;
    readonly List<(ICardSpace Zone, Func<CardBase, bool> Allowed)> cards = [];
    readonly HashSet<ICardSpace> zones = new(ReferenceEqualityComparer.Instance);
    readonly HashSet<BoardButton> buttons = [];
    readonly HashSet<IZoneView> highlighted = new(ReferenceEqualityComparer.Instance);
    readonly Dictionary<ICardSpace, Func<CardBase, IReadOnlyList<DropChoice>>> drops = new(ReferenceEqualityComparer.Instance);
    readonly Dictionary<ICardSpace, Func<GestureDetector.SwipeDirection, bool>> swipes = new(ReferenceEqualityComparer.Instance);
    readonly Dictionary<ICardSpace, Func<AimTarget, bool>> aims = new(ReferenceEqualityComparer.Instance);
    readonly Dictionary<ICardSpace, ICardSpace> aimVias = new(ReferenceEqualityComparer.Instance);
    readonly List<(ICardSpace Zone, Func<CardBase, bool> Allowed)> longPresses = [];
    Func<CardBase, ICardSpace?, ISkill, bool>? activatable;
    Func<CardBase, ICardSpace?, IEnumerable<CardCommand>>? commands;
    bool gateInUse;

    internal InputScope(InputBoard board)
    {
        this.board = board;
        board.Enter(this);
        OnDispose(() => board.Leave(this));
    }

    #region Taps and buttons
    /// <summary>The cards of <paramref name="zone"/> that <paramref name="allowed"/> accepts (every card when null) can be tapped.</summary>
    public InputScope Cards(ICardSpace zone, Func<CardBase, bool>? allowed = null)
    {
        EnsureOpen();
        cards.Add((zone, allowed ?? (_ => true)));
        Highlight(zone);
        return this;
    }

    /// <summary><paramref name="offered"/> can be tapped as zones (a tap on their cards counts too).</summary>
    public InputScope Zones(IEnumerable<ICardSpace> offered)
    {
        EnsureOpen();
        foreach(var zone in offered)
        {
            zones.Add(zone);
            Highlight(zone);
        }
        return this;
    }

    public InputScope Button(BoardButton button)
    {
        EnsureOpen();
        var view = board.Button(button);
        Set(() => view.Available, available => view.Available = available, true);
        buttons.Add(button);
        return this;
    }

    /// <summary>
    /// Numbers <paramref name="picked"/> 1, 2, … on the board (IZoneView.SetPickNumber), in that order, for as long as
    /// the scope lasts (e.g. the Xyz materials picked so far, which can be tapped again to drop them).
    /// </summary>
    public InputScope Numbered(IEnumerable<ICardSpace> picked)
    {
        EnsureOpen();
        int number = 1;
        foreach(var zone in picked)
        {
            var view = board.ViewOf(zone);
            view.SetPickNumber(number++);
            OnDispose(() => view.SetPickNumber(null));
        }
        return this;
    }

    /// <summary>Says what is asked, on the HUD's InputHintLabel (if it has one), for as long as the scope lasts.</summary>
    public InputScope Hint(string text)
    {
        EnsureOpen();
        if(board.Hint is { } label) Set(() => label.Text, value => label.Text = value, text);
        return this;
    }

    /// <summary>The cards of <paramref name="zone"/> that <paramref name="allowed"/> accepts can be long pressed (a CardLongPressed).</summary>
    public InputScope LongPress(ICardSpace zone, Func<CardBase, bool> allowed)
    {
        EnsureOpen();
        longPresses.Add((zone, allowed));
        return this;
    }
    #endregion

    #region Drags, swipes and aims
    /// <summary>The cards of <paramref name="zone"/> that <paramref name="allowed"/> accepts can be dragged out (e.g. from the hand).</summary>
    public InputScope Drag(ICardSpace zone, Func<CardBase, bool> allowed)
    {
        EnsureOpen();
        var view = board.ViewOf(zone);
        view.SetDraggable(allowed);
        OnDispose(() => view.SetDraggable(null));
        return this;
    }

    /// <summary>
    /// <paramref name="zone"/> takes dragged cards: <paramref name="choicesFor"/> gives each card's choices, shown as
    /// regions while it hovers (no choices: it doesn't take that card). Dropping is a CardDropped.
    /// </summary>
    public InputScope Drop(ICardSpace zone, Func<CardBase, IReadOnlyList<DropChoice>> choicesFor)
    {
        EnsureOpen();
        var view = board.ViewOf(zone);
        drops[zone] = choicesFor;
        view.OfferDrop(new DropOffer(choicesFor));
        OnDispose(() => view.OfferDrop(null));
        return this;
    }

    /// <summary>Each of <paramref name="offered"/> takes dragged cards with the choices <paramref name="choicesFor"/> gives.</summary>
    public InputScope Drop(IEnumerable<ICardSpace> offered, Func<CardBase, IReadOnlyList<DropChoice>> choicesFor)
    {
        foreach(var zone in offered) Drop(zone, choicesFor);
        return this;
    }

    /// <summary><paramref name="zone"/> can be swiped in the directions <paramref name="allowed"/> accepts (a ZoneSwiped).</summary>
    public InputScope Swipe(ICardSpace zone, Func<GestureDetector.SwipeDirection, bool> allowed)
    {
        EnsureOpen();
        swipes[zone] = allowed;
        UpdateGestures(zone);
        return this;
    }

    /// <summary>
    /// A drag from <paramref name="zone"/> can be let go on the targets <paramref name="canTarget"/> accepts (an Aimed):
    /// a line follows the drag and snaps to such a target.
    /// </summary>
    public InputScope Aim(ICardSpace zone, Func<AimTarget, bool> canTarget)
    {
        EnsureOpen();
        aims[zone] = canTarget;
        UpdateGestures(zone);
        return this;
    }

    /// <summary>
    /// A drag from <paramref name="zone"/> that first passes over <paramref name="via"/> can be let go on the targets
    /// <paramref name="canTarget"/> accepts (an Aimed with Via, e.g. a boost: from the booster, over the unit it boosts,
    /// to the target). Until it passes over <paramref name="via"/>, the line only reaches for it.
    /// </summary>
    public InputScope Aim(ICardSpace zone, ICardSpace via, Func<AimTarget, bool> canTarget)
    {
        Aim(zone, canTarget);
        aimVias[zone] = via;
        return this;
    }

    internal bool CanAimFrom(ICardSpace zone) => aims.ContainsKey(zone);
    internal ICardSpace? ViaOf(ICardSpace zone) => aimVias.GetValueOrDefault(zone);
    internal bool CanAim(ICardSpace from, AimTarget target) => aims.TryGetValue(from, out var canTarget) && canTarget(target);

    private void UpdateGestures(ICardSpace zone)
    {
        var view = board.ViewOf(zone);
        bool first = !swipes.ContainsKey(zone) || !aims.ContainsKey(zone);
        view.SetGestures(swipes.ContainsKey(zone), aims.ContainsKey(zone));
        if(first) OnDispose(() => view.SetGestures(false, false));
    }
    #endregion

    #region Card details
    /// <summary>The card details may activate the skills <paramref name="allowed"/> accepts; pressing one is a SkillActivated.</summary>
    public InputScope Activatable(Func<CardBase, ICardSpace?, ISkill, bool> allowed)
    {
        EnsureOpen();
        activatable = allowed;
        UseGate();
        return this;
    }

    /// <summary>The card details offer the commands <paramref name="offered"/> gives for a card where it is; pressing one is a CommandPressed.</summary>
    public InputScope Commands(Func<CardBase, ICardSpace?, IEnumerable<CardCommand>> offered)
    {
        EnsureOpen();
        commands = offered;
        UseGate();
        return this;
    }

    private void UseGate()
    {
        if(gateInUse) return;
        gateInUse = true;
        Track(board.Activations.Use(new ScopeGate(this)));
    }
    #endregion

    /// <summary>The next gesture this scope allows. Cancelled when the scope is disposed.</summary>
    public async Task<IGesture> Next()
    {
        var gesture = await Next<IGesture, IGesture>(board.Gestures, candidate => Interpret(candidate) is not null);
        return Interpret(gesture)!;
    }

    internal bool Takes(IGesture gesture) => !IsDisposed && Interpret(gesture) is not null;

    // The gesture as this scope takes it, or null when it doesn't allow it
    private IGesture? Interpret(IGesture gesture) => gesture switch
    {
        CardLongPressed press when longPresses.Any(rule => ReferenceEquals(rule.Zone, press.Zone) && rule.Allowed(press.Card)) => press,
        CardClicked click when cards.Any(rule => ReferenceEquals(rule.Zone, click.Zone) && rule.Allowed(click.Card)) => click,
        CardClicked click when zones.Contains(click.Zone) => new ZoneClicked(click.Zone),
        ZoneClicked click when zones.Contains(click.Zone) => click,
        ButtonPressed press when buttons.Contains(press.Button) => press,
        CardDropped drop when drops.TryGetValue(drop.To, out var choicesFor) && choicesFor(drop.Card).Contains(drop.Choice) => drop,
        ZoneSwiped swipe when swipes.TryGetValue(swipe.Zone, out var allowed) && allowed(swipe.Direction) => swipe,
        Aimed aim when CanAim(aim.From, aim.Target) && ReferenceEquals(aim.Via, ViaOf(aim.From)) => aim,
        SkillActivated activation when activatable?.Invoke(activation.Card, activation.From, activation.Skill) == true => activation,
        CommandPressed command when commands?.Invoke(command.Card, command.From).Contains(command.Command) == true => command,
        _ => null,
    };

    private void Highlight(ICardSpace zone)
    {
        var view = board.ViewOf(zone);
        if(highlighted.Add(view)) OnDispose(() => view.SetHighlight(false, null));
        view.SetHighlight(zones.Any(offered => ReferenceEquals(board.ViewOf(offered), view)), CardsAllowedIn(view));
    }

    private Func<CardBase, bool>? CardsAllowedIn(IZoneView view)
    {
        var rules = cards.Where(rule => ReferenceEquals(board.ViewOf(rule.Zone), view)).Select(rule => rule.Allowed).ToList();
        return rules.Count == 0 ? null : card => rules.Any(allowed => allowed(card));
    }

    sealed class ScopeGate(InputScope scope) : IActivationGate
    {
        public bool CanActivate(CardBase card, ICardSpace? from, ISkill skill)
            => !scope.IsDisposed && scope.activatable?.Invoke(card, from, skill) == true;

        public void Activate(CardBase card, ICardSpace? from, ISkill skill)
            => scope.board.Gestures.Publish(new SkillActivated(card, from, skill));

        public IReadOnlyList<CardCommand> CommandsFor(CardBase card, ICardSpace? from)
            => scope.IsDisposed || scope.commands is null ? [] : scope.commands(card, from).ToList();

        public void Invoke(CardBase card, ICardSpace? from, CardCommand command)
            => scope.board.Gestures.Publish(new CommandPressed(card, from, command));

        // The slot announces the switch to and from this gate; what it allows doesn't change while the scope lasts
        public event System.Action? Changed { add {} remove {} }
    }
}
