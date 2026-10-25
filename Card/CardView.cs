using System;
using ArC.CardGames.Predefined.Vanguard;
using DuelCreatures.Data;
using Godot;

/// <summary>
/// Presents a base <see cref="Card"/> at a scale, for screens outside the game (deck builder, card lists).
/// Besides press and long press it reports vertical flicks as swipes. A slower drag scrolls the
/// <see cref="ScrollContainer"/> it is in, so a list of cards can still be scrolled by dragging on them.
/// </summary>
public partial class CardView : Control
{
    const float SwipeDistance = 60;
    const double SwipeMaxTime = 0.35;
    const float DragTolerance = 10;

    Card card = null!;
    PanelContainer BadgePanel = null!;
    Label Badge = null!;

    bool tracking;
    bool scrolling;
    double trackedTime;
    Vector2 pressPosition;
    Vector2 pendingScroll;

    public string CardId { get; private set; } = null!;
    public VanguardCard ShownCard { get; private set; } = null!;

    public event Action<CardView>? Pressed;
    public event Action<CardView>? LongPressed;
    public event Action<CardView>? SwipedUp;
    public event Action<CardView>? SwipedDown;

    public static CardView Create(string cardId, VanguardCard card, float scale)
    {
        var view = new CardView();
        view.Initialize(cardId, card, scale);
        return view;
    }

    private void Initialize(string cardId, VanguardCard vanguardCard, float scale)
    {
        CardId = cardId;
        ShownCard = vanguardCard;
        CustomMinimumSize = SizeConstants.CardDefaultSize * scale;

        card = SceneFactory.CreateCard(vanguardCard);
        // Lets the presses the card doesn't use reach this view, for swipes and scrolling
        card.MouseFilter = MouseFilterEnum.Pass;
        // The card scene is centered on its parent and scales around its center
        card.Scale = new Vector2(scale, scale);
        card.CardPressed += _ => Pressed?.Invoke(this);
        card.CardLongPressed += _ => LongPressed?.Invoke(this);
        AddChild(card);

        BadgePanel = new PanelContainer { Visible = false, MouseFilter = MouseFilterEnum.Ignore };
        BadgePanel.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomRight, LayoutPresetMode.KeepSize);
        BadgePanel.GrowHorizontal = GrowDirection.Begin;
        BadgePanel.GrowVertical = GrowDirection.Begin;
        Badge = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        Badge.AddThemeFontSizeOverride("font_size", Mathf.Max(46, (int)(50 * scale)));
        BadgePanel.AddChild(Badge);
        AddChild(BadgePanel);
    }

    /// <summary>A short text over the card's corner, eg; the copies in the deck. Null hides it.</summary>
    public string? BadgeText
    {
        get => BadgePanel.Visible ? Badge.Text : null;
        set
        {
            Badge.Text = value ?? string.Empty;
            BadgePanel.Visible = value is not null;
        }
    }

    /// <summary>Greys the card out, eg; when no copy is left to add.</summary>
    public bool Dimmed
    {
        get => card.Modulate != Colors.White;
        set => card.Modulate = value ? new Color(0.45f, 0.45f, 0.45f) : Colors.White;
    }

    public Color? Outline
    {
        get => card.Outline;
        set => card.Outline = value;
    }

    /// <summary>False shows the card's back, in <see cref="SleeveInfo"/>.</summary>
    public bool IsFront
    {
        get => card.IsFront;
        set => card.IsFront = value;
    }

    public SleeveInfo SleeveInfo
    {
        get => card.SleeveInfo;
        set => card.SleeveInfo = value;
    }

    public override void _GuiInput(InputEvent e)
    {
        if(e is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
        {
            if(mb.Pressed) StartTracking(mb.GlobalPosition);
            else StopTracking(mb.GlobalPosition);
        }
        else if(e is InputEventMouseMotion mm && tracking)
        {
            Track(mm.GlobalPosition, mm.Relative);
        }
    }

    public override void _Process(double delta)
    {
        if(tracking) trackedTime += delta;
    }

    private void StartTracking(Vector2 position)
    {
        tracking = true;
        scrolling = false;
        trackedTime = 0;
        pressPosition = position;
        pendingScroll = Vector2.Zero;
    }

    private void Track(Vector2 position, Vector2 relative)
    {
        if(scrolling)
        {
            Scroll(relative);
            return;
        }

        pendingScroll += relative;
        var moved = position - pressPosition;
        if(moved.Length() < DragTolerance) return;

        // Sideways drags never swipe, and a vertical drag slower than a flick is a scroll
        if(Mathf.Abs(moved.X) > Mathf.Abs(moved.Y) || trackedTime > SwipeMaxTime)
        {
            scrolling = true;
            Scroll(pendingScroll);
        }
    }

    private void StopTracking(Vector2 position)
    {
        if(!tracking) return;
        tracking = false;
        if(scrolling) return;

        var moved = position - pressPosition;
        bool isSwipe = trackedTime <= SwipeMaxTime
            && Mathf.Abs(moved.Y) >= SwipeDistance
            && Mathf.Abs(moved.Y) > Mathf.Abs(moved.X) * 1.5f;
        if(!isSwipe) return;

        if(moved.Y < 0) SwipedUp?.Invoke(this);
        else SwipedDown?.Invoke(this);
    }

    private void Scroll(Vector2 relative)
    {
        for(Node? node = GetParent(); node is not null; node = node.GetParent())
        {
            if(node is ScrollContainer scroll)
            {
                scroll.ScrollHorizontal -= (int)relative.X;
                scroll.ScrollVertical -= (int)relative.Y;
                return;
            }
        }
    }
}
