namespace Scavengers.Scenes;

public sealed class SceneTransition : Scene
{
    private SpriteBatcher _batch;
    private float _fade, _textFade;
    private Camera _camera;

    public SceneTransition()
    {
        Layer = 1;
    }

    public override void OnEnter()
    {
        _batch = new SpriteBatcher();
        _camera = new Camera();

        CoroutineManager.Instance.Run(Transition());
        CoroutineManager.Instance.Run(Globals.FadeOutMusic());

        base.OnEnter();
    }

    public override void OnExit()
    {
        _batch.Dispose();

        base.OnExit();
    }

    public override void Draw(FrameTime frameTime)
    {
        _batch.Begin(SortMode.BackToFront, camera: _camera);

        _batch.DrawText(Globals.Font, $"Day {Globals.Data.Days}", GameSettings.Instance.Viewport / 2, Color.WithAlpha(Color.White, _textFade), TextAlignment.Center, Vect2.One, 1f);

        _batch.DrawBypassAtlas(Globals.TempTexture, new Rect2(Vect2.Zero, GameSettings.Instance.Viewport), Color.WithAlpha(GameSettings.Instance.ClearColor, _fade), 0f);

        _batch.End();

        base.Draw(frameTime);
    }

    private IEnumerator Transition()
    {
        var sm = SceneManager.Instance;
        var toRemove = new List<Scene>(sm.Scenes.Count);

        yield return Globals.FadeInOut(0f, 1f, 0.8f, v => _fade = v);
        yield return Globals.FadeInOut(0f, 1f, 1.2f, v => _textFade = v);
        yield return new WaitForNextFrame();

        SoundExtensions.PlayRandom([Globals.FootStep1, Globals.FootStep2], volume: Globals.SoundFxVolume);

        

        Globals.Data.Days++;
        yield return new WaitForSeconds(2.5f);

        foreach (var scene in sm.Scenes)
        {
            if (scene == this)
                continue;

            toRemove.Add(scene);
        }

        yield return new WaitForNextFrame();

        foreach (var scene in toRemove)
            SceneManager.Instance.Remove(scene);
        toRemove.Clear();

        yield return new WaitForNextFrame();

        sm.Add(new SceneGame());

        var r1 = CoroutineManager.Instance.Run(Globals.FadeInOut(1f, 0f, 0.8f, v => _fade = v));
        var r2 = CoroutineManager.Instance.Run(Globals.FadeInOut(1f, 0f, 0.8f, v => _textFade = v));

        yield return new WaitWhile(() => r1.IsRunning || r2.IsRunning);

        ExitScene();
    }
}
