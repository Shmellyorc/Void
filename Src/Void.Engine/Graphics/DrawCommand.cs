namespace Void.Engine.Graphics;

internal struct DrawCommand
{
    public SFTexture Texture;
    public float Depth;
    public Rect2 DstRect;
    public Rect2 SrcRect;
    public Color Color;
    public float Rotation;
    public Vect2 Scale;
    public Vect2 Origin;
    public TextureEffects Effects;
}