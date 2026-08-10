namespace Void.Engine.Sounds.EventArg;

public class SoundEventArgs : EventArgs
{
    public SoundInstance Instance { get; }
    public string SoundName { get; }
    public float PlayTime { get; }
    public float Duration { get; }

    public SoundEventArgs(SoundInstance instance)
    {
        Instance = instance;
        SoundName = instance?.SoundName ?? "Unknown";
        PlayTime = instance?.PlayTime ?? 0f;
        Duration = instance?.Duration ?? 0f;
    }
}
