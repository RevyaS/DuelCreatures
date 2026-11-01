using Godot;

public static class CardPlacement
{
    // Global transform of a card centered in `area`, the way CardContainer lays its card out
    public static Transform2D Centered(Control area, float scale, float rotation = 0)
    {
        return Centered(area.GetGlobalTransform(), area.Size, scale, rotation);
    }

    // The same for an area given by its global transform and size (e.g. a slot not laid out yet)
    public static Transform2D Centered(Transform2D areaGlobal, Vector2 areaSize, float scale, float rotation = 0)
    {
        var local = new Transform2D(rotation, new Vector2(scale, scale), 0, Vector2.Zero);
        var cardCenter = local.BasisXform(SizeConstants.CardDefaultSize / 2);
        local.Origin = areaSize / 2 - cardCenter;
        return areaGlobal * local;
    }
}
