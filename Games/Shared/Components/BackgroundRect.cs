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

    private int _marginLeft = 0;
    [Export]
    public int MarginLeft
    {
        get => _marginLeft;
        set
        {
            _marginLeft = value;
            Render();
        }
    }

    private int _marginRight = 0;
    [Export]
    public int MarginRight
    {
        get => _marginRight;
        set
        {
            _marginRight = value;
            Render();
        }
    }

    private int _marginTop = 0;
    [Export]
    public int MarginTop
    {
        get => _marginTop;
        set
        {
            _marginTop = value;
            Render();
        }
    }

    private int _marginBottom = 0;
    [Export]
    public int MarginBottom
    {
        get => _marginBottom;
        set
        {
            _marginBottom = value;
            Render();
        }
    }

    private void Render()
    {
        if(!IsNodeReady()) return;
        MarginContainer.AddThemeConstantOverride("margin_left", _marginLeft);
        MarginContainer.AddThemeConstantOverride("margin_right", _marginRight);
        MarginContainer.AddThemeConstantOverride("margin_top", _marginTop);
        MarginContainer.AddThemeConstantOverride("margin_bottom", _marginBottom);
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
        Render();
    }
}
