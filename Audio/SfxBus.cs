using System;

public enum SfxCue
{
    Ride,
    Call,
    Target,
    PhaseChange,
    PowerUp,
    CriticalUp,
}

// Everything that wants a sound raises it here; SfxPlayer decides whether it plays
public class SfxBus
{
    public void Play(SfxCue cue, BoardSide side)
    {
        CuePlayed?.Invoke(cue, side);
    }

    public event Action<SfxCue, BoardSide>? CuePlayed;
}
