using System;
using ArC.CardGames.Components;
using Godot;

[Tool]
public partial class Card : CardBaseComponent
{
    CardBase card = null!;
    public virtual CardBase CurrentCard => card;

    protected override void OnPressed()
    {
        CardPressed?.Invoke(this);
    }

    protected override void OnLongPress()
    {
        CardLongPressed?.Invoke(this);
    }

    public override Card CreateClone()
    {
        var preview = CardScenes.CreateBase(CurrentCard);
        return preview;
    }

    public virtual void LoadCard(CardBase card)
    {
        this.card = card;
        Texture = GetCardNameTexture(card.Name);
    }

    protected Texture2D GetCardNameTexture(string cardName)
    {
        var cleanedName = cardName.Replace(" ", "")
            .Replace(",", "")
            .Replace(".", "")
            .Replace("'", "");
        var path = $"res://Assets/Cards/{cleanedName}.png";
        if(ResourceLoader.Exists(path)) return ResourceLoader.Load<Texture2D>(path);

        // Card art isn't all in the project yet: a card without art shows no texture, with a warning once per card name
        // (Load would log an error for every card node and every flight)
        if(MissingArtWarned.Add(path))
        {
            GD.PushWarning($"No card art for \"{cardName}\" ({path}); the card is shown without it");
        }
        return null!;
    }

    static readonly System.Collections.Generic.HashSet<string> MissingArtWarned = [];

    public event Action<Card>? CardPressed;
    public event Action<Card>? CardLongPressed;
}