namespace Void.Engine.Sounds;

public class SoundStoppedEventArgs : SoundEventArgs
{
    public bool WasPlaying { get; }
    public bool WasPaused { get; }

    public SoundStoppedEventArgs(SoundInstance instance, bool wasPlaying, bool wasPaused) : base(instance)
    {
        WasPlaying = wasPlaying;
        WasPaused = wasPaused;
    }
}
