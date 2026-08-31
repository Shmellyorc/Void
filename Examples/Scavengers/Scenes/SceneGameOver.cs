// ============================================================================
//  SceneGameOver.cs - Scavengers Demo Game Over Scene
// ============================================================================
//  This scene displays when the player runs out of food. It shows the player's
//  stats (play time and food looted) and allows them to restart.
//
//  The demo shows how to:
//  - Create a game over screen with fade effects
//  - Display player statistics
//  - Reset game data on restart
//  - Transition back to gameplay
// ============================================================================

namespace Scavengers.Scenes;

/// <summary>
/// Game over scene displayed when the player's food reaches zero.
/// Shows statistics and allows restarting.
/// </summary>
/// <remarks>
/// The scene flow:
/// 1. Fade in the game over screen with stats
/// 2. Wait for the player to press Interact
/// 3. Remove all scenes and start a new game
/// 4. Fade out and exit
/// </remarks>
public sealed class SceneGameOver : Scene
{
    // Fade values for the overlay and text
    private float _fade, _textFade;
    private SpriteBatcher _batch;
    private bool _isExiting;
    private Camera _camera;
    private CoroutineHandle _handle;
    private float _time;
    private int _looted;
    private bool _isReady;

    /// <summary>
    /// Layer 1 ensures this scene renders on top of the game scene.
    /// </summary>
    public SceneGameOver() => Layer = 1;

    /// <summary>
    /// Called when the scene is added.
    /// Captures the player's stats and starts the fade in.
    /// </summary>
    public override void OnEnter()
    {
        _camera = new Camera();
        _batch = new SpriteBatcher();

        // Capture the player's final stats before resetting
        _time = Globals.Data.PlayTime;
        _looted = Globals.Data.Looted;

        // Start the transition in and fade out the music
        _handle = CoroutineManager.Instance.Run(TransitionIn());
        CoroutineManager.Instance.Run(Globals.FadeOutMusic());

        base.OnEnter();
    }

    /// <summary>
    /// Called when the scene is removed.
    /// </summary>
    public override void OnExit()
    {
        _batch.Dispose();

        base.OnExit();
    }

    /// <summary>
    /// Called every frame.
    /// Waits for the player to press Interact to restart.
    /// </summary>
    public override void Update(FrameTime frameTime)
    {
        // Don't process input until the fade in is complete
        if (!_isReady)
            return;

        var state = InputAction.GetState();

        // Wait for the player to press Interact
        if (!_isExiting && state.IsPressed(GameInputs.Interact))
        {
            CoroutineManager.Instance.Run(TransitionOut());
            _isExiting = true;
        }

        base.Update(frameTime);
    }

    /// <summary>
    /// Draws the game over screen.
    /// </summary>
    public override void Draw(FrameTime frameTime)
    {
        _batch.Begin(camera: _camera);

        // Full-screen overlay with the game's clear color
        _batch.DrawBypassAtlas(
            Globals.TempTexture,
            new Rect2(Vect2.Zero, GameSettings.Instance.Viewport),
            Color.WithAlpha(GameSettings.Instance.ClearColor, _fade)
        );

        // Game over text with stats
        // The text fades in separately from the overlay
        _batch.DrawText(
            Globals.Font,
            $"Game Over!\n\nLasted {PlayTime()}\nLooted {Looted()}\n\nPress Interact Key\nto continue",
            GameSettings.Instance.Viewport / 2,
            Color.WithAlpha(Color.White, _textFade),
            TextAlignment.Center,
            Vect2.One,
            1f
        );

        _batch.End();

        base.Draw(frameTime);
    }

    /// <summary>
    /// Formats the play time for display.
    /// </summary>
    private string PlayTime()
    {
        var t = TimeSpan.FromSeconds(_time);

        if (t.Minutes > 0)
            return $"{t.Minutes:0}:{t.Seconds:00} minutes";

        return $"{t.Seconds:0} seconds";
    }

    /// <summary>
    /// Formats the looted food count for display.
    /// </summary>
    private string Looted()
        => $"{_looted} food";

    /// <summary>
    /// Fades in the game over screen.
    /// </summary>
    private IEnumerator TransitionIn()
    {
        // Fade in the overlay (black screen)
        yield return Globals.FadeInOut(0f, 1f, 0.8f, v => _fade = v);

        // Fade in the text (appears after the overlay)
        yield return Globals.FadeInOut(0f, 1f, 1.2f, v => _textFade = v);

        // Reset the game data for the next playthrough
        Globals.Data = new GameData
        {
            Food = Globals.DefaultStartingFruit,
            PlayTime = 0,
            Looted = 0
        };

        _isReady = true;
    }

    /// <summary>
    /// Fades out the game over screen and starts a new game.
    /// </summary>
    private IEnumerator TransitionOut()
    {
        var sm = SceneManager.Instance;

        // ========================================================================
        // Phase 1: Remove all old scenes (except this one)
        // ========================================================================
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

        // ========================================================================
        // Phase 2: Add the new game scene
        // ========================================================================
        sm.Add(new SceneGame());

        // ========================================================================
        // Phase 3: Fade out the game over screen
        // ========================================================================
        var r1 = CoroutineManager.Instance.Run(Globals.FadeInOut(1f, 0f, 0.8f, v => _fade = v));
        var r2 = CoroutineManager.Instance.Run(Globals.FadeInOut(1f, 0f, 0.8f, v => _textFade = v));

        yield return new WaitWhile(() => r1.IsRunning || r2.IsRunning);

        // ========================================================================
        // Phase 4: Exit
        // ========================================================================
        ExitScene();
    }
}