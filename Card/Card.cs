using Godot;

[Tool]
public partial class Card : Control
{
    ColorRect Back;
    TextureRect Front;

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

    private bool _isFront;
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
        Back = GetNode<ColorRect>($"%{nameof(Back)}");
        Front = GetNode<TextureRect>($"%{nameof(Front)}");
    }

    private void Render()
    {
        if(!IsInsideTree()) return;
        Back.Visible = !_isFront;
        Front.Visible = _isFront;
        Front.Texture = texture;
    }
}
