using Godot;

namespace DuelCreatures.Data;

[GlobalClass]
[Tool]
public abstract partial class SleeveInfo : Resource
{
    [Export]
    public int MarginLeft { get; set; }
    [Export]
    public int MarginRight { get; set; }
    [Export]
    public int MarginTop { get; set; }
    [Export]
    public int MarginBottom { get; set; }
}