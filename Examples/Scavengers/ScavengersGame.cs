namespace Scavengers;

public sealed class ScavengersGame(GameSettings settings) : Game(settings)
{
    private SceneManager _sceneManager;

    protected override void OnEnter()
    {
        Globals.Camera = new Camera();
        Globals.CameraUi = new Camera();
        Globals.TempTexture = new Texture(Vect2.One);
        Globals.Texture = AssetManager.Instance.Load<Texture>("Graphics/Spritesheet.png");
        Globals.Sheet = AssetManager.Instance.Load<Spritesheet>("Graphics/Spritesheet.sheet");
        Globals.Map = AssetManager.Instance.Load<LDtkMap>("Maps/Map.ldtk");
        Globals.Font = AssetManager.Instance.LoadSpriteFont("Fonts/Font.png", spacing: 1, lineSpacing: 2);
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

        InputAction.AddAction(GameInputs.MoveUp).AddKey(KeyboardKey.W).AddKey(KeyboardKey.Up).AddGamepad(GamepadButton.DPadUp);
        InputAction.AddAction(GameInputs.MoveRight).AddKey(KeyboardKey.D).AddKey(KeyboardKey.Right).AddGamepad(GamepadButton.DPadRight);
        InputAction.AddAction(GameInputs.MoveDown).AddKey(KeyboardKey.S).AddKey(KeyboardKey.Down).AddGamepad(GamepadButton.DPadDown);
        InputAction.AddAction(GameInputs.MoveLeft).AddKey(KeyboardKey.A).AddKey(KeyboardKey.Left).AddGamepad(GamepadButton.DPadLeft);
        InputAction.AddAction(GameInputs.Interact).AddKey(KeyboardKey.E).AddGamepad(GamepadButton.A);

        Globals.Data = new GameData { Food = Globals.DefaultStartingFruit, PlayTime = 0, Looted = 0 };

        _sceneManager = new SceneManager();
        _sceneManager.Add(new SceneGame());

        base.OnEnter();
    }

    protected override void OnExit()
    {
        Globals.TempTexture.Dispose();
        _sceneManager.Clear();

        base.OnExit();
    }

    protected override void OnUpdate(FrameTime frameTime)
    {


        _sceneManager.Update(frameTime);

        base.OnUpdate(frameTime);
    }

    protected override void OnDraw(FrameTime frameTime)
    {
        _sceneManager.Draw(frameTime);

        base.OnDraw(frameTime);
    }
}
