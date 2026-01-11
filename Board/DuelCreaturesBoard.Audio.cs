using System.Threading.Tasks;
using Godot;


public partial class DuelCreaturesBoard : Control
{
    public void PlayRideSfx()
    {
        SfxPlayer.Stream = ResourceLoader.Load<AudioStream>("res://Assets/Sfx/ride.ogg");
        SfxPlayer.Play();
    }

    public void PlayCallSfx()
    {
        SfxPlayer.Stream = ResourceLoader.Load<AudioStream>("res://Assets/Sfx/call.ogg");
        SfxPlayer.Play();
    }
    public void PlayTargetSfx()
    {
        SfxPlayer.Stream = ResourceLoader.Load<AudioStream>("res://Assets/Sfx/target.ogg");
        SfxPlayer.Play();
    }
    public void PlayPhaseChangeSfx()
    {
        SfxPlayer.Stream = ResourceLoader.Load<AudioStream>("res://Assets/Sfx/phase_change.ogg");
        SfxPlayer.Play();
    }
}
