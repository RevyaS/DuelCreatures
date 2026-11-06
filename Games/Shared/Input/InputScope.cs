using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Rules;

/// <summary>
/// What the player may do for one question, in game terms: which cards and zones they can click, which HUD buttons,
/// which skills the card details may activate. What is allowed is highlighted (and the buttons enabled) while the scope
/// lasts; <see cref="Next"/> waits for an allowed gesture. Disposing it undoes it all.
/// <code>
/// using var scope = Board.Scope()
///     .Cards(PlayArea.Hand, card => card is YuGiOhSpellCard)
///     .Zones(PlayArea.SpellAndTrapZones.Where(zone => zone.IsEmpty))
///     .Button(BoardButton.NextPhase);
/// var gesture = await scope.Next();
/// </code>
/// A click on a card in a zone the scope offers as a zone (but not that card) counts as clicking the zone.
/// </summary>
public sealed class InputScope : InteractionScope
{
    readonly InputBoard board;
    readonly List<(ICardSpace Zone, Func<CardBase, bool> Allowed)> cards = [];
    readonly HashSet<ICardSpace> zones = new(ReferenceEqualityComparer.Instance);
    readonly HashSet<BoardButton> buttons = [];
    readonly HashSet<IZoneView> highlighted = new(ReferenceEqualityComparer.Instance);
    Func<CardBase, ICardSpace?, ISkill, bool>? activatable;

    internal InputScope(InputBoard board)
    {
        this.board = board;
    }

    /// <summary>The cards of <paramref name="zone"/> that <paramref name="allowed"/> accepts (every card when null) can be clicked.</summary>
    public InputScope Cards(ICardSpace zone, Func<CardBase, bool>? allowed = null)
    {
        EnsureOpen();
        cards.Add((zone, allowed ?? (_ => true)));
        Highlight(zone);
        return this;
    }

    /// <summary><paramref name="offered"/> can be clicked as zones (a click on their cards counts too).</summary>
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
        Set(() => view.Disabled, disabled => view.Disabled = disabled, false);
        buttons.Add(button);
        return this;
    }

    /// <summary>Says what is asked, on the HUD's InputHintLabel (if it has one), for as long as the scope lasts.</summary>
    public InputScope Hint(string text)
    {
        EnsureOpen();
        if(board.Hint is { } label) Set(() => label.Text, value => label.Text = value, text);
        return this;
    }

    /// <summary>The card details may activate the skills <paramref name="allowed"/> accepts; pressing one is a SkillActivated gesture.</summary>
    public InputScope Activatable(Func<CardBase, ICardSpace?, ISkill, bool> allowed)
    {
        EnsureOpen();
        activatable = allowed;
        Track(board.Activations.Use(new ScopeGate(this)));
        return this;
    }

    /// <summary>The next gesture this scope allows. Cancelled when the scope is disposed.</summary>
    public async Task<IGesture> Next()
    {
        var gesture = await Next<IGesture, IGesture>(board.Gestures, candidate => Interpret(candidate) is not null);
        return Interpret(gesture)!;
    }

    // The gesture as this scope takes it, or null when it doesn't allow it
    private IGesture? Interpret(IGesture gesture) => gesture switch
    {
        CardClicked click when cards.Any(rule => ReferenceEquals(rule.Zone, click.Zone) && rule.Allowed(click.Card)) => click,
        CardClicked click when zones.Contains(click.Zone) => new ZoneClicked(click.Zone),
        ZoneClicked click when zones.Contains(click.Zone) => click,
        ButtonPressed press when buttons.Contains(press.Button) => press,
        SkillActivated activation when activatable?.Invoke(activation.Card, activation.From, activation.Skill) == true => activation,
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

        // The slot announces the switch to and from this gate; what it allows doesn't change while the scope lasts
        public event Action? Changed { add {} remove {} }
    }
}
