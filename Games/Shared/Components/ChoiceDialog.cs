using System;
using System.Collections.Generic;
using Godot;
using static SceneHelper;

/// <summary>
/// A dialog listing choices as buttons, picking one closes it and runs the choice. Frees itself once closed.
/// Laid out in ChoiceDialog.tscn; only the buttons are added here.
/// </summary>
public partial class ChoiceDialog : AcceptDialog
{
    const string ScenePath = "res://Games/Shared/Components/ChoiceDialog.tscn";

    public sealed record Choice(string Label, Action OnChosen, bool Disabled = false);

    public static ChoiceDialog Show(Node parent, string title, IEnumerable<Choice> choices)
    {
        var dialog = GetScene<ChoiceDialog>(ScenePath);
        dialog.Title = title;

        var list = dialog.GetNode<VBoxContainer>("%Choices");
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

        dialog.VisibilityChanged += () =>
        {
            if(!dialog.Visible) dialog.QueueFree();
        };
        parent.AddChild(dialog);
        dialog.PopupCentered();
        return dialog;
    }
}
