using Godot;

[Tool]
public abstract partial class CardBaseComponent : Control
{
    TextureRect Front = null!;

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

    public override Variant _GetDragData(Vector2 atPosition)
    {
        if(!Draggable) return base._GetDragData(atPosition);

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

    public abstract CardBaseComponent CreateClone();
}
