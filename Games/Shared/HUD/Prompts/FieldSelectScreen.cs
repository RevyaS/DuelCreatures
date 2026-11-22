using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Setup;
using Godot;

/// <summary>
/// Field selection: the board's play area holding the offered spaces is enlarged into %FieldFocus (the real board,
/// scaled; the far side's cards turned to face the player), and the spaces are picked on it. Above, the asking skill
/// (a ResolvingSkillDetails banner, if the layout has one) and the %Header; below, the %CardRow (only for placing cards)
/// and the %Buttons. Taps on cards that don't answer show their details in the %DetailsSpace.
/// <list type="bullet">
/// <item><see cref="ChooseZones"/>: one space, a tap answers; several, taps number them (tap again to drop), then OK.</item>
/// <item><see cref="Assign{TCard, TSpace}"/>: ArC's Assigning question. The row holds the cards still to place; a tap on
/// a highlighted space places the leftmost one there, or any row card is dragged onto a space it may go to. A placed card
/// shows on its space (a preview: nothing moves until OK) and a tap on it takes it back.</item>
/// </list>
/// Cancel shows when the question may be declined, and answers nothing. Give it the board with <see cref="Attach"/>.
/// </summary>
public partial class FieldSelectScreen : InputScreen
{
    /// <summary>The region shown on a space while a row card is dragged over it.</summary>
    [Export] public string PlaceText { get; set; } = "Place";
    /// <summary>
    /// A single pick is numbered instead of answering: a tap on another space moves it, OK answers (e.g. a trigger's
    /// unit, so its would-be numbers can be read first). Off: the tap answers.
    /// </summary>
    [Export] public bool ConfirmSinglePick { get; set; }

    /// <summary>The spaces picked so far changed (in picking order), e.g. to preview what they'd do.</summary>
    public event Action<IReadOnlyList<ICardSpace>>? PicksChanged;

    /// <summary>Shows a card's details at the top (a part of the layout was tapped, e.g. a subclass's).</summary>
    protected void ShowDetailsOf(CardBase card) => CardPressed?.Invoke(card);

    PromptHeader header = null!;
    FieldFocus fieldFocus = null!;
    SelectionGrid cardRow = null!;
    PromptButtons buttons = null!;
    DropChoice place = null!;

    InputBoard? input;
    ArcDriver? driver;
    Control? boardRoot;
    List<Control> focusAreas = [];
    List<PlayAreaView> facing = [];

    /// <summary>A card in the row was tapped: show its details.</summary>
    public event Action<CardBase>? CardPressed;

    InputBoard Input => input ?? throw new InvalidOperationException($"{GetPath()} has no board yet ({nameof(Attach)})");
    ArcDriver Driver => driver ?? throw new InvalidOperationException($"{GetPath()} has no board yet ({nameof(Attach)})");
    Control BoardRoot => boardRoot ?? throw new InvalidOperationException($"{GetPath()} has no board yet ({nameof(Attach)})");

    public override void _Ready()
    {
        base._Ready();
        header = GetNode<PromptHeader>("%Header");
        fieldFocus = GetNode<FieldFocus>("%FieldFocus");
        cardRow = GetNode<SelectionGrid>("%CardRow");
        buttons = GetNode<PromptButtons>("%Buttons");
        place = new DropChoice(PlaceText);
        cardRow.CardPressed += card => CardPressed?.Invoke(card.Card);
    }

    /// <summary>The board this screen enlarges: its input, its driver (zone views, play area views) and its root node.</summary>
    public void Attach(InputBoard input, ArcDriver driver, Control boardRoot)
    {
        this.input = input;
        this.driver = driver;
        this.boardRoot = boardRoot;
    }

    #region Choosing spaces
    /// <summary>
    /// Between <paramref name="minimum"/> and <paramref name="maximum"/> of <paramref name="candidates"/>, in the order
    /// picked; nothing when cancelled (<paramref name="canCancel"/>).
    /// </summary>
    public async Task<IReadOnlyList<ICardSpace>> ChooseZones(IReadOnlyList<ICardSpace> candidates, int minimum, int maximum, string hint, bool canCancel)
    {
        await Begin(hint, candidates, withRow: false);
        try
        {
            buttons.CanCancel = canCancel;
            if(minimum == 1 && maximum == 1 && ConfirmSinglePick)
            {
                // One, confirmed: a tap picks (numbered), a tap elsewhere moves the pick, OK answers
                ICardSpace? pick = null;
                buttons.ShowsOk = true;
                while(true)
                {
                    buttons.CanConfirm = pick is not null;
                    using var scope = Input.Scope().Zones(candidates).Numbered(pick is null ? [] : [ pick ]);
                    var (gesture, button) = await Next(scope);
                    if(button == PromptButton.Cancel) return [];
                    if(button == PromptButton.Ok && pick is not null) return [ pick ];
                    if(gesture is ZoneClicked { Zone: var zone })
                    {
                        pick = ReferenceEquals(pick, zone) ? null : zone;
                        PicksChanged?.Invoke(pick is null ? [] : [ pick ]);
                    }
                }
            }
            if(minimum == 1 && maximum == 1)
            {
                // One: the tap answers
                buttons.ShowsOk = false;
                while(true)
                {
                    using var scope = Input.Scope().Zones(candidates);
                    var (gesture, button) = await Next(scope);
                    if(button == PromptButton.Cancel) return [];
                    if(gesture is ZoneClicked { Zone: var zone }) return [ zone ];
                }
            }

            var chosen = new CardSelection<ICardSpace>(candidates, minimum, maximum);
            buttons.ShowsOk = true;
            buttons.Follow(chosen);
            header.Follow(chosen);
            while(true)
            {
                // Picked spaces can always be tapped again to drop them; the others while there's room
                using var scope = Input.Scope().Zones(chosen.CanPickMore ? candidates : chosen.Picks).Numbered(chosen.Picks);
                var (gesture, button) = await Next(scope);
                switch(button, gesture)
                {
                    case (PromptButton.Ok, _):
                        return chosen.Picks.ToList();
                    case (PromptButton.Cancel, _):
                        return [];
                    case (_, ZoneClicked { Zone: var zone }):
                        chosen.Toggle(zone);
                        PicksChanged?.Invoke(chosen.Picks);
                        break;
                }
            }
        }
        finally
        {
            End();
        }
    }
    #endregion

    #region Placing cards
    /// <summary>The placements answering <paramref name="question"/> (checked by the caller with its Expect); none when cancelled.</summary>
    public async Task<IReadOnlyList<Placement<TCard, TSpace>>> Assign<TCard, TSpace>(Assigning<TCard, TSpace> question)
        where TCard : CardBase
        where TSpace : class, ISingleSpace, ICardDestination<TCard>
    {
        await Begin(question.Prompt, question.Spaces.Cast<ICardSpace>().ToList(), withRow: true);
        var placements = new List<Placement<TCard, TSpace>>();
        try
        {
            buttons.ShowsOk = true;
            buttons.CanCancel = question.CanCancel;
            while(true)
            {
                var unplaced = question.Cards.Where(card => !placements.Any(placed => ReferenceEquals(placed.Card, card))).ToList();
                bool IsFree(TSpace space) => !placements.Any(placed => ReferenceEquals(placed.Space, space));
                bool roomLeft = placements.Count < question.Maximum;
                // A tap on a space places the leftmost card still in the row
                var next = roomLeft ? unplaced.FirstOrDefault() : null;

                header.ShowCount(placements.Count, question.Maximum);
                buttons.CanConfirm = placements.Count >= question.Minimum;
                foreach(var item in cardRow.Show(unplaced)) item.Draggable = roomLeft;

                using var scope = Input.Scope();
                if(next is not null) scope.Zones(question.SpacesFor(next).Where(IsFree));
                foreach(var placed in placements) scope.Cards(placed.Space);
                scope.Numbered(placements.Select(placed => (ICardSpace)placed.Space));
                if(roomLeft)
                {
                    foreach(var space in question.Spaces.Where(IsFree))
                    {
                        scope.Drop(space, card => card is TCard dragged && Refs.Has(unplaced, dragged) && question.CanPlace(dragged, space) ? [ place ] : []);
                    }
                }

                var (gesture, button) = await Next(scope);
                switch(button, gesture)
                {
                    case (PromptButton.Ok, _):
                        return placements;
                    case (PromptButton.Cancel, _):
                        return [];
                    case (_, ZoneClicked { Zone: TSpace space }) when next is not null:
                        Place(placements, next, space);
                        break;
                    case (_, CardDropped { Card: TCard card, To: TSpace space }):
                        Place(placements, card, space);
                        break;
                    case (_, CardClicked { Zone: var zone }):
                        placements.RemoveAll(placed => ReferenceEquals(placed.Space, zone));
                        Driver.ViewOf(zone).Resync();
                        break;
                }
            }
        }
        finally
        {
            // The previews go: the board shows the game again (the placements arrive as moves once answered)
            foreach(var space in question.Spaces) Driver.ViewOf(space).Resync();
            End();
        }
    }

    // Shows card on space, until the board is resynced
    private void Place<TCard, TSpace>(List<Placement<TCard, TSpace>> placements, TCard card, TSpace space)
        where TCard : CardBase
        where TSpace : class, ISingleSpace, ICardDestination<TCard>
    {
        placements.Add(new Placement<TCard, TSpace>(card, space));
        var view = Driver.ViewOf(space) as CardArea
            ?? throw new NotSupportedException($"{((Node)Driver.ViewOf(space)).Name} can't preview a placed card: show its zone with a {nameof(CardArea)}");
        view.ClearCard();
        var node = CardScenes.Create(card);
        node.IsFront = true;
        view.AddCard(node);
    }
    #endregion

    #region Screen
    // Opens the screen and enlarges the play areas holding spaces (the whole board for spaces outside them)
    private async Task Begin(string hint, IReadOnlyList<ICardSpace> spaces, bool withRow)
    {
        Open(hint);
        header.Hint = hint;
        header.HideCount();
        buttons.Unfollow();
        cardRow.Clear();
        cardRow.Visible = withRow;

        focusAreas = [];
        foreach(var space in spaces)
        {
            var area = Driver.AreaHolding((Node)Driver.ViewOf(space));
            var view = area is null ? BoardRoot : Driver.AreaViews[area];
            if(!focusAreas.Contains(view)) focusAreas.Add(view);
        }
        if(focusAreas.Contains(BoardRoot)) focusAreas = [ BoardRoot ];
        facing = focusAreas.OfType<PlayAreaView>().ToList();

        // Laid out first: the focus rect is where the screen's layout puts it
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        ApplyFocus();
    }

    private void End()
    {
        ReleaseFocus();
        header.HideCount();
        buttons.Unfollow();
        cardRow.Clear();
        Close();
    }

    private void ApplyFocus()
    {
        if(IsCollapsed || focusAreas.Count == 0) return;
        foreach(var area in facing) area.FaceViewer(true);
        fieldFocus.Focus(BoardRoot, focusAreas);
    }

    private void ReleaseFocus()
    {
        fieldFocus.Release();
        foreach(var area in facing) area.FaceViewer(false);
    }

    // Collapsed, the board shows as it is; back up, it's enlarged again once the layer is back in place
    protected override async void OnCollapsedChanged(bool collapsed)
    {
        if(!ReferenceEquals(Current, this)) return;
        if(collapsed)
        {
            ReleaseFocus();
            return;
        }
        while(Layer is { } layer && !IsCollapsed && !Mathf.IsZeroApprox(layer.Position.Y))
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        if(ReferenceEquals(Current, this) && !IsCollapsed) ApplyFocus();
    }

    // The next gesture the scope allows, or a press of OK / Cancel, whichever comes first
    private async Task<(IGesture? Gesture, PromptButton? Button)> Next(InputScope scope)
    {
        var gesture = scope.Next();
        var button = buttons.NextPress();
        var first = await Task.WhenAny(gesture, button);
        return first == button ? (null, await button) : (await gesture, null);
    }
    #endregion
}
