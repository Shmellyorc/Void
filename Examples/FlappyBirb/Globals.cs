// ============================================================================
//  Globals.cs
// ============================================================================
//  Shared constants and assets for the FlappyBirb example.
//
//  This file holds values that multiple classes need access to,
//  like the pipe gap size, scroll speeds, and loaded assets.
//
//  Copyright (c) 2025 Void Engine Examples
//  Licensed under the MIT License.
//  See LICENSE file in the project root for full license information.
// ============================================================================

namespace FlappyBirb;

/// <summary>
/// Global constants and shared assets for the game.
/// </summary>
public static class Globals
{
    // Gameplay tuning
    public const float PipeGap = 60;              // Vertical gap between top and bottom pipes
    public const float Floor = 56;                // Height of the ground strip at the bottom

    // Scroll speeds (pixels per second)
    public const float GroundSpeed = 30f;         // Ground scrolls fastest (closest to player)
    public const float BackgroundSpeed = 15f;     // Background scrolls slower (parallax effect)

    // Shared assets loaded once in FlappyBirbGame.OnEnter()
    public static Texture Texture;                // The main spritesheet texture
    public static SpriteFont Font;                // The pixel font for score and UI text
    public static Spritesheet Sheet;              // Sprite bounds data for all game sprites
}