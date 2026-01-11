using System;
using DuelCreatures.Data;
using Godot;

[Tool]
public abstract partial class CardBaseComponent : Control
{
    private double LONG_PRESS_TRESHOLD = 0.5;
    TextureRect Front = null!;
    private bool _pressed = false;
    private double _pressTimeElapsed = 0;
    BackgroundRect Back = null!;

    private bool _currentlyDragged = false;
    public bool CurrentlyDragged { 
        get => _currentlyDragged;
        set
        {
            _currentlyDragged = value;
            Visible = !_currentlyDragged;
        }
    }

    public bool Draggable { get; set; } = false;

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

    public Vector2 EffectiveSize => CustomMinimumSize * Scale;

    public override void _Ready()
    {
        CustomMinimumSize = SizeConstants.CardDefaultSize;
        Front = GetNode<TextureRect>($"%{nameof(Front)}");
        Back = GetNode<BackgroundRect>($"%{nameof(Back)}");
        Render();
    }

    protected void Render()
    {
        if(!IsInsideTree()) return;
        RenderCore();
    }

    protected virtual void RenderCore()
    {
        Front.Visible = _isFront;
        Front.Texture = texture;

        SetSleeveInfo(_sleeveInfo);
    }

    public override Variant _GetDragData(Vector2 atPosition)
    {
        if(!Draggable) return base._GetDragData(atPosition);

        SetDragPreview(CreateClone());
        CurrentlyDragged = true;
        CardDragging?.Invoke(this);
        return this;
    }

    public override void _Notification(int what)
    {
        if(what == NotificationDragEnd && CurrentlyDragged)
        {
            if(!IsDragSuccessful())
            {
                CurrentlyDragged = false;
                CardDragCancelled?.Invoke(this);
            }
        }
    }

    public override void _GuiInput(InputEvent e)
    {
        // Mouse click or touch
        if (e is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
        {
            if(mb.Pressed && !_pressed)
            {
                _pressed = true;
                _pressTimeElapsed = 0;
            }
            else 
            {
                HandleRelease();
            }
        }
    }

    public override void _Process(double delta)
    {
        if (_pressed)
        {
            _pressTimeElapsed += delta;

            if (_pressTimeElapsed >= LONG_PRESS_TRESHOLD)
            {
                _pressed = false;
                OnLongPress();
            }
        }
    }

    protected virtual void OnLongPress()
    {
        
    }

    private void HandleRelease()
    {
        if(!_pressed) return;

        _pressed = false;
        OnPressed();
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
