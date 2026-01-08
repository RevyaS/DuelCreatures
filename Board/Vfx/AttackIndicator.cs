using Godot;
using System;
using System.ComponentModel.DataAnnotations;

[Tool]
public partial class AttackIndicator : Path2D
{
    Sprite2D Sprite2D = null!;
    PathFollow2D PathFollow2D = null!;

    int TextureHeight, TextureWidth;

    [Export]
    public bool Active = false;
    [Export]
    public float Speed = 0.2f; // Progress per second
    
    private float _textureScale = 1f;
    [Export]
    public float TextureScale
    {
        get => _textureScale;
        set
        {
            _textureScale = value;
            Render();
        }
    } // Progress per second

    private void Render()
    {
        if(!IsInsideTree()) return;
        Sprite2D.Scale = new(TextureScale, TextureScale);
    }

    public override void _Ready()
    {
        Sprite2D = GetNode<Sprite2D>($"%{nameof(Sprite2D)}");
        PathFollow2D = GetNode<PathFollow2D>($"%{nameof(PathFollow2D)}");
        TextureHeight = Sprite2D.Texture.GetHeight();
        TextureWidth = Sprite2D.Texture.GetWidth();
        VisibilityChanged += OnVisibilityChanged;
        Render();
    }

    private void OnVisibilityChanged()
    {
        Active = Visible;
    }

    public override void _Process(double delta)
    {
        if(!Active)
        {
            //Reset
            PathFollow2D.ProgressRatio = 0;
            return;
        }

        PathFollow2D.ProgressRatio += (float)(Speed * delta);

        var length = Curve.GetBakedLength();
        var progressRatio = PathFollow2D.ProgressRatio;
        var progressDistance = length * progressRatio;
        var remainingDistance = length - progressDistance;

        var target = TextureHeight / 2;
        var spriteReductionTop = Math.Max(target - remainingDistance, 0);
        var spriteReductionRangeTop = spriteReductionTop / TextureHeight;

        var remainingWidth = Math.Max(target - progressDistance, 0);
        var spriteReductionRangeBottom = remainingWidth / TextureHeight;


        ((ShaderMaterial)Sprite2D.Material).SetShaderParameter("heightReductionTop", spriteReductionRangeTop);
        ((ShaderMaterial)Sprite2D.Material).SetShaderParameter("heightReductionBottom", spriteReductionRangeBottom);
    }
}
