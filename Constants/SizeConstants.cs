using Godot;

public static class SizeConstants
{
    public static Vector2 CardBoardScale = new(CardScaleFactor, CardScaleFactor);
    public static float CardScaleFactor = 0.45f;
    public static Vector2 CardDefaultSize => new(300, 420);
}