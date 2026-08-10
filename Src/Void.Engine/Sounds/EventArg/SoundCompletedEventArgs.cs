namespace Void.Engine.Sounds;

public sealed class SoundCompletedEventArgs : SoundEventArgs
{
    public bool WasLooping { get; }
    public int LoopCount { get; }

    public SoundCompletedEventArgs(SoundInstance instance, bool wasLooping, int loopCount) : base(instance)
    {
        WasLooping = wasLooping;
        LoopCount = loopCount;
    }
}
