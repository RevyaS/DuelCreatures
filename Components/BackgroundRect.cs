using Godot;

[Tool]
public partial class BackgroundRect : MarginContainer
{
    ColorRect ColorRect = null!;
    TextureRect TextureRect = null!;

    private Color _color = Colors.White;
    [Export]
    public Color Color
    {
        get => _color;
        set
        {
            _color = value;
            Render();
        }
    }

    private Texture2D _texture = null!;
    [Export]
    public Texture2D Texture
    {
        get => _texture;
        set
        {
            _texture = value;
            Render();
        }
    }

    private bool _textureMode = false;
    [Export]
    public bool TextureMode
    {
        get => _textureMode;
        set
        {
            _textureMode = value;
            Render();
        }
    }



    private void Render()
    {
        if(!IsInsideTree()) return;
        ColorRect.Modulate = Color;
        TextureRect.Texture = Texture;
        ColorRect.Visible = !TextureMode;
        TextureRect.Visible = TextureMode;
    }

    public override void _Ready()
    {
        ColorRect = GetNode<ColorRect>($"%{nameof(ColorRect)}");
        TextureRect = GetNode<TextureRect>($"%{nameof(TextureRect)}");
    }
}
