using System.Collections.Generic;
using System.Linq;
using Godot;

// Plays SfxBus cues for the sides enabled in PresentationConfig.Sfx. Its AudioStreamPlayer
// children are the voices (see SfxPlayer.tscn), so overlapping sounds don't cut each other off.
// A cue with a spacing (PresentationConfig.SfxSpacings) waits for the gap after its last play.
// Each play is as loud as AudioBalancer says for its cue, on top of the voice's own volume.
public partial class SfxPlayer : Node
{
    readonly List<AudioStreamPlayer> voices = [];
    // Each voice's volume as set in the scene: a cue's balance is added to it
    readonly Dictionary<AudioStreamPlayer, float> voiceVolumes = [];
    readonly Dictionary<SfxCue, AudioStream> streams = [];
    int nextVoice = 0;
    // Per spaced cue: when it may play next (seconds, engine time), and how many plays are waiting
    readonly Dictionary<SfxCue, (double NextAt, int Waiting)> spaced = [];
    SfxBus? bus = null;

    public override void _Ready()
    {
        voices.AddRange(GetChildren().OfType<AudioStreamPlayer>());
        foreach(var voice in voices) voiceVolumes[voice] = voice.VolumeDb;
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
        if(!PresentationConfig.Sfx.Includes(side) || AudioBalancer.IsMuted(cue)) return;

        var stream = GetStream(cue);
        if(stream is null) return;

        if(!PresentationConfig.SfxSpacings.TryGetValue(cue, out var spacing))
        {
            Play(cue, stream);
            return;
        }

        var now = Time.GetTicksMsec() / 1000.0;
        var (nextAt, waiting) = spaced.GetValueOrDefault(cue, (now, 0));
        var startAt = System.Math.Max(now, nextAt);
        if(startAt <= now)
        {
            spaced[cue] = (now + spacing.Gap, waiting);
            Play(cue, stream);
            return;
        }
        if(waiting >= spacing.MaxWaiting) return;

        spaced[cue] = (startAt + spacing.Gap, waiting + 1);
        GetTree().CreateTimer(startAt - now).Timeout += () =>
        {
            if(spaced.TryGetValue(cue, out var state)) spaced[cue] = (state.NextAt, state.Waiting - 1);
            Play(cue, stream);
        };
    }

    private void Play(SfxCue cue, AudioStream stream)
    {
        // Prefer an idle voice, otherwise cut the oldest one
        if(!IsInsideTree()) return;
        var voice = voices.Find(v => !v.Playing) ?? voices[nextVoice];
        nextVoice = (nextVoice + 1) % voices.Count;

        voice.Stream = stream;
        voice.VolumeDb = voiceVolumes[voice] + AudioBalancer.VolumeDbOf(cue);
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
