using System;
using System.Collections.Generic;
using Godot;

/// <summary>A dialog listing choices as buttons, picking one closes it and runs the choice. Frees itself once closed.</summary>
public partial class ChoiceDialog : AcceptDialog
{
    public sealed record Choice(string Label, Action OnChosen, bool Disabled = false);

    public static ChoiceDialog Show(Node parent, string title, IEnumerable<Choice> choices)
    {
        var dialog = new ChoiceDialog
        {
            Title = title,
            OkButtonText = "Cancel",
            Theme = GD.Load<Theme>("res://Resources/Themes/GUITheme.tres"),
            MinSize = new Vector2I(900, 800)
        };

        var list = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        foreach(var choice in choices)
        {
            var button = new Button { Text = choice.Label, Disabled = choice.Disabled, Alignment = HorizontalAlignment.Left };
            button.Pressed += () =>
            {
                dialog.Hide();
                choice.OnChosen();
            };
            list.AddChild(button);
        }

        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        scroll.AddChild(list);
        dialog.AddChild(scroll);

        dialog.VisibilityChanged += () =>
        {
            if(!dialog.Visible) dialog.QueueFree();
        };
        parent.AddChild(dialog);
        dialog.PopupCentered();
        return dialog;
    }
}
