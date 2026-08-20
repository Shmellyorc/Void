// ============================================================================
//  Program.cs
// ============================================================================
//  Entry point for the FlappyBirb example game.
//
//  This is where everything starts:
//    1. Configure the game window and engine settings
//    2. Create the game instance
//    3. Run the game loop
//
//  Copyright (c) 2025 Void Engine Examples
//  Licensed under the MIT License.
//  See LICENSE file in the project root for full license information.
// ============================================================================


// ----------------------------------------------------------------------------
// Game Settings
// ----------------------------------------------------------------------------
// FlappyBirb uses a pixel art style, so we set a small viewport (144x256)
// and scale the window up 4x for that chunky retro look.
// ----------------------------------------------------------------------------

using Void.Engine.Saves;

var settings = GameSettings.Instance
    .SetAppCompany("Shmellyorc")
    .SetAppName("FlappyBirb")
    .SetAppTitle("Flappy Birb")
    .SetHalfTexelOffset(false)          // Keeps pixel art crisp
    .SetWindow(144 * 4, 256 * 4)       // Window is 4x the viewport size
    .SetViewport(144, 256)             // Game renders at 144x256 resolution
    .Build();

// ----------------------------------------------------------------------------
// Create and Run
// ----------------------------------------------------------------------------

using var game = new FlappyBirbGame(settings);

game.Run();