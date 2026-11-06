using Godot;

public static class ZoneLayout
{
    /// <summary>
    /// Keeps a line of cards one card tall (plus <paramref name="padding"/>, its margins) even when it's empty, so the
    /// layout around it doesn't jump as cards come and go (a hand is emptied when it's bound, then fills up with the
    /// opening draw).
    /// </summary>
    public static void KeepCardHeight(Control line, float cardScale, float padding)
    {
        var height = Mathf.Ceil(SizeConstants.CardDefaultSize.Y * cardScale + padding);
        if(!Mathf.IsEqualApprox(line.CustomMinimumSize.Y, height))
        {
            line.CustomMinimumSize = new(line.CustomMinimumSize.X, height);
        }
    }
}
