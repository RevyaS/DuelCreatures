using System;
using System.Collections.Generic;
using System.Linq;
using ArC.CardGames.Components;
using Godot;

/// <summary>
/// What input scopes work on: the board's zone views (through its ArcDriver), the HUD's buttons, hint and aim line,
/// the card details' gate, and the gestures the player makes. One per player input provider.
/// </summary>
public sealed class InputBoard
{
    readonly Dictionary<BoardButton, BoardButtonView> buttons = [];
    readonly List<InputScope> scopes = [];
    ArcDriver? driver;

    public InputBoard(ActivationGateSlot activations, IEnumerable<BoardButtonView> buttonViews, InputHintLabel? hint = null, AimIndicatorLayer? aimLine = null)
    {
        Activations = activations;
        Hint = hint;
        AimLine = aimLine;
        if(hint is not null) hint.Text = string.Empty;
        foreach(var view in buttonViews)
        {
            if(!buttons.TryAdd(view.Kind, view)) throw new InvalidOperationException($"Two HUD buttons are {view.Kind} ({buttons[view.Kind].Name} and {view.Name})");
            view.Disabled = true;
            view.Pressed += () => Gestures.Publish(new ButtonPressed(view.Kind));
        }
    }

    public TypedEventBus<IGesture> Gestures { get; } = new();
    public ActivationGateSlot Activations { get; }
    /// <summary>Where scopes say what they ask (InputScope.Hint); none in a HUD without one.</summary>
    public InputHintLabel? Hint { get; }
    /// <summary>Where the line of a drag from a zone is drawn (InputScope.Aim); none in a HUD without one.</summary>
    public AimIndicatorLayer? AimLine { get; }

    /// <summary>Starts taking the gestures on the board <paramref name="driver"/> shows.</summary>
    public void Attach(ArcDriver driver)
    {
        this.driver = driver;
        driver.Clicked += (view, card) =>
        {
            if(view.Space is not { } zone) return;
            if(card is null) Gestures.Publish(new ZoneClicked(zone));
            else Gestures.Publish(new CardClicked(card, zone));
        };
        driver.CardDropped += (view, card, choice) =>
        {
            if(view.Space is { } zone) Gestures.Publish(new CardDropped(card, ZoneShowing(card), zone, choice));
        };
        driver.Swiped += (view, direction) =>
        {
            if(view.Space is { } zone) Gestures.Publish(new ZoneSwiped(zone, direction));
        };
        driver.Aiming += OnAiming;
    }

    public IZoneView ViewOf(ICardSpace zone) => Driver.ViewOf(zone);

    public BoardButtonView Button(BoardButton button) => buttons.TryGetValue(button, out var view)
        ? view
        : throw new InvalidOperationException($"The HUD has no {button} button: add a {nameof(BoardButtonView)} with Kind = {button}");

    public InputScope Scope() => new(this);

    ArcDriver Driver => driver ?? throw new InvalidOperationException($"{nameof(InputBoard)} has no board yet ({nameof(Attach)})");

    // The innermost open scope decides what a gesture in progress (an aim) may do
    InputScope? Current => scopes.Count == 0 ? null : scopes[^1];
    internal void Enter(InputScope scope) => scopes.Add(scope);
    internal void Leave(InputScope scope) => scopes.Remove(scope);

    // The zone a dragged card is still shown in (where it was dragged from)
    private ICardSpace? ZoneShowing(CardBase card) => Driver.Views.FirstOrDefault(view => view.FindCardNode(card) is not null)?.Space;

    #region Aiming
    // A drag from a zone: a line from it to the pointer, snapping to a target the current scope accepts; let go on one, it's an Aimed
    private void OnAiming(IZoneView from, AimStep step, Vector2 pointer)
    {
        if(from.Space is not { } fromZone || Current is not { } scope || !scope.CanAimFrom(fromZone))
        {
            AimLine?.HideLine();
            return;
        }

        var target = TargetAt(fromZone, pointer);
        var accepted = target is not null && scope.CanAim(fromZone, target) ? target : null;
        switch(step)
        {
            case AimStep.Start:
            case AimStep.Move:
                AimLine?.ShowLine(CenterOf((Control)from), accepted is null ? pointer : CenterOf(ControlOf(accepted)));
                break;
            case AimStep.End:
                AimLine?.HideLine();
                if(accepted is not null) Gestures.Publish(new Aimed(fromZone, accepted));
                break;
            case AimStep.Cancel:
                AimLine?.HideLine();
                break;
        }
    }

    // A zone under the pointer first, else a play area
    private AimTarget? TargetAt(ICardSpace from, Vector2 pointer)
    {
        var zone = Driver.Views
            .Where(view => view.Space is not null && !ReferenceEquals(view.Space, from) && UnderPointer.Contains((Control)view, pointer))
            .Select(view => view.Space!)
            .FirstOrDefault();
        if(zone is not null) return new ZoneTarget(zone);

        var area = Driver.AreaViews.FirstOrDefault(entry => UnderPointer.Contains(entry.Value, pointer)).Key;
        return area is null ? null : new AreaTarget(area);
    }

    private Control ControlOf(AimTarget target) => target switch
    {
        ZoneTarget zone => (Control)ViewOf(zone.Zone),
        AreaTarget area => Driver.AreaViews[area.Area],
        _ => throw new NotSupportedException($"Unknown aim target {target.GetType().Name}"),
    };

    private static Vector2 CenterOf(Control control) => control.GetGlobalTransform() * (control.Size / 2);
    #endregion
}
