using Void.Engine.Assets.Loaders.Fonts;

namespace Scavengers;

public sealed class ScavengersGame(GameSettings settings) : Game(settings)
{
    private SceneManager _sceneManager;

    protected override void OnEnter()
    {
        Globals.Camera = new Camera();
        Globals.CameraUi = new Camera();
        Globals.Texture = AssetManager.Instance.Load<Texture>("Graphics/Spritesheet.png");
        Globals.Sheet = AssetManager.Instance.Load<Spritesheet>("Graphics/Spritesheet.sheet");
        Globals.Font = AssetManager.Instance.LoadSpriteFont("Fonts/Font.png", spacing: 1);
        Globals.Data = new GameData { Food = Globals.DefaultStartingFruit, PlayTime = 0f };

        InputAction.AddAction(GameInputs.MoveUp).AddKey(KeyboardKey.W).AddKey(KeyboardKey.Up).AddGamepad(GamepadButton.DPadUp);
        InputAction.AddAction(GameInputs.MoveRight).AddKey(KeyboardKey.D).AddKey(KeyboardKey.Right).AddGamepad(GamepadButton.DPadRight);
        InputAction.AddAction(GameInputs.MoveDown).AddKey(KeyboardKey.S).AddKey(KeyboardKey.Down).AddGamepad(GamepadButton.DPadDown);
        InputAction.AddAction(GameInputs.MoveLeft).AddKey(KeyboardKey.A).AddKey(KeyboardKey.Left).AddGamepad(GamepadButton.DPadLeft);
        InputAction.AddAction(GameInputs.Interact).AddKey(KeyboardKey.E).AddGamepad(GamepadButton.A);

        _sceneManager = new SceneManager();
        _sceneManager.Add(new SceneGame());

        base.OnEnter();
    }

    protected override void OnExit()
    {
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
