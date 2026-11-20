using System;
using Godot;

/// <summary>
/// Shows a <see cref="CardTag"/>: its %Icon, its %Text, or both, whichever the tag has; hidden with no tag. Laid out in
/// CardTagView.tscn.
/// </summary>
public partial class CardTagView : Control
{
    TextureRect? icon;
    Label? text;
    TextureRect IconRect => icon ??= GetNode<TextureRect>("%Icon");
    Label TextLabel => text ??= GetNode<Label>("%Text");

    CardTag? tag;

    /// <summary>The tag shown, or null: hidden.</summary>
    public CardTag? Tag
    {
        get => tag;
        set
        {
            if(value is { HasIcon: false, HasText: false })
            {
                throw new InvalidOperationException($"The card tag {value.ResourcePath} has neither an icon nor a text to show");
            }
            tag = value;
            Visible = value is not null;
            IconRect.Texture = value?.Icon;
            IconRect.Visible = value?.HasIcon == true;
            TextLabel.Text = value?.Text ?? string.Empty;
            TextLabel.Visible = value?.HasText == true;
        }
    }
}
