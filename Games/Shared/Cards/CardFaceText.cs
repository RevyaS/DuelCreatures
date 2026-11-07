using ArC.CardGames.Components;
using Godot;

/// <summary>
/// A text on a card's face, written by <see cref="Format"/> (null hides it, e.g. a Level on a spell). Font, colour,
/// alignment and position are the Label's, set in the scene; <see cref="Prefix"/> and <see cref="Suffix"/> go
/// around the value (e.g. "ATK/").
/// </summary>
[Tool]
public abstract partial class CardFaceText : Label, ICardFacePart
{
    [Export] public string Prefix { get; set; } = string.Empty;
    [Export] public string Suffix { get; set; } = string.Empty;

    bool hasValue;
    bool faceUp = true;

    /// <summary>Whether the card it was last shown with has a value here (e.g. not ATK on a spell).</summary>
    public bool HasValue => hasValue;

    /// <summary>The value to show for <paramref name="card"/>, or null when it has none.</summary>
    protected abstract string? Format(CardBase card);

    /// <summary>What goes before the value for <paramref name="card"/>: the <see cref="Prefix"/>, unless a part says otherwise.</summary>
    protected virtual string PrefixFor(CardBase card) => Prefix;

    public void Show(CardBase card)
    {
        var value = Format(card);
        hasValue = value is not null;
        Text = hasValue ? $"{PrefixFor(card)}{value}{Suffix}" : string.Empty;
        Visible = hasValue && faceUp;
    }

    public void FaceUp(bool faceUp)
    {
        this.faceUp = faceUp;
        // In the editor the card scene keeps its placeholder text, to place and style the part
        if(Engine.IsEditorHint()) return;
        Visible = hasValue && faceUp;
    }
}
