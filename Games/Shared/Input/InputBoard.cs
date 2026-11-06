using System;
using System.Collections.Generic;
using System.Linq;
using ArC.CardGames.Components;
using Godot;

/// <summary>
/// What input scopes work on: the board's zone views (through its ArcDriver), the HUD's buttons, the card details'
/// activation gate, and the gestures the player makes. One per player input provider.
/// </summary>
public sealed class InputBoard
{
    readonly Dictionary<BoardButton, BoardButtonView> buttons = [];
    ArcDriver? driver;

    public InputBoard(ActivationGateSlot activations, IEnumerable<BoardButtonView> buttonViews, InputHintLabel? hint = null)
    {
        Activations = activations;
        Hint = hint;
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

    /// <summary>Starts taking clicks from the board <paramref name="driver"/> shows.</summary>
    public void Attach(ArcDriver driver)
    {
        this.driver = driver;
        driver.Clicked += (view, card) =>
        {
            if(view.Space is not { } zone) return;
            if(card is null) Gestures.Publish(new ZoneClicked(zone));
            else Gestures.Publish(new CardClicked(card, zone));
        };
    }

    public IZoneView ViewOf(ICardSpace zone) => (driver ?? throw new InvalidOperationException($"{nameof(InputBoard)} has no board yet ({nameof(Attach)})")).ViewOf(zone);

    public BoardButtonView Button(BoardButton button) => buttons.TryGetValue(button, out var view)
        ? view
        : throw new InvalidOperationException($"The HUD has no {button} button: add a {nameof(BoardButtonView)} with Kind = {button}");

    public InputScope Scope() => new(this);
}
