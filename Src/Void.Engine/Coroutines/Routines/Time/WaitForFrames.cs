namespace Void.Engine.Coroutines.Routines.Time;

public sealed class WaitForFrames : IEnumerator
{
    private float _framesLeft;

    public object Current => null;

    public WaitForFrames(float frames)
    {
        _framesLeft = Math.Max(0f, frames);
    }

    public bool MoveNext()
    {
        _framesLeft--;
        return _framesLeft > 0f;
    }

    public void Reset() => throw new NotSupportedException();
    public void Dispose() { }
}