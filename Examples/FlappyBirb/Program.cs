var settings = new GameSettings()
    .SetAppCompany("Shmellyorc")
    .SetAppName("FlappyBirb")
    .SetAppTitle("Flappy Birb")
    .SetHalfTexelOffset(true)
    .SetWindow(144 * 4, 256 * 4)
    .SetViewport(144, 256)
    .Build();

using var game = new FlappyBirbGame(settings);

game.Run();

// var test = new PathfinderTest();
// test.Run();