using Godot;

namespace DuelCreatures.Data;

[GlobalClass]
[Tool]
public partial class TextureSleeveInfo : SleeveInfo
{
    [Export]
    public Texture2D Texture { get; set; } = null!;
}