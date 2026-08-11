namespace Void.Engine.Assets.Loaders.Spritesheets;

public readonly struct SpritesheetEntry
{
    public Rect2 Bounds { get; }
    public Rect2 Patch { get; }
    public Vect2 Pivot { get; }

    internal SpritesheetEntry(Rect2 bounds, Rect2 patch, Vect2 pivot)
    {
        Bounds = bounds;
        Patch = patch;
        Pivot = pivot;
    }
}
