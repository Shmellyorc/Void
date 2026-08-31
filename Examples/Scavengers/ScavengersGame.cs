// ============================================================================
//  ScavengersGame.cs - Scavengers Demo Main Game Class
// ============================================================================
//  This is the entry point for the Scavengers demo game. It inherits from
//  Void Engine's Game class and handles the game lifecycle.
//
//  The demo shows how to:
//  - Load assets through the AssetManager
//  - Set up input actions
//  - Initialize the scene manager
//  - Handle the game loop (OnEnter, OnUpdate, OnDraw, OnExit)
// ============================================================================

namespace Scavengers;

/// <summary>
/// The main game class for the Scavengers demo.
/// Inherits from Void Engine's Game class and sets up the game world.
/// </summary>
/// <remarks>
/// This demonstrates how to:
/// - Load assets through the AssetManager
/// - Set up input actions
/// - Initialize the scene manager
/// - Handle the game loop
/// </remarks>
public sealed class ScavengersGame(GameSettings settings) : Game(settings)
{
    private SceneManager _sceneManager;

    /// <summary>
    /// Called once when the game starts.
    /// Loads all assets, sets up input, and starts the first scene.
    /// </summary>
    protected override void OnEnter()
    {
        // Load the asset pack once at startup
        var mount = AssetManager.Instance.LoadPack("GameAssets.pack");
        AssetManager.Instance.AddMountToStart(mount);

        // Setup cameras - one for the game world and one for UI overlay
        // The UI camera has a separate viewport so UI elements are not affected by world zoom
        Globals.Camera = new Camera();
        Globals.CameraUi = new Camera();

        // Temporary texture used for full-screen effects like fades
        // Vect2.One creates a 1x1 texture that can be stretched across the screen
        Globals.TempTexture = new Texture(Vect2.One);

        // Load all game assets through the AssetManager
        // The AssetManager handles caching, so these assets are only loaded once
        Globals.Texture = AssetManager.Instance.Load<Texture>("Graphics/Spritesheet.png");
        Globals.Sheet = AssetManager.Instance.Load<Spritesheet>("Graphics/Spritesheet.sheet");
        Globals.Map = AssetManager.Instance.Load<LDtkMap>("Maps/Map.ldtk");
        Globals.Font = AssetManager.Instance.LoadSpriteFont("Fonts/Font.png", spacing: 1, lineSpacing: 2);

        // Load audio assets
        Globals.Music = AssetManager.Instance.Load<Sound>("Sounds/Music.wav");
        Globals.Fruit1 = AssetManager.Instance.Load<Sound>("Sounds/Fruit1.wav");
        Globals.Fruit2 = AssetManager.Instance.Load<Sound>("Sounds/Fruit2.wav");
        Globals.Soda1 = AssetManager.Instance.Load<Sound>("Sounds/Soda1.wav");
        Globals.Soda2 = AssetManager.Instance.Load<Sound>("Sounds/Soda2.wav");
        Globals.FootStep1 = AssetManager.Instance.Load<Sound>("Sounds/Footstep1.wav");
        Globals.FootStep2 = AssetManager.Instance.Load<Sound>("Sounds/Footstep2.wav");
        Globals.Enemy1 = AssetManager.Instance.Load<Sound>("Sounds/Enemy1.wav");
        Globals.Enemy2 = AssetManager.Instance.Load<Sound>("Sounds/Enemy2.wav");
        Globals.Chop1 = AssetManager.Instance.Load<Sound>("Sounds/Chop1.wav");
        Globals.Chop2 = AssetManager.Instance.Load<Sound>("Sounds/Chop2.wav");
        Globals.Die = AssetManager.Instance.Load<Sound>("Sounds/Die.wav");

        // Define input actions with multiple bindings
        // Each action can be triggered by keyboard keys or gamepad buttons
        InputAction.AddAction(GameInputs.MoveUp)
            .AddKey(KeyboardKey.W)
            .AddKey(KeyboardKey.Up)
            .AddGamepad(GamepadButton.DPadUp);

        InputAction.AddAction(GameInputs.MoveRight)
            .AddKey(KeyboardKey.D)
            .AddKey(KeyboardKey.Right)
            .AddGamepad(GamepadButton.DPadRight);

        InputAction.AddAction(GameInputs.MoveDown)
            .AddKey(KeyboardKey.S)
            .AddKey(KeyboardKey.Down)
            .AddGamepad(GamepadButton.DPadDown);

        InputAction.AddAction(GameInputs.MoveLeft)
            .AddKey(KeyboardKey.A)
            .AddKey(KeyboardKey.Left)
            .AddGamepad(GamepadButton.DPadLeft);

        InputAction.AddAction(GameInputs.Interact)
            .AddKey(KeyboardKey.E)
            .AddGamepad(GamepadButton.A);

        // Initialize game data with starting values
        Globals.Data = new GameData
        {
            Food = Globals.DefaultStartingFruit,
            PlayTime = 0,
            Looted = 0
        };

        // Create the scene manager and add the main gameplay scene
        _sceneManager = new SceneManager();
        _sceneManager.Add(new SceneGame());

        base.OnEnter();
    }

    /// <summary>
    /// Called when the game exits.
    /// Cleans up resources and clears all scenes.
    /// </summary>
    protected override void OnExit()
    {
        // Dispose of the temporary texture to free GPU memory
        Globals.TempTexture.Dispose();

        // Remove all scenes and clean up their resources
        _sceneManager.Clear();

        base.OnExit();
    }

    /// <summary>
    /// Called every frame.
    /// Updates the scene manager which updates all active scenes.
    /// </summary>
    protected override void OnUpdate(FrameTime frameTime)
        => _sceneManager.Update(frameTime);

    /// <summary>
    /// Called every frame after OnUpdate.
    /// Draws all active scenes through the scene manager.
    /// </summary>
    protected override void OnDraw(FrameTime frameTime)
        => _sceneManager.Draw(frameTime);
}