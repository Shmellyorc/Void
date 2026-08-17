using Void.Engine.Coroutines.Routines.Conditionals;
using Void.Engine.Coroutines.Routines.Time;

namespace Scavengers.Scenes;

public sealed class SceneTransition : Scene
{
    private SpriteBatcher _batch;

    private Texture _texture;
    private float _fade, _textFade;
    private Camera _camera;

    public SceneTransition()
    {
        Layer = 1;
    }

    public override void OnEnter()
    {
        _texture = new Texture(Vect2.One);
        _batch = new SpriteBatcher();
        _camera = new Camera();

        CoroutineManager.Instance.Run(Transition());

        base.OnEnter();
    }

    public override void OnExit()
    {
        _texture.Dispose();
        _batch.Dispose();

        base.OnExit();
    }

    public override void Draw(FrameTime frameTime)
    {
        _batch.Begin(SortMode.BackToFront, camera: _camera);

        _batch.DrawText(Globals.Font, $"Day {Globals.Data.Days}", GameSettings.Instance.Viewport / 2, Color.WithAlpha(Color.White, _textFade), TextAlignment.Center, Vect2.One, 1f);

        _batch.DrawBypassAtlas(_texture, new Rect2(Vect2.Zero, GameSettings.Instance.Viewport), Color.WithAlpha(GameSettings.Instance.ClearColor, _fade), 0f);

        _batch.End();

        base.Draw(frameTime);
    }

    private IEnumerator Transition()
    {
        var sm = SceneManager.Instance;
        var toRemove = new List<Scene>(sm.Scenes.Count);

        yield return FadeBackground(0f, 1f, 0.8f);
        yield return FadeText(0f, 1f, 1.2f);

        Globals.Data.Days++;

        yield return new WaitForSeconds(1.5f);

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

        var r1 = CoroutineManager.Instance.Run(FadeBackground(1f, 0f, 0.8f));
        var r2 = CoroutineManager.Instance.Run(FadeText(1f, 0f, 0.8f));

        yield return new WaitWhile(() => r1.IsRunning || r2.IsRunning);

        ExitScene();
    }

    private IEnumerator FadeBackground(float start, float end, float speed)
    {
        yield return new Tween<float>(start, end, speed, EaseType.SineOut, MathHelper.Lerp, v => _fade = v);
    }

    private IEnumerator FadeText(float start, float end, float speed)
    {
        yield return new Tween<float>(start, end, speed, EaseType.SineOut, MathHelper.Lerp, v => _textFade = v);
    }
}
