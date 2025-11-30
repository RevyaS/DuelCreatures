using static SceneHelper;

public static class SceneFactory
{
    public static Card CreateCard()
    {
        return GetScene<Card>("res://Card/Card.tscn");
    }
}