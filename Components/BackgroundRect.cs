using Godot;

[Tool]
public partial class BackgroundRect : MarginContainer
{
    ColorRect ColorRect = null!;
    TextureRect TextureRect = null!;
    MarginContainer MarginContainer = null!;

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

    [Export]
    public int MarginLeft
    {
        get => MarginContainer.GetThemeConstant("margin_left");
        set
        {
            MarginContainer.AddThemeConstantOverride("margin_left", value);
        }
    }

    [Export]
    public int MarginRight
    {
        get => MarginContainer.GetThemeConstant("margin_right");
        set
        {
            MarginContainer.AddThemeConstantOverride("margin_right", value);
        }
    }

    [Export]
    public int MarginTop
    {
        get => MarginContainer.GetThemeConstant("margin_top");
        set
        {
            MarginContainer.AddThemeConstantOverride("margin_top", value);
        }
    }

    [Export]
    public int MarginBottom
    {
        get => MarginContainer.GetThemeConstant("margin_bottom");
        set
        {
            MarginContainer.AddThemeConstantOverride("margin_bottom", value);
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
        MarginContainer = GetNode<MarginContainer>($"%{nameof(MarginContainer)}");
    }
}
