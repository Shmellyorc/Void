namespace Void.Engine.Sounds.EventArg;

public class SoundErrorEventArgs : EventArgs
{
    public SoundInstance Instance { get; }
    public string SoundName { get; }
    public Exception Exception { get; }
    public string ErrorMessage { get; }

    public SoundErrorEventArgs(SoundInstance instance, Exception exception, string message = null)
    {
        Instance = instance;
        SoundName = instance?.SoundName ?? "Unknwon";
        Exception = exception;
        ErrorMessage = message ?? exception?.Message ?? "Unknown Error";
    }
}