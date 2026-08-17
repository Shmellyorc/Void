global using MiniPac;

global using MiniPac.Entities;

global using Void.Engine;
global using Void.Engine.Beacons;
global using Void.Engine.Graphics;
global using Void.Engine.Graphics.RenderTargets;
global using Void.Engine.Helpers;
global using Void.Engine.Inputs.InputActions;
global using Void.Engine.Inputs.Keyboards;
global using Void.Engine.Systems;

var settings = new GameSettings()
    .SetAppCompany("Shmellyorc")
    .SetAppName("MiniPac")
    .SetAppTitle("MiniPac")
    .Build();

using var game = new MiniPackGame(settings);

game.Run();

