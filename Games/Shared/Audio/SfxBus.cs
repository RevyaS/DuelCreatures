using System;

// A sound, by name, so each game adds its own (e.g. VanguardSfx); its stream is in PresentationConfig.SfxStreams
public readonly record struct SfxCue(string Name);

// Everything that wants a sound raises it here; SfxPlayer decides whether it plays
public class SfxBus
{
    public void Play(SfxCue cue, BoardSide side)
    {
        CuePlayed?.Invoke(cue, side);
    }

    public event Action<SfxCue, BoardSide>? CuePlayed;
}
