using Void.Engine.Assets.Loaders.Fonts;

namespace Scavengers;

public static class Globals
{
    public const int TileSize = 32;
    public const float DefaultDepth = 0.3f;
    public const float PlayerDepth = 0.7f;
    public const float MoveSpeed = 75f;
    public const int DefaultStartingFruit = 250;

    public static Camera Camera, CameraUi;
    public static Texture Texture;
    public static Spritesheet Sheet;
    public static SpriteFont Font;
    public static GameData Data;
}
