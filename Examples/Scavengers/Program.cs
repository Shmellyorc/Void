global using System.Collections;

global using Scavengers;
global using Scavengers.Entities;
global using Scavengers.Scenes;

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

public enum GameInputs
{
    MoveUp, MoveDown, MoveLeft, MoveRight, Interact
}

public enum GameBecaons
{
    PlayerMoved,
    LockUnits,
    PlayerInteract,
    UpdateFood,
    PlayerHit,
    GameOver,
}

public sealed class GameData
{
    public float PlayTime;
    public int Food;
    public int Looted;
    public int Days = 1;
}

internal sealed class Program
{
    [STAThread]
    private static void Main(string[] _)
    {
        var setting = GameSettings.Instance
            .SetAppCompany("Shmellyorc")
            .SetAppName("Scravengers")
            .SetAppTitle("Scavengers")
            .SetClearColor(new Color("#3e3f3e"))
            .SetLogMinLevel(Void.Engine.Logs.LogLevel.Debug)
            .Build();

        using var game = new ScavengersGame(setting);

        game.Run();
    }
}