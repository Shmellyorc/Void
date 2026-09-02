// ============================================================
//  Void Engine - Game Entry Point
// ============================================================
//  This file is the entry point for your game.
//  It sets up the game window and starts the game loop.
//
//  For more information:
//  https://github.com/Shmellyorc/Void/wiki/Getting-Started
// ============================================================

using MyGame;           // Your game's namespace
using Void.Engine;      // The Void Engine core

// ============================================================
//  1. Configure game settings
// ============================================================
//  SetAppCompany   - The company name (used for AppData folders)
//  SetAppName      - The game name (used for AppData folders)
//  SetAppTitle     - The title displayed in the window title bar
//  SetWindow       - The window size (width, height)
//  Build()         - Finalizes the settings
// ============================================================

var settings = GameSettings.Instance
    .SetAppCompany("MyCompany")     // Change this to your company name
    .SetAppName("MyGame")           // Change this to your game name
    .SetAppTitle("My Game")         // Change this to your game title
    .SetWindow(1280, 720)           // Change this to your preferred resolution
    .Build();

// ============================================================
//  2. Create and run the game
// ============================================================
//  The game instance is created with the settings above.
//  game.Run() starts the game loop and blocks until the window closes.
// ============================================================

using var game = new MyGameGame(settings);

game.Run();

// ============================================================
//  NEXT STEPS
// ============================================================
//  1. Open MyGameGame.cs to add your game logic
//  2. Add assets to the Content/ folder
//
//  See the Getting Started guide:
//  https://github.com/Shmellyorc/Void/wiki/Getting-Started
//
//  Full documentation:
//  https://github.com/Shmellyorc/Void/wiki
// ============================================================