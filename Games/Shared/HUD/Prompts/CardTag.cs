using Godot;

/// <summary>
/// Where a card offered to pick is ("Soul", "Drop", "Hand") or what it's for ("Intercept"): an icon, a text, or both,
/// shown at the card's top right (CardTagView). A tag with neither is an error once it's shown.
/// </summary>
[GlobalClass]
public partial class CardTag : Resource
{
    [Export] public string Text { get; set; } = "";
    [Export] public Texture2D? Icon { get; set; }

    public bool HasText => !string.IsNullOrEmpty(Text);
    public bool HasIcon => Icon is not null;
}
