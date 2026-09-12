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
// EXAMPLE TOUR
// ----------------------------------------------------------------------------
// New to VOID? Read this example in this order:
//   1. Program.cs          - GameSettings and starting the Game
//   2. FlappyBirbGame.cs   - asset loading and the update/draw lifecycle
//   3. Birb.cs             - input, movement, physics, and animation
//   4. Pipe.cs             - collision, scrolling obstacles, and scoring
//   5. Globals.cs          - shared assets and tuning values
//
// See README.md next to this file for a guided walkthrough and experiments.
// ----------------------------------------------------------------------------

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
    .SetWindow(144 * 4, 256 * 4)       // Window is 4x the viewport size
    .SetViewport(144, 256)             // Game renders at 144x256 resolution
    .Build();

// ----------------------------------------------------------------------------
// Create and Run
// ----------------------------------------------------------------------------

using var game = new FlappyBirbGame(settings);

game.Run();
