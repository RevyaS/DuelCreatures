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
        var preview = SceneFactory.CreateCard(CurrentCard);
        return preview;
    }

    public virtual void LoadVanguardCard(CardBase card)
    {
        this.card = card;
        Texture = GetCardNameTexture(card.Name);
    }

    protected Texture2D GetCardNameTexture(string cardName)
    {
        var cleanedName = cardName.Replace(" ", "").Replace(",", "");
        var path = $"res://Assets/Cards/{cleanedName}.png";
        return ResourceLoader.Load<Texture2D>(path);
    }

    public event Action<Card>? CardPressed;
    public event Action<Card>? CardLongPressed;
}