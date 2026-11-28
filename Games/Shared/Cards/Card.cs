using System;
using System.Collections.Generic;
using System.Linq;
using ArC.CardGames.Components;
using Godot;

/// <summary>
/// A card shown in a scene: its art (from its game's art folder, CardGameStyle) and, in a game's card scene, its face
/// parts (ICardFacePart, e.g. a YuGiOh monster's Level and ATK), shown only while it's face up. The game's card script
/// finds its texts by unique name (FindNodes) and fills them in (ShowFace); the parts are found anywhere under the card
/// and turned with it.
/// </summary>
[Tool]
public partial class Card : CardBaseComponent
{
    CardBase card = null!;
    public virtual CardBase CurrentCard => card;

    List<ICardFacePart>? faceParts;

    protected override void OnPressed()
    {
        CardPressed?.Invoke(this);
    }

    protected override void OnLongPress()
    {
        CardLongPressed?.Invoke(this);
    }

    // Its game's card scene, so a dragged copy shows what the card shows
    public override Card CreateClone() => CardScenes.Create(CurrentCard);

    public virtual void LoadCard(CardBase card)
    {
        this.card = card;
        Texture = CardStyles.For(card).ArtOf(card.Name)!;
        ShowFace(card);
        Render();
    }

    /// <summary>Fills in the face texts with what <paramref name="card"/> prints (a game's card script writes it).</summary>
    protected virtual void ShowFace(CardBase card) {}

    protected override void RenderCore()
    {
        base.RenderCore();
        foreach(var part in FaceParts) part.FaceUp(IsFront);
    }

    // Found once: the scene doesn't add parts after it's built
    private List<ICardFacePart> FaceParts => faceParts ??= this.Descendants<ICardFacePart>().ToList();

    public event Action<Card>? CardPressed;
    public event Action<Card>? CardLongPressed;
}
