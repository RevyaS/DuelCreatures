using System;
using ArC.CardGames.Vanguard;
using DuelCreatures.Data;
using Godot;

/// <summary>
/// Presents a base <see cref="Card"/> at a scale, for screens outside the game (deck builder, card lists).
/// Besides press and long press it reports vertical flicks as swipes. A slower drag scrolls the
/// <see cref="ScrollContainer"/> it is in, so a list of cards can still be scrolled by dragging on them
/// (all through its <see cref="GestureDetector"/>).
/// </summary>
public partial class CardView : Control
{
    Card card = null!;
    PanelContainer BadgePanel = null!;
    Label Badge = null!;

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

        card = CardScenes.CreateBase(vanguardCard);
        // The card scene is centered on its parent and scales around its center
        card.Scale = new Vector2(scale, scale);
        AddChild(card);
        // This view takes every press; the card only shows
        IgnoreMouse(card);

        var gestures = new GestureDetector { Swipes = true };
        gestures.Tapped += () => Pressed?.Invoke(this);
        gestures.LongPressed += () => LongPressed?.Invoke(this);
        gestures.Swiped += direction =>
        {
            if(direction == GestureDetector.SwipeDirection.Up) SwipedUp?.Invoke(this);
            else if(direction == GestureDetector.SwipeDirection.Down) SwipedDown?.Invoke(this);
        };
        AddChild(gestures);

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

    private static void IgnoreMouse(Node node)
    {
        if(node is Control control) control.MouseFilter = MouseFilterEnum.Ignore;
        foreach(var child in node.GetChildren())
        {
            IgnoreMouse(child);
        }
    }
}
