namespace Void.Engine.Assets.Loaders.Fonts;

public struct Glyph
{
    public Vect2 Position;  // X, Y in texture atlas
    public Vect2 Size;      // Width, Height
    public Vect2 Offset;    // OffsetX, OffsetY from baseline
    public float Advance;     // Distance to move cursor

    public readonly bool IsEmpty => Size.X <= 0 || Size.Y <= 0;

    public override readonly string ToString()
        => $"Glyph(Pos:{Position}, Size:{Size}, Offset:{Offset}, Advance:{Advance})";
}