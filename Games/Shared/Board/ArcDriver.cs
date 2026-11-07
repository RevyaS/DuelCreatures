using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Flow;
using ArC.CardGames.Predefined.Common;
using Godot;

/// <summary>A view matched to a zone by name that isn't a zone view (IZoneView), so nothing would show the zone.</summary>
public sealed record NotAZoneView(string Path, string Name, Type ViewType) : BindingProblem(Path, Name)
{
    public override string Describe() => $"the {ViewType.Name} matches a zone but isn't a zone view; use a zone scene (CardSlot, CardBox, LineZone, HandZone, StackZone)";
}

/// <summary>A zone bound to two views.</summary>
public sealed record ZoneBoundTwice(string Path, string Name, Type ZoneType) : BindingProblem(Path, Name)
{
    public override string Describe() => $"the {ZoneType.Name} is already shown by another view";
}

/// <summary>
/// Shows an ArC game on a board scene. Binds the board's zone views to the game's zones by node name (plus the few
/// bound in code), then turns the game's CardsMoved, CardTurned and deck shuffles into zone changes, presented in
/// order. A board script only does:
/// <code>
/// var driver = new ArcDriver(session.EventBus, presenter, sfx);
/// driver.Bind(PlayerHand, board.Player1Area.Hand, BoardSide.Player);   // views outside the naming convention
/// driver.BindBoard(this, board, area => [ area.PendingMaterials ]);   // everything else, by name; hidden zones listed
/// driver.Animate&lt;Called&gt;(MyMoves.ToCircle).Sound&lt;Called&gt;(MySfx.Call);  // how moves look and sound, by reason
/// driver.Start();                                                      // fails listing every binding problem
/// </code>
/// Naming: the board scene's unique nodes Player1Area and Player2Area are the play areas (their scene instances hold
/// one unique zone node per zone property, e.g. %MonsterZone1); zones both play areas share are unique nodes of the
/// board scene itself. Nodes under a zone view that bind its zone (IBindable) are bound to it too (its extensions).
/// In debug builds, each time the game settles, every view is checked against its zone once the changes so far are
/// presented, and a mismatch is reported as an error.
/// </summary>
public sealed class ArcDriver : IDisposable
{
    sealed record Shown(IZoneView View, BoardSide Side);

    readonly EventBus eventBus;
    readonly Presenter presenter;
    readonly SfxBus sfx;
    readonly Dictionary<ICardSpace, Shown> shown = new(ReferenceEqualityComparer.Instance);
    readonly HashSet<Node> boundInCode = [];
    readonly Dictionary<Type, CardMove> moveAnimations = [];
    readonly Dictionary<Type, SfxCue> moveSounds = [];
    readonly Dictionary<BoardSide, PresentationContext> contexts = [];
    readonly BindingReport report = new();
    readonly List<IDisposable> subscriptions = [];
    bool started;
    bool boardBound;

    public ArcDriver(EventBus eventBus, Presenter presenter, SfxBus sfx)
    {
        this.eventBus = eventBus;
        this.presenter = presenter;
        this.sfx = sfx;
    }

    /// <summary>A zone view asked to show its whole zone (an Inspectable LineZone or StackZone was pressed).</summary>
    public event Action<IZoneView>? ZoneInspected;

    /// <summary>A card on the board its viewer may see was clicked, to see its details: face-up, or their own Set cards.</summary>
    public event Action<IZoneView, CardBase>? CardInspected;

    /// <summary>Any zone or card on the board was clicked (the card, or null for the zone itself), for input scopes.</summary>
    public event Action<IZoneView, CardBase?>? Clicked;
    /// <summary>A card was dropped on a zone (IZoneView.OfferDrop), for input scopes.</summary>
    public event Action<IZoneView, CardBase, DropChoice>? CardDropped;
    /// <summary>A zone was swiped (IZoneView.SetGestures), for input scopes.</summary>
    public event Action<IZoneView, GestureDetector.SwipeDirection>? Swiped;
    /// <summary>A drag from a zone moved (IZoneView.SetGestures), for input scopes.</summary>
    public event Action<IZoneView, AimStep, Vector2>? Aiming;

    readonly Dictionary<PlayAreaBase, Control> areaViews = new(ReferenceEqualityComparer.Instance);
    /// <summary>The node showing each play area (its scene instance on the board), e.g. to aim at a play area itself.</summary>
    public IReadOnlyDictionary<PlayAreaBase, Control> AreaViews => areaViews;

    /// <summary>The views shown, by zone.</summary>
    public IEnumerable<IZoneView> Views => shown.Values.Select(entry => entry.View);

    /// <summary>The view showing <paramref name="zone"/>; fails when no view shows it.</summary>
    public IZoneView ViewOf(ICardSpace zone) => shown.TryGetValue(zone, out var entry)
        ? entry.View
        : throw new InvalidOperationException($"No view shows this {zone.GetType().Name}");

    public PresentationContext ContextFor(BoardSide side)
    {
        if(!contexts.TryGetValue(side, out var context)) contexts[side] = context = new(presenter, sfx, side);
        return context;
    }

    #region Binding
    /// <summary>
    /// Binds a view the naming convention can't reach (e.g. a hand placed outside its play area's scene). Call before
    /// <see cref="BindBoard"/>, which then leaves the zone and the view out.
    /// </summary>
    public void Bind<TView, TZone>(TView view, TZone zone, BoardSide side)
        where TView : Node, IZoneView, IBindable<TZone>
        where TZone : ICardSpace
    {
        if(boardBound) throw new InvalidOperationException($"Bind {view.Name} before {nameof(BindBoard)}, so the board leaves it out");
        boundInCode.Add(view);
        Register(view, zone, side, view.GetParent()?.Name ?? "", view.Name);
    }

    /// <summary>
    /// Binds the board scene <paramref name="boardScene"/> to <paramref name="board"/> by name (see the class summary).
    /// <paramref name="hidden"/> lists each play area's zones nothing shows (e.g. cards waiting to become XYZ
    /// materials); every other zone needs a view.
    /// </summary>
    public void BindBoard<TPlayArea>(Node boardScene, TwoPlayerBoard<TPlayArea> board, Func<TPlayArea, IEnumerable<ICardSpace>>? hidden = null)
        where TPlayArea : PlayAreaBase
    {
        if(boardBound) throw new InvalidOperationException($"{nameof(BindBoard)} was already called");
        boardBound = true;

        var boardPath = boardScene.Name.ToString();
        var boardNodes = UniqueNodes.Of(boardScene);
        var boundZones = new HashSet<ICardSpace>(shown.Keys, ReferenceEqualityComparer.Instance);

        // Zones both play areas share: unique nodes of the board scene itself
        var sharedZones = PropertiesOf<ICardSpace>(board).Where(zone => !boundZones.Contains(zone.Value)).ToDictionary();
        var boardViews = ZoneCandidates(boardScene);
        foreach(var (name, zone, view) in Bindings.BindByName(sharedZones, boardViews, boardPath, report))
        {
            Register(view, zone, BoardSide.Both, boardPath, name, alreadyBound: true);
            boundZones.Add(zone);
        }

        foreach(var (areaName, area) in PropertiesOf<TPlayArea>(board))
        {
            var side = ReferenceEquals(area, board.Player1Area) ? BoardSide.Player : BoardSide.Opponent;
            if(!boardNodes.TryGetValue(areaName, out var areaNode))
            {
                report.Add(new MissingView(boardPath, areaName, area.GetType()));
                continue;
            }
            if(areaNode is Control areaControl) areaViews[area] = areaControl;
            // A play area's own script may show the play area itself (e.g. its life points)
            if(Bindings.IsBindable(areaNode)) Bindings.TryBind(areaNode, area, boardPath, areaName, report);

            var hiddenZones = new HashSet<ICardSpace>(hidden?.Invoke(area) ?? [], ReferenceEqualityComparer.Instance);
            var zones = area.ZonesByName
                .Where(zone => !boundZones.Contains(zone.Value) && !hiddenZones.Contains(zone.Value))
                .ToDictionary();
            var views = ZoneCandidates(areaNode);
            var areaPath = $"{boardPath}/{areaName}";
            foreach(var (name, zone, view) in Bindings.BindByName(zones, views, areaPath, report))
            {
                Register(view, zone, side, areaPath, name, alreadyBound: true);
            }
        }
    }

    /// <summary>
    /// Binds every node under <paramref name="root"/> that shows a <typeparamref name="TModel"/> (IBindable of it or
    /// of a base type), e.g. a phase indicator showing any game's session.
    /// </summary>
    public void BindAll<TModel>(Node root, TModel model) where TModel : notnull
    {
        foreach(var node in Descendants(root).Where(node => Bindings.ModelTypesOf(node.GetType()).Any(type => type.IsInstanceOfType(model))))
        {
            Bindings.TryBind(node, model, root.Name, node.Name, report);
        }
    }

    private void Register(Node view, ICardSpace zone, BoardSide side, string path, string name, bool alreadyBound = false)
    {
        if(view is not IZoneView zoneView)
        {
            report.Add(new NotAZoneView(path, name, view.GetType()));
            return;
        }
        if(shown.ContainsKey(zone))
        {
            report.Add(new ZoneBoundTwice(path, name, zone.GetType()));
            return;
        }
        if(!alreadyBound && !Bindings.TryBind(view, zone, path, name, report)) return;
        shown[zone] = new(zoneView, side);

        if(view is LineZone line) line.InspectRequested += OnInspectRequested;
        // A pile hiding its cards (a deck, an Extra Deck) only lists them to its owner
        if(view is StackZone stack) stack.InspectRequested += inspected =>
        {
            if(!stack.HideCards || side == BoardSide.Player) OnInspectRequested(inspected);
        };
        zoneView.Clicked += (clicked, card) => OnClicked(clicked, card, side);
        zoneView.CardDropped += (target, card, choice) => CardDropped?.Invoke(target, card, choice);
        zoneView.Swiped += (swiped, direction) => Swiped?.Invoke(swiped, direction);
        zoneView.Aiming += (from, step, position) => Aiming?.Invoke(from, step, position);

        // Its extensions: the nodes under it that show its zone
        foreach(var extension in Descendants(view).Where(node => node is not IZoneView && Bindings.IsBindable(node)))
        {
            Bindings.TryBind(extension, zone, $"{path}/{name}", extension.Name, report);
        }
    }

    private void OnInspectRequested(IZoneView view) => ZoneInspected?.Invoke(view);

    private void OnClicked(IZoneView view, CardBase? card, BoardSide side)
    {
        Clicked?.Invoke(view, card);
        // Zones that list their cards when pressed show the list instead
        if(card is null || view is LineZone { Inspectable: true } or StackZone { Inspectable: true }) return;
        if(MaySee(view, card, side)) CardInspected?.Invoke(view, card);
    }

    // A face-up card, or the player's own card outside a hidden pile or hand (e.g. their Set cards)
    private static bool MaySee(IZoneView view, CardBase card, BoardSide side)
    {
        bool faceUp = view.Displayed.Cards.Any(shown => ReferenceEquals(shown.Card, card) && shown.IsFaceUp);
        bool hidden = view is StackZone { HideCards: true } or HandZone { HideCards: true };
        return faceUp || (side == BoardSide.Player && !hidden);
    }

    // The unique nodes of a scene that are meant to show a zone: zone views, and nodes binding a kind of zone (which
    // are then reported, not being zone views). Others, like a label showing life points, aren't matched to zones.
    private Dictionary<string, Node> ZoneCandidates(Node scene) => UniqueNodes.BindableOf(scene)
        .Where(view => !boundInCode.Contains(view.Value) && ShowsZones(view.Value))
        .ToDictionary();

    private static bool ShowsZones(Node node)
        => node is IZoneView || Bindings.ModelTypesOf(node.GetType()).Any(type => typeof(ICardSpace).IsAssignableFrom(type));

    // The public properties of `model` holding a T, by property name
    private static Dictionary<string, T> PropertiesOf<T>(object model) where T : class => model.GetType()
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(property => property.CanRead && property.GetIndexParameters().Length == 0 && typeof(T).IsAssignableFrom(property.PropertyType))
        .Select(property => (property.Name, Value: property.GetValue(model) as T))
        .Where(found => found.Value is not null)
        .ToDictionary(found => found.Name, found => found.Value!);

    private static IEnumerable<Node> Descendants(Node node)
    {
        foreach(var child in node.GetChildren())
        {
            yield return child;
            foreach(var descendant in Descendants(child)) yield return descendant;
        }
    }
    #endregion

    #region Moves
    /// <summary>
    /// Animates moves for <typeparamref name="TReason"/> (or a reason deriving from it) as <paramref name="move"/>, for
    /// its timing (PresentationConfig). The most specific registered reason type wins.
    /// </summary>
    public ArcDriver Animate<TReason>(CardMove move) where TReason : IMoveReason
    {
        moveAnimations[typeof(TReason)] = move;
        return this;
    }

    /// <summary>
    /// Plays <paramref name="cue"/> when cards moved for <typeparamref name="TReason"/> (or a reason deriving from it)
    /// have arrived, e.g. a ride's sound once the new vanguard is on its circle. The most specific registered reason wins.
    /// </summary>
    public ArcDriver Sound<TReason>(SfxCue cue) where TReason : IMoveReason
    {
        moveSounds[typeof(TReason)] = cue;
        return this;
    }

    // The reason's own type, then its base types, then its interfaces
    private static bool TryFind<T>(Dictionary<Type, T> byReason, IMoveReason reason, out T found)
    {
        for(var type = reason.GetType(); type is not null; type = type.BaseType)
        {
            if(byReason.TryGetValue(type, out found!)) return true;
        }
        foreach(var type in reason.GetType().GetInterfaces())
        {
            if(byReason.TryGetValue(type, out found!)) return true;
        }
        found = default!;
        return false;
    }

    private CardMove MoveFor(IMoveReason reason, ICardSpace to)
    {
        if(TryFind(moveAnimations, reason, out var move)) return move;
        return to switch
        {
            Hand => CardMoves.ToHand,
            Deck => CardMoves.ToDeck,
            _ => DefaultMove,
        };
    }

    public static readonly CardMove DefaultMove = new(nameof(DefaultMove));
    #endregion

    #region Running
    /// <summary>Starts showing the game. Fails, listing every binding problem, when the board doesn't match the game.</summary>
    public void Start()
    {
        if(started) throw new InvalidOperationException($"{nameof(ArcDriver)} was already started");
        report.ThrowIfAny();
        started = true;

        subscriptions.Add(eventBus.Subscribe<CardsMoved>(OnCardsMoved));
        subscriptions.Add(eventBus.Subscribe<CardTurned>(OnCardTurned));
        eventBus.DeckShuffled += OnDeckShuffled;
        subscriptions.Add(new Disposable(() => eventBus.DeckShuffled -= OnDeckShuffled));
        if(OS.IsDebugBuild())
        {
            eventBus.Settled += OnSettled;
            subscriptions.Add(new Disposable(() => eventBus.Settled -= OnSettled));
        }
    }

    // One ArC move: the zone it left and the zone it reached each get their change, with their content as of now
    private void OnCardsMoved(CardsMoved moved)
    {
        var from = shown.GetValueOrDefault(moved.From);
        var to = shown.GetValueOrDefault(moved.To);
        if(from is not null) Apply(from, new ZoneCardsLeft(moved.Cards, to?.View, ZoneSnapshot.Of(moved.From)));
        if(to is not null) Apply(to, new ZoneCardsArrived(moved.Cards, from?.View, MoveFor(moved.Reason, moved.To), ZoneSnapshot.Of(moved.To)));
        if(TryFind(moveSounds, moved.Reason, out var cue))
        {
            var context = ContextFor(to?.Side ?? from?.Side ?? BoardSide.Both);
            context.Present(() => context.PlaySfx(cue));
        }
    }

    private void OnCardTurned(CardTurned turned)
    {
        if(shown.GetValueOrDefault(turned.Space) is { } entry)
        {
            Apply(entry, new ZoneCardTurned(turned.Card, ZoneSnapshot.Of(turned.Space)));
        }
    }

    private void OnDeckShuffled(Deck deck)
    {
        if(shown.GetValueOrDefault(deck) is { } entry)
        {
            Apply(entry, new ZoneShuffled(ZoneSnapshot.Of(deck)));
        }
    }

    // Runs inside the game's event: a view that can't show a change reports it loudly, but must never stop the game
    // (an exception here would unwind into ArC's move)
    private void Apply(Shown entry, ZoneChange change)
    {
        try
        {
            entry.View.Apply(change, ContextFor(entry.Side));
        }
        catch(Exception e)
        {
            GD.PushError($"{nameof(ArcDriver)}: {ZoneDesync.NameOf(entry.View)} couldn't show a {change.GetType().Name}; the game goes on. {e.Message}");
        }
    }

    // The game settled: what every zone holds now must be what the views show once the changes so far are presented
    private Task OnSettled()
    {
        var expected = shown.Values.Select(entry => (entry.View, Zone: ZoneSnapshot.Of(entry.View.Space!))).ToList();
        presenter.Enqueue(BoardSide.Both, () =>
        {
            foreach(var (view, zone) in expected)
            {
                if(ZoneDesync.Describe(view, zone) is { } mismatch)
                {
                    GD.PushError($"{nameof(ArcDriver)}: {ZoneDesync.NameOf(view)} doesn't match its zone: {mismatch}");
                }
            }
        });
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        foreach(var subscription in subscriptions) subscription.Dispose();
        subscriptions.Clear();
    }
    #endregion
}
