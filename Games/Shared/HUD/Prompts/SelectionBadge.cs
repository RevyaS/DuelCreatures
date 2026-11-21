using Godot;
using static SceneHelper;

/// <summary>
/// A badge with a short text over whatever it marks: which pick it was ("1", "2", … on the board's circles and on a
/// SelectableCard) or a count (a deck list's "x3"). Hidden while it has nothing to say.
/// <para>
/// The scene's root covers the thing it marks (it's put over a zone or a card, full size; the scene's 193x193 is a
/// circle on the field, to design against), and %Pill is the badge itself, placed in it by its anchors. The %Pill shows
/// %Background (a Panel: its style is the shape and colour), %Image (a TextureRect: give it a texture to draw an image
/// instead, and hide %Background) and %Text, all filling it. Its size is its minimum size: a wider text widens it.
/// Laid out in SelectionBadge.tscn; the code only sets the text.
/// </para>
/// </summary>
public partial class SelectionBadge : Control
{
    public const string ScenePath = "res://Games/Shared/HUD/Prompts/SelectionBadge.tscn";

    Label? label;
    // Looked up when first used: the badge may be set before it's in the tree
    Label Label => label ??= GetNode<Label>("%Text");
    string? text;

    public static SelectionBadge Create() => GetScene<SelectionBadge>(ScenePath);

    public override void _Ready() => Text = text;

    /// <summary>The pick number shown, or null: hidden.</summary>
    public int? Number
    {
        get => int.TryParse(Text, out var number) ? number : null;
        set => Text = value?.ToString();
    }

    /// <summary>The text shown, or null: hidden.</summary>
    public string? Text
    {
        get => text;
        set
        {
            text = value;
            Visible = value is not null;
            if(IsNodeReady()) Label.Text = value ?? string.Empty;
        }
    }

    /// <summary>Overrides the scene's font size (e.g. a card view that scales with its card).</summary>
    public void SetFontSize(int size) => Label.AddThemeFontSizeOverride("font_size", size);
}
