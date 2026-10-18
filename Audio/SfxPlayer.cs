using System.Collections.Generic;
using Godot;

// Plays SfxBus cues for the sides enabled in PresentationConfig.Sfx,
// with a few voices so overlapping sounds don't cut each other off
public partial class SfxPlayer : Node
{
    readonly List<AudioStreamPlayer> voices = [];
    readonly Dictionary<SfxCue, AudioStream> streams = [];
    int nextVoice = 0;
    SfxBus? bus = null;

    public override void _Ready()
    {
        for(int i = 0; i < PresentationConfig.SfxVoices; i++)
        {
            var voice = new AudioStreamPlayer();
            AddChild(voice);
            voices.Add(voice);
        }
    }

    public void Listen(SfxBus sfxBus)
    {
        if(bus is not null)
        {
            bus.CuePlayed -= OnCuePlayed;
        }
        bus = sfxBus;
        bus.CuePlayed += OnCuePlayed;
    }

    public override void _ExitTree()
    {
        if(bus is not null)
        {
            bus.CuePlayed -= OnCuePlayed;
        }
    }

    private void OnCuePlayed(SfxCue cue, BoardSide side)
    {
        if(!PresentationConfig.Sfx.Includes(side)) return;

        var stream = GetStream(cue);
        if(stream is null) return;

        // Prefer an idle voice, otherwise cut the oldest one
        var voice = voices.Find(v => !v.Playing) ?? voices[nextVoice];
        nextVoice = (nextVoice + 1) % voices.Count;

        voice.Stream = stream;
        voice.Play();
    }

    private AudioStream? GetStream(SfxCue cue)
    {
        if(streams.TryGetValue(cue, out var cached)) return cached;
        if(!PresentationConfig.SfxStreams.TryGetValue(cue, out var path)) return null;

        var stream = ResourceLoader.Load<AudioStream>(path);
        streams[cue] = stream;
        return stream;
    }
}
