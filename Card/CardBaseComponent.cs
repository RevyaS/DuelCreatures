using System;
using DuelCreatures.Data;
using Godot;

[Tool]
public abstract partial class CardBaseComponent : Control
{
    TextureRect Front = null!;
    BackgroundRect Back = null!;
    // Press, long press and drag (see GestureDetector)
    GestureDetector? gestures = null;

    private bool _currentlyDragged = false;
    // The card stays where it is but can't be seen while a copy of it is dragged around.
    // Transparent rather than hidden: a hidden control loses the press it's tracking.
    public bool CurrentlyDragged { 
        get => _currentlyDragged;
        set
        {
            _currentlyDragged = value;
            Modulate = _currentlyDragged ? Colors.Transparent : Colors.White;
        }
    }

    private bool _draggable = false;
    public bool Draggable
    {
        get => _draggable;
        set
        {
            _draggable = value;
            if(gestures is not null) gestures.Drags = value;
        }
    }

    private Texture2D texture = null!;
    [Export]
    public Texture2D Texture
    {
        get => texture;
        set
        {
            texture = value;
            Render();
        }
    }

    private bool _isFront = true;
    [Export]
    public bool IsFront
    {
        get => _isFront;
        set
        {
            _isFront = value;
            Render();
        }
    }

    private SleeveInfo _sleeveInfo = null!;
    [Export]
    public SleeveInfo SleeveInfo
    {
        get => _sleeveInfo;
        set
        {
            _sleeveInfo = value;
            Render();
        }
    }

    ColorRect Border = null!;
    Color defaultBorderColor = Colors.Black;

    private Color? _outline = null;
    // Colors the card's border (e.g. to mark intercepting units); null for the normal border
    public Color? Outline
    {
        get => _outline;
        set
        {
            _outline = value;
            Render();
        }
    }

    public Vector2 EffectiveSize => CustomMinimumSize * Scale;

    public override void _Ready()
    {
        CustomMinimumSize = SizeConstants.CardDefaultSize;
        Front = GetNode<TextureRect>($"%{nameof(Front)}");
        Back = GetNode<BackgroundRect>($"%{nameof(Back)}");

        if(!Engine.IsEditorHint())
        {
            // A card dragged out of a list drags in any direction instead of scrolling it
            gestures = new GestureDetector { Drags = _draggable, ScrollsParent = false };
            gestures.Tapped += OnPressed;
            gestures.LongPressed += OnLongPress;
            gestures.DragStarted += position =>
            {
                if(this is Card card) CardDrag.Begin(card, position);
            };
            gestures.DragMoved += CardDrag.Move;
            gestures.DragEnded += CardDrag.End;
            gestures.DragCancelled += CardDrag.Cancel;
            AddChild(gestures);
        }
        Border = GetNode<ColorRect>($"%{nameof(Border)}");
        defaultBorderColor = Border.Color;
        Render();
    }

    protected void Render()
    {
        if(!IsNodeReady()) return;
        RenderCore();
    }

    protected virtual void RenderCore()
    {
        Front.Visible = _isFront;
        Front.Texture = texture;
        Border.Color = _outline ?? defaultBorderColor;

        SetSleeveInfo(_sleeveInfo);
    }

    // The drag session reports through this card's events
    public void NotifyDragStarted() => CardDragging?.Invoke(this);
    public void NotifyDragCancelled() => CardDragCancelled?.Invoke(this);

    protected virtual void OnLongPress()
    {
        
    }

    protected virtual void OnPressed()
    {
    }

    public void SetSleeveInfo(SleeveInfo sleeveInfo)
    {
        if(sleeveInfo is null) return;

        Back.MarginBottom = sleeveInfo.MarginBottom;
        Back.MarginTop = sleeveInfo.MarginTop;
        Back.MarginLeft = sleeveInfo.MarginLeft;
        Back.MarginRight = sleeveInfo.MarginRight;
        switch(sleeveInfo)
        {
            case ColorSleeveInfo colorSleeveInfo:
                Back.Color = colorSleeveInfo.Color;
                Back.TextureMode = false;
                break;
            case TextureSleeveInfo textureSleeveInfo:
                Back.Texture = textureSleeveInfo.Texture;
                Back.TextureMode = true;
                break;
        }
    }

    public abstract CardBaseComponent CreateClone();

    public Action<CardBaseComponent>? CardDragging;
    public Action<CardBaseComponent>? CardDragCancelled;
}
