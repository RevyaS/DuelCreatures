using Godot;

public static class ZoneLayout
{
    // CardLine's margin around its row of cards (2 on each side)
    const float LinePadding = 4;

    /// <summary>
    /// Keeps a line of cards one card tall even when it's empty, so the layout around it doesn't jump as cards come and
    /// go (a hand is emptied when it's bound, then fills up with the opening draw).
    /// </summary>
    public static void KeepCardHeight(Control line, float cardScale)
    {
        var height = Mathf.Ceil(SizeConstants.CardDefaultSize.Y * cardScale + LinePadding);
        if(!Mathf.IsEqualApprox(line.CustomMinimumSize.Y, height))
        {
            line.CustomMinimumSize = new(line.CustomMinimumSize.X, height);
        }
    }
}
