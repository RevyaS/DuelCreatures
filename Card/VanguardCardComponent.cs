using ArC.CardGames.Predefined.Vanguard;

public partial class VanguardCardComponent : Card
{
    public override VanguardCard CurrentCard => (VanguardCard)base.CurrentCard;
    public override Card CreateClone()
    {
        var card = SceneFactory.CreateVanguardCard(CurrentCard);
        return card;
    }
}
