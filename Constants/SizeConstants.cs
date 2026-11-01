using Godot;

public static class SizeConstants
{
    // A card at scale 1 (the card scene's own size). Every zone sizes itself as
    // this times its CardScale, so a board is laid out by its zones' scales alone.
    public static Vector2 CardDefaultSize => new(300, 420);

    // The scale a zone starts with
    public static float DefaultCardScale = 0.45f;
}