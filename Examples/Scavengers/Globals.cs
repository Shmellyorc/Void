// ============================================================================
//  Globals.cs - Scavengers Demo Global Constants and Shared Resources
// ============================================================================
//  This file contains all the global constants, shared resources, and utility
//  coroutines used throughout the Scavengers demo.
//
//  The demo shows how to:
//  - Define game constants in one place
//  - Share assets and resources globally
//  - Create reusable coroutines for common effects
// ============================================================================

namespace Scavengers;

/// <summary>
/// Global constants and shared resources for the Scavengers demo.
/// All game-wide data and utilities are stored here for easy access.
/// </summary>
public static class Globals
{
    // ============================================================================
    // Game Constants
    // ============================================================================
    public const int TileSize = 32;                      // Size of each tile in pixels
    public const float DefaultDepth = 0.3f;              // Default rendering depth (Z-order)
    public const float PlayerDepth = 0.7f;               // Player render depth (on top of most things)
    public const float EnemyDepth = 0.6f;                // Enemy render depth (between player and tiles)
    public const float MoveSpeed = 75f;                  // Movement speed in pixels per second

    // Food system constants
    public const int DefaultStartingFruit = 80;          // Starting food at game start
    public const int SodaPoints = 12;                    // Food points gained from soda
    public const int FruitPoints = 4;                    // Food points gained from fruit
    public const int EnemyFoodReduction = -10;           // Food lost when hit by enemy
    public const int PlayerMoveFoodReduction = -1;       // Food lost per movement step
    public const int PlayerAttackFoodReduction = -2;     // Food lost per attack

    // Audio volumes
    public const float MusicVolume = 0.5f;               // Background music volume
    public const float SoundFxVolume = 0.25f;            // Sound effects volume

    // ============================================================================
    // Level Management
    // ============================================================================
    /// <summary>
    /// List of available level names from the LDtk map.
    /// The game randomly selects from these when starting or transitioning.
    /// </summary>
    public static readonly string[] Levels = ["Level_0", "Level_1", "Level_2"];

    // ============================================================================
    // Shared Resources
    // These are loaded once in ScavengersGame.OnEnter() and used everywhere.
    // ============================================================================
    public static Camera Camera;                         // Main game camera (world space)
    public static Camera CameraUi;                       // UI camera (screen space)
    public static Texture Texture;                       // Spritesheet texture
    public static Spritesheet Sheet;                     // Spritesheet data (bounds, patches, pivots)
    public static SpriteFont Font;                       // Bitmap font for rendering text
    public static GameData Data;                         // Persistent game data
    public static Texture TempTexture;                   // 1x1 white texture for fades and overlays
    public static LDtkMap Map;                           // The LDtk map containing all levels

    // ============================================================================
    // Audio Assets
    // ============================================================================
    public static Sound Chop1, Chop2;                    // Player attack sounds
    public static Sound Die;                             // Player death sound
    public static Sound Enemy1, Enemy2;                  // Enemy hit sounds
    public static Sound FootStep1, FootStep2;            // Footstep sounds
    public static Sound Fruit1, Fruit2;                  // Fruit collection sounds
    public static Sound Soda1, Soda2;                    // Soda collection sounds
    public static Sound Music;                           // Background music asset

    /// <summary>
    /// The currently playing music instance. This is used to control volume fading.
    /// </summary>
    public static SoundInstance MusicInstance { get; internal set; }

    // ============================================================================
    // Reusable Coroutines
    // ============================================================================

    /// <summary>
    /// Creates a smooth fade transition between two values.
    /// </summary>
    /// <param name="start">Starting value</param>
    /// <param name="end">Ending value</param>
    /// <param name="speed">Duration of the transition in seconds</param>
    /// <param name="value">Callback that receives the current interpolated value</param>
    public static IEnumerator FadeInOut(float start, float end, float speed, Action<float> value)
    {
        // Tween from start to end using quadratic easing for a smooth feel
        yield return new Tween<float>(start, end, speed, EaseType.QuadIn, MathHelper.Lerp, v => value?.Invoke(v));
    }

    /// <summary>
    /// Fades in the background music from silence to full volume.
    /// </summary>
    public static IEnumerator FadeInMusic()
    {
        // Create a new instance of the music sound
        MusicInstance = Music.CreateInstance();
        MusicInstance.Looping = true;                     // Loop the music indefinitely
        MusicInstance.Volume = 0f;                        // Start at silence
        MusicInstance.Play();

        // Fade in over 2.5 seconds using sine easing for a smooth start
        yield return new Tween<float>(0f, MusicVolume, 2.5f, EaseType.SineIn, MathHelper.Lerp,
            v => MusicInstance.Volume = v);
    }

    /// <summary>
    /// Fades out the background music to silence.
    /// </summary>
    public static IEnumerator FadeOutMusic()
    {
        // Fade out over 2.5 seconds using sine easing for a smooth end
        yield return new Tween<float>(MusicVolume, 0f, 2.5f, EaseType.SineOut, MathHelper.Lerp,
            v => MusicInstance.Volume = v);

        // Stop the music after the fade is complete
        MusicInstance.Stop();
    }
}