using Godot;

namespace DuelCreatures.Data;

[GlobalClass]
[Tool]
public partial class ColorSleeveInfo : SleeveInfo
{
    [Export]
    public Color Color { get; set; } = Colors.White;
}