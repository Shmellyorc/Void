namespace Void.Engine.Sounds;

public class SoundLoopedEventArgs : SoundEventArgs
{
    public int LoopCount { get; }

    public SoundLoopedEventArgs(SoundInstance instance, int loopCount) : base(instance)
    {
        LoopCount = loopCount;
    }
}
