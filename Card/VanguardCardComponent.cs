using ArC.CardGames.Predefined.Vanguard;
using Godot;

public partial class VanguardCardComponent : Card
{
    VanguardCard card;
    public VanguardCard Card => card;
    public void LoadVanguardCard(VanguardCard card)
    {
        this.card = card;
        Texture = GetCardNameTexture(card.Name);
    }

    private Texture2D GetCardNameTexture(string cardName)
    {
        var cleanedName = cardName.Replace(" ", "").Replace(",", "");
        var path = $"res://Assets/Cards/{cleanedName}.png";
        return ResourceLoader.Load<Texture2D>(path);
    }
}
