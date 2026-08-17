global using System.Collections;

global using Scavengers;
global using Scavengers.Entities;
global using Scavengers.Scenes;

global using Void.Engine;
global using Void.Engine.Assets;
global using Void.Engine.Assets.Loaders;
global using Void.Engine.Assets.Loaders.LDtk;
global using Void.Engine.Assets.Loaders.LDtk.Instances;
global using Void.Engine.Assets.Loaders.Spritesheets;
global using Void.Engine.Beacons;
global using Void.Engine.Coroutines;
global using Void.Engine.Coroutines.Routines.Animations;
global using Void.Engine.Graphics;
global using Void.Engine.Helpers;
global using Void.Engine.Inputs.Gamepads;
global using Void.Engine.Inputs.InputActions;
global using Void.Engine.Inputs.Keyboards;
global using Void.Engine.Pathfinding;
global using Void.Engine.Systems;

////////////////////////////////////////////////////////////////////////////

var setting = new GameSettings()
    .SetAppCompany("Shmellyorc")
    .SetAppName("Scravengers")
    .SetAppTitle("Scavengers")
    .SetClearColor(new Color("#3e3f3e"))
    .Build();

using var game = new ScavengersGame(setting);

game.Run();

////////////////////////////////////////////////////////////////////////////

public enum GameInputs
{
    MoveUp, MoveDown, MoveLeft, MoveRight, Interact
}

public enum GameBecaons
{
    PlayerMoved,
    LockUnits,
    PlayerInteract,
    EnemyMoved
}