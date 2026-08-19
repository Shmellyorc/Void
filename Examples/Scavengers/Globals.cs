using Void.Engine.Sounds;

namespace Scavengers;

public static class Globals
{
    public const int TileSize = 32;
    public const float DefaultDepth = 0.3f;
    public const float PlayerDepth = 0.7f;
    public const float EnemyDepth = 0.6f;
    public const float MoveSpeed = 75f;

    public const int DefaultStartingFruit = 80;
    public const int SodaPoints = 12;
    public const int FruitPoints = 4;
    public const int EnemyFoodReduction = -10;
    public const int PlayerMoveFoodReduction = -1;
    public const int PlayerAttackFoodReduction = -1;

    public const float MusicVolume = 0.5f;
    public const float SoundFxVolume = 0.25f;

    public static readonly string[] Levels = ["Level_0", "Level_1", "Level_2"];

    public static Camera Camera, CameraUi;
    public static Texture Texture;
    public static Spritesheet Sheet;
    public static SpriteFont Font;
    public static GameData Data;
    public static Texture TempTexture;
    public static LDtkMap Map;

    public static Sound Music;
    public static Sound Fruit1, Fruit2;
    public static Sound Soda1, Soda2;
    public static Sound FootStep1, FootStep2;
    public static Sound Enemy1, Enemy2;

    public static SoundInstance MusicInstance { get; internal set; }

    public static IEnumerator FadeInOut(float start, float end, float speed, Action<float> value)
    {
        yield return new Tween<float>(start, end, speed, EaseType.QuadIn, MathHelper.Lerp, v => value?.Invoke(v));
    }

    public static IEnumerator FadeInMusic()
    {
        MusicInstance = Music.CreateInstance();
        MusicInstance.Looping = true;
        MusicInstance.Volume = 0f;
        MusicInstance.Play();

        yield return new Tween<float>(0f, MusicVolume, 2.5f, EaseType.SineIn, MathHelper.Lerp,
            v => MusicInstance.Volume = v);
    }

    public static IEnumerator FadeOutMusic()
    {
        yield return new Tween<float>(MusicVolume, 0f, 2.5f, EaseType.SineOut, MathHelper.Lerp,
            v => MusicInstance.Volume = v);

        MusicInstance.Stop();
    }
}
