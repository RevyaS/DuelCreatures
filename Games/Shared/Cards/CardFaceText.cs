using Godot;

/// <summary>
/// A text on a card's face (a Level, an ATK), filled in by the game's card script, which finds it by its unique name
/// and calls <see cref="Show(string?, string?)"/> when a card is loaded; no value hides it (e.g. ATK on a spell). It
/// shows only while the card is face up. Font, colour, alignment and position are the Label's, set in the scene;
/// <see cref="Prefix"/> and <see cref="Suffix"/> go around the value (e.g. "ATK/").
/// </summary>
[Tool]
public partial class CardFaceText : Label, ICardFacePart
{
    [Export] public string Prefix { get; set; } = string.Empty;
    [Export] public string Suffix { get; set; } = string.Empty;

    bool hasValue;
    bool faceUp = true;

    /// <summary>Whether the card it was last shown with has a value here (e.g. not ATK on a spell).</summary>
    public bool HasValue => hasValue;

    /// <summary>Shows <paramref name="value"/> between the prefix and the <see cref="Suffix"/>, or hides when it's null.</summary>
    /// <param name="prefix">Instead of <see cref="Prefix"/> for this card (e.g. an Xyz monster's Rank instead of a Level).</param>
    public void Show(string? value, string? prefix = null)
    {
        hasValue = value is not null;
        Text = hasValue ? $"{prefix ?? Prefix}{value}{Suffix}" : string.Empty;
        Visible = hasValue && faceUp;
    }

    /// <inheritdoc cref="Show(string?, string?)"/>
    public void Show(int? value, string? prefix = null) => Show(value?.ToString(), prefix);

    public void FaceUp(bool faceUp)
    {
        this.faceUp = faceUp;
        // In the editor the card scene keeps its placeholder text, to place and style the part
        if(Engine.IsEditorHint()) return;
        Visible = hasValue && faceUp;
    }
}
