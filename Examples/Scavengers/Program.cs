// ============================================================================
//  Program.cs - Scavengers Demo Entry Point
// ============================================================================
//  This file contains the global usings, enums, data classes, and the main
//  entry point for the Scavengers demo game.
//
//  The demo shows how to:
//  - Configure the engine with GameSettings
//  - Define custom input actions and beacon topics
//  - Launch the game
// ============================================================================

// Global usings - these are available in every file without needing to import
global using System.Collections;

global using Scavengers;
global using Scavengers.Entities;
global using Scavengers.Scenes;

// Void Engine core namespaces
global using Void.Engine;
global using Void.Engine.Assets;
global using Void.Engine.Assets.Loaders;
global using Void.Engine.Assets.Loaders.Fonts;
global using Void.Engine.Assets.Loaders.LDtk;
global using Void.Engine.Assets.Loaders.LDtk.Instances;
global using Void.Engine.Assets.Loaders.Spritesheets;
global using Void.Engine.Beacons;
global using Void.Engine.Coroutines;
global using Void.Engine.Coroutines.Routines.Animations;
global using Void.Engine.Coroutines.Routines.Conditionals;
global using Void.Engine.Coroutines.Routines.Time;
global using Void.Engine.Coroutines.Routines.Utilities;
global using Void.Engine.Graphics;
global using Void.Engine.Helpers;
global using Void.Engine.Inputs.Gamepads;
global using Void.Engine.Inputs.InputActions;
global using Void.Engine.Inputs.Keyboards;
global using Void.Engine.Pathfinding;
global using Void.Engine.Sounds;
global using Void.Engine.Systems;

/// <summary>
/// Custom input actions for the Scavengers demo.
/// These are used with Void's InputAction system for flexible input mapping.
/// </summary>
public enum GameInputs
{
    MoveUp,
    MoveDown,
    MoveLeft,
    MoveRight,
    Interact
}

/// <summary>
/// Custom beacon topics for the Scavengers demo.
/// Beacons are used for decoupled communication between systems.
/// </summary>
public enum GameBecaons
{
    PlayerMoved,      // Published when the player moves
    LockUnits,        // Locks all units from moving (e.g., during transitions)
    PlayerInteract,   // Published when the player interacts with something
    UpdateFood,       // Updates the player's food count
    PlayerHit,        // Published when the player takes damage
    GameOver,         // Published when the game ends
}

/// <summary>
/// Persistent game data that survives scene transitions.
/// This data is stored globally and persists across scenes.
/// </summary>
public sealed class GameData
{
    public float PlayTime;   // Total time played in seconds
    public int Food;         // Current food count
    public int Looted;       // Total food looted
    public int Days = 1;     // Current day/level number
}

/// <summary>
/// Entry point for the Scavengers demo game.
/// </summary>
internal sealed class Program
{
    /// <summary>
    /// Main entry point. Configures the engine, creates the game instance, and runs it.
    /// </summary>
    [STAThread]
    private static void Main(string[] _)
    {
        // Build the game settings using the fluent builder pattern
        var setting = GameSettings.Instance
            .SetAppCompany("Shmellyorc")                        // Required: Company name
            .SetAppName("Scravengers")                          // Required: Application name
            .SetAppTitle("Scavengers")                          // Window title
            .SetClearColor(new Color("#3e3f3e"))                // Background color
            .SetLogMinLevel(Void.Engine.Logs.LogLevel.Debug)    // Show debug logs
            .Build();

        // Create the game instance and run it
        // The using statement ensures proper cleanup when the game exits
        using var game = new ScavengersGame(setting);
        game.Run();
    }
}