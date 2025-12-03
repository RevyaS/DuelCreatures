using System;
using Godot;

[GlobalClass]
[Icon("res://Assets/Icons/Control.svg")]
public partial class DropArea : Control
{
    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        var result = data.Obj is VanguardCardComponent;
        return result;
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        CardDropped?.Invoke((Card)data.Obj!);
    }

    public event Action<Card>? CardDropped;
}
