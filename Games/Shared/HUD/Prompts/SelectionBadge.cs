using Godot;
using static SceneHelper;

/// <summary>
/// A round badge with a short text over a card: which pick it was ("1", "2", … on the board's circles and in a
/// SelectionGrid) or a count (a deck list's "x3"). Hidden while it has nothing to say. Laid out in SelectionBadge.tscn.
/// </summary>
public partial class SelectionBadge : PanelContainer
{
    public const string ScenePath = "res://Games/Shared/HUD/Prompts/SelectionBadge.tscn";

    Label? label;
    // Looked up when first used: the badge may be set before it's in the tree
    Label Label => label ??= GetNode<Label>("%Text");

    public static SelectionBadge Create() => GetScene<SelectionBadge>(ScenePath);

    /// <summary>The pick number shown, or null: hidden.</summary>
    public int? Number
    {
        get => int.TryParse(Text, out var number) ? number : null;
        set => Text = value?.ToString();
    }

    /// <summary>The text shown, or null: hidden.</summary>
    public string? Text
    {
        get => Visible ? Label.Text : null;
        set
        {
            Label.Text = value ?? string.Empty;
            Visible = value is not null;
        }
    }

    /// <summary>Overrides the scene's font size (e.g. a card view that scales with its card).</summary>
    public void SetFontSize(int size) => Label.AddThemeFontSizeOverride("font_size", size);
}
