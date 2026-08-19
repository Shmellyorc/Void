namespace Scavengers.Scenes;

public sealed class SceneGameOver : Scene
{
    private float _fade, _textFade;
    private SpriteBatcher _batch;
    private bool _isExiting;
    private Camera _camera;
    private CoroutineHandle _handle;
    private float _time;
    private int _looted;
    private bool _isReady;

    public SceneGameOver() => Layer = 1;

    public override void OnEnter()
    {
        _camera = new Camera();
        _batch = new SpriteBatcher();
        _time = Globals.Data.PlayTime;
        _looted = Globals.Data.Looted;

        _handle = CoroutineManager.Instance.Run(TransitionIn());
        CoroutineManager.Instance.Run(Globals.FadeOutMusic());

        base.OnEnter();
    }

    public override void OnExit()
    {
        _batch.Dispose();

        base.OnExit();
    }

    public override void Update(FrameTime frameTime)
    {
        if (!_isReady)
            return;

        var state = InputAction.GetState();

        if (!_isExiting && state.IsPressed(GameInputs.Interact))
        {
            CoroutineManager.Instance.Run(TransitionOut());
            _isExiting = true;
        }

        base.Update(frameTime);
    }

    public override void Draw(FrameTime frameTime)
    {
        _batch.Begin(camera: _camera);

        _batch.DrawBypassAtlas(Globals.TempTexture, new Rect2(Vect2.Zero, GameSettings.Instance.Viewport),
            Color.WithAlpha(GameSettings.Instance.ClearColor, _fade));
        _batch.DrawText(Globals.Font, $"Game Over!\n\nLasted {PlayTime()}\nLooted {Looted()}\n\nPress Interact Key\nto continue",
            GameSettings.Instance.Viewport / 2, Color.WithAlpha(Color.White, _textFade), TextAlignment.Center, Vect2.One, 1f);

        _batch.End();

        base.Draw(frameTime);
    }

    private string PlayTime()
    {
        var t = TimeSpan.FromSeconds(_time);

        if (t.Minutes > 0)
            return $"{t.Minutes:0}:{t.Seconds:00} minutes";

        return $"{t.Seconds:0} seconds";
    }

    private string Looted()
        => $"{_looted} food";

    private IEnumerator TransitionIn()
    {
        yield return Globals.FadeInOut(0f, 1f, 0.8f, v => _fade = v);
        yield return Globals.FadeInOut(0f, 1f, 1.2f, v => _textFade = v);

        Globals.Data = new GameData { Food = Globals.DefaultStartingFruit, PlayTime = 0, Looted = 0 };

        _isReady = true;
    }

    private IEnumerator TransitionOut()
    {
        var sm = SceneManager.Instance;

        var toRemove = new List<Scene>(sm.Scenes.Count);
        foreach (var scene in sm.Scenes)
        {
            if (scene == this)
                continue;

            toRemove.Add(scene);
        }

        foreach (var scene in toRemove)
            SceneManager.Instance.Remove(scene);
        toRemove.Clear();

        sm.Add(new SceneGame());

        var r1 = CoroutineManager.Instance.Run(Globals.FadeInOut(1f, 0f, 0.8f, v => _fade = v));
        var r2 = CoroutineManager.Instance.Run(Globals.FadeInOut(1f, 0f, 0.8f, v => _textFade = v));

        yield return new WaitWhile(() => r1.IsRunning || r2.IsRunning);

        ExitScene();
    }
}
