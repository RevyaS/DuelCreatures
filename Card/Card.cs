using System;
using Godot;

[Tool]
public partial class Card : Control
{
    TextureRect Front;

    private bool _currentlyDragged = false;
    public bool CurrentlyDragged { 
        get => _currentlyDragged;
        set
        {
            _currentlyDragged = value;
            Visible = !_currentlyDragged;
        } 
    }

    private Texture2D texture;
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

    public Vector2 EffectiveSize => CustomMinimumSize * Scale;

    public override void _Ready()
    {
        CustomMinimumSize = SizeConstants.CardDefaultSize;
        Front = GetNode<TextureRect>($"%{nameof(Front)}");
        Render();
    }

    private void Render()
    {
        if(!IsInsideTree()) return;
        Front.Visible = _isFront;
        Front.Texture = texture;
    }

    public override void _GuiInput(InputEvent @event)
    {
        if(@event is InputEventMouseButton inputEventMouseButton)
        {
            if(inputEventMouseButton.ButtonIndex == MouseButton.Left && inputEventMouseButton.Pressed)
            {
                CardPressed(this);
            }
        }
    }

    public override Variant _GetDragData(Vector2 atPosition)
    {
        SetDragPreview(CreateClone());
        CurrentlyDragged = true;
        return this;
    }

    public override void _Notification(int what)
    {
        if(what == NotificationDragEnd && CurrentlyDragged)
        {
            if(!IsDragSuccessful())
            {
                CurrentlyDragged = false;
            }
        }
    }

    public virtual Card CreateClone()
    {
        var preview = SceneFactory.CreateCard(Texture, IsFront);
        return preview;
    }

    public event Action<Card> CardPressed;
}
