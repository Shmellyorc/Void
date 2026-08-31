// ============================================================================
//  SceneTransition.cs - Scavengers Demo Transition Scene
// ============================================================================
//  This scene handles the transition between levels. It displays a "Day X"
//  message, fades the screen, and loads the next level.
//
//  The demo shows how to:
//  - Create a transition scene with fade effects
//  - Use coroutines for sequenced events
//  - Run multiple coroutines concurrently
//  - Remove and add scenes during transitions
// ============================================================================

namespace Scavengers.Scenes;

/// <summary>
/// Transition scene that displays between levels.
/// Shows a "Day X" message with fade in/out effects.
/// </summary>
/// <remarks>
/// The transition flow:
/// 1. Fade in the "Day X" text
/// 2. Wait 2.5 seconds
/// 3. Remove all scenes except this one
/// 4. Add the new SceneGame
/// 5. Fade out the text and overlay
/// 6. Exit the transition scene
/// </remarks>
public sealed class SceneTransition : Scene
{
    private SpriteBatcher _batch;
    private float _fade, _textFade;
    private Camera _camera;

    /// <summary>
    /// Creates a new transition scene.
    /// Layer 1 means it renders on top of the game scene.
    /// </summary>
    public SceneTransition()
    {
        Layer = 1;
    }

    /// <summary>
    /// Called when the scene is added.
    /// Starts the transition coroutine and fades out the music.
    /// </summary>
    public override void OnEnter()
    {
        // Create a batcher and camera for the transition
        _batch = new SpriteBatcher();
        _camera = new Camera();

        // Run the transition sequence
        // Note: Both coroutines run concurrently
        CoroutineManager.Instance.Run(Transition());
        CoroutineManager.Instance.Run(Globals.FadeOutMusic());

        base.OnEnter();
    }

    /// <summary>
    /// Called when the scene is removed.
    /// Cleans up resources.
    /// </summary>
    public override void OnExit()
    {
        _batch.Dispose();

        base.OnExit();
    }

    /// <summary>
    /// Draws the transition overlay and text.
    /// </summary>
    public override void Draw(FrameTime frameTime)
    {
        _batch.Begin(SortMode.BackToFront, camera: _camera);

        // Draw the "Day X" text in the center of the screen
        // The alpha is controlled by _textFade
        _batch.DrawText(
            Globals.Font,
            $"Day {Globals.Data.Days}",
            GameSettings.Instance.Viewport / 2,
            Color.WithAlpha(Color.White, _textFade),
            TextAlignment.Center,
            Vect2.One,
            1f
        );

        // Draw a full-screen overlay with the game's clear color
        // The alpha is controlled by _fade
        _batch.DrawBypassAtlas(
            Globals.TempTexture,
            new Rect2(Vect2.Zero, GameSettings.Instance.Viewport),
            Color.WithAlpha(GameSettings.Instance.ClearColor, _fade),
            0f
        );

        _batch.End();

        base.Draw(frameTime);
    }

    /// <summary>
    /// The main transition coroutine.
    /// Handles the entire transition sequence.
    /// </summary>
    private IEnumerator Transition()
    {
        var sm = SceneManager.Instance;
        var toRemove = new List<Scene>(sm.Scenes.Count);

        // ========================================================================
        // Phase 1: Fade in
        // ========================================================================
        // Fade the overlay from 0 to 1 (fade to black)
        yield return Globals.FadeInOut(0f, 1f, 0.8f, v => _fade = v);

        // Fade the text from 0 to 1 (text appears)
        yield return Globals.FadeInOut(0f, 1f, 1.5f, v => _textFade = v);

        // ========================================================================
        // Phase 2: Wait and prepare
        // ========================================================================
        // Play a footstep sound (signifying stepping into the next level)
        SoundHelper.PlayRandom([Globals.FootStep1, Globals.FootStep2], Globals.SoundFxVolume);

        // Increment the day counter
        Globals.Data.Days++;

        // Wait 2.5 seconds so the player can read the "Day X" message
        yield return new WaitForSeconds(2.5f);

        // ========================================================================
        // Phase 3: Clean up old scenes
        // ========================================================================
        // Collect all scenes except this one
        foreach (var scene in sm.Scenes)
        {
            if (scene == this)
                continue;

            toRemove.Add(scene);
        }

        // Wait one frame to ensure the scene list is stable before modifying it
        yield return new WaitForNextFrame();

        // Remove all old scenes
        foreach (var scene in toRemove)
            SceneManager.Instance.Remove(scene);
        toRemove.Clear();

        // Wait another frame for the removal to complete
        yield return new WaitForNextFrame();

        // ========================================================================
        // Phase 4: Load the new scene
        // ========================================================================
        // Add the new game scene
        sm.Add(new SceneGame());

        // ========================================================================
        // Phase 5: Fade out
        // ========================================================================
        // Run both fades concurrently
        var r1 = CoroutineManager.Instance.Run(Globals.FadeInOut(1f, 0f, 0.8f, v => _fade = v));
        var r2 = CoroutineManager.Instance.Run(Globals.FadeInOut(1f, 0f, 0.8f, v => _textFade = v));

        // Wait for both fades to complete
        yield return new WaitWhile(() => r1.IsRunning || r2.IsRunning);

        // ========================================================================
        // Phase 6: Exit
        // ========================================================================
        // Remove the transition scene, revealing the new game scene below
        ExitScene();
    }
}