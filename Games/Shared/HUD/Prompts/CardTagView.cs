using System;
using Godot;

/// <summary>
/// The location indicator: shows a <see cref="CardTag"/> (where a card offered to pick is, or what it's for) over the
/// card. The scene's root covers the card at the card component's own size (300x420, scaled with the card), and %Panel
/// is the indicator itself, placed by its anchors; it holds %Icon and %Text, each shown only when the tag has one.
/// Hidden with no tag. Restyle it, or give it a graphic, in CardTagView.tscn; the code only fills in the tag.
/// </summary>
public partial class CardTagView : Control
{
    TextureRect? icon;
    Label? text;
    PanelContainer? textPanel;
    TextureRect IconRect => icon ??= GetNode<TextureRect>("%Icon");
    Label TextLabel => text ??= GetNode<Label>("%Text");
    PanelContainer TextPanel => textPanel ??= GetNode<PanelContainer>($"%{nameof(TextPanel)}"); 

    CardTag? tag;

    public override void _Ready()
    {
        Tag = tag;
    }

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
            if(!IsNodeReady()) return;
            IconRect.Texture = value?.Icon;
            IconRect.Visible = value?.HasIcon == true;
            TextLabel.Text = value?.Text ?? string.Empty;
            TextPanel.Visible = value?.HasText == true;
        }
    }
}
