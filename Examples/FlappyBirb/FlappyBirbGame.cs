// ============================================================================
//  FlappyBirbGame.cs
// ============================================================================
//  This is an example game built with the Void Engine.
//
//  Feel free to use, modify, and learn from this code however you like.
//  It's meant to showcase the engine's capabilities and serve as a
//  starting point for your own projects.
//
//  Showcasing:
//    - Sprite rendering with rotation and depth sorting
//    - Keyboard and mouse input handling
//    - Rect2 collision detection
//    - Parallax scrolling backgrounds
//    - Text drawing with alignment
//    - Game state management (playing, game over, restart)
//
//  Copyright (c) 2025 Void Engine Examples
//  Licensed under the MIT License.
//  See LICENSE file in the project root for full license information.
// ============================================================================

using Void.Engine.Graphics.RenderTargets;
using Void.Engine.Graphics.Shaders;

namespace FlappyBirb;

/// <summary>
/// Main game class. Entry point for the FlappyBirb example.
/// </summary>
public sealed class FlappyBirbGame(GameSettings settings) : Game(settings)
{
    private const float PipeSpawnTime = 3.5f;

    // Parallax positions for background (slow) and ground (fast)
    private readonly float[] _bgParallax = [0, 144];
    private readonly float[] _groundParallax = [0, 154];

    // Active pipes and pipes waiting to be removed
    private readonly List<Pipe> _pipes = [];
    private readonly List<Pipe> _pipesRemove = [];

    // Timers
    private float _pipeDelay;      // Time until next pipe spawns
    private float _restartDelay;   // Delay before accepting restart input

    // Sprite source rectangles
    private Rect2 _bgRect;
    private Rect2 _groundRect;

    // Rendering and game objects
    private SpriteBatcher _batch;
    private Camera _camera;
    private Birb _birb;

    // Game state
    private int _score;
    private bool _gameOver;

    protected override void OnEnter()
    {
        // Load the asset pack once at startup
        var mount = AssetManager.Instance.LoadPack("GameAssets.pack");
        AssetManager.Instance.AddMountToStart(mount);

        // Load all assets from the spritesheet
        Globals.Texture = AssetManager.Instance.Load<Texture>("Spritesheet.png");
        Globals.Font = AssetManager.Instance.LoadSpriteFont("Fonts/FontOutline.png", -2);
        Globals.Sheet = AssetManager.Instance.Load<Spritesheet>("Spritesheet.sheet");

        // Get source rectangles for background and ground sprites
        _bgRect = Globals.Sheet.GetBound("Background");
        _groundRect = Globals.Sheet.GetBound("Ground");

        // Create batcher, camera, and bird
        _batch = new SpriteBatcher();
        _camera = new Camera();
        _birb = new Birb(new(30, 60));

        base.OnEnter();
    }

    protected override void OnUpdate(FrameTime frameTime)
    {
        // Game over state - wait for restart input
        if (_gameOver)
        {
            // Small delay so the player doesn't accidentally restart
            _restartDelay += frameTime.DeltaTime;

            if (_restartDelay > 0.5f)
            {
                var keyboard = Keyboard.GetState();
                var mouse = Mouse.GetState();

                // Space or left click to restart
                if (keyboard.IsKeyDown(KeyboardKey.Space) || mouse.IsButtonPressed(MouseButton.Left))
                    RestartGame();
            }

            base.OnUpdate(frameTime);
            return;
        }

        // Scroll background (slow parallax)
        for (int i = 0; i < _bgParallax.Length; i++)
        {
            _bgParallax[i] -= frameTime.DeltaTime * Globals.BackgroundSpeed;

            // Wrap around when off screen
            if ((_bgParallax[i] + _bgRect.Width) < 0)
                _bgParallax[i] += _bgRect.Width * 2f;
        }

        // Scroll ground (fast parallax)
        for (int i = 0; i < _groundParallax.Length; i++)
        {
            _groundParallax[i] -= frameTime.DeltaTime * Globals.GroundSpeed;

            if ((_groundParallax[i] + _groundRect.Width) < 0)
                _groundParallax[i] += _groundRect.Width * 2f;
        }

        // Move pipes and mark off-screen ones for removal
        foreach (var pipe in _pipes)
        {
            pipe.Update(frameTime);

            if (pipe.IsOffScreen)
                _pipesRemove.Add(pipe);
        }

        // Clean up off-screen pipes
        foreach (var pipe in _pipesRemove)
            _pipes.Remove(pipe);
        _pipesRemove.Clear();

        // Update bird physics and animation
        _birb.Update(frameTime);

        // Check pipe collisions and scoring
        foreach (var pipe in _pipes)
        {
            if (pipe.CollidesWith(_birb.CollisionRect))
            {
                _birb.Die();
                _gameOver = true;
                break;
            }

            // Score when bird passes a pipe
            if (pipe.CheckPassed(_birb.Position.X))
                _score++;
        }

        // Check if bird hit the ground or ceiling
        if (_birb.IsOutOfBounds(_bgRect.Height - _groundRect.Height))
        {
            _birb.Die();
            _gameOver = true;
        }

        // Spawn new pipes on a timer
        if (_pipeDelay > PipeSpawnTime)
        {
            // Random Y position for the pipe gap
            var range = FastRandom.Shared.RangeFloat(50, 150);
            _pipes.Add(new Pipe(new(_bgRect.Width + 60, range)));
            _pipeDelay -= PipeSpawnTime;
        }
        else
        {
            _pipeDelay += frameTime.DeltaTime;
        }

        base.OnUpdate(frameTime);
    }

    protected override void OnDraw(FrameTime frameTime)
    {
        _batch.Begin(SortMode.BackToFront, BlendMode.Alpha, _camera);

        // Draw scrolling background
        for (int i = 0; i < _bgParallax.Length; i++)
            _batch.Draw(Globals.Texture, new Vect2(_bgParallax[i], 0), _bgRect, Color.White, 0f);

        // Draw pipes (behind bird, in front of background)
        foreach (var pipe in _pipes)
            pipe.Draw(_batch);

        // Draw scrolling ground (in front of everything)
        for (int i = 0; i < _groundParallax.Length; i++)
        {
            var pos = new Vect2(_groundParallax[i], _bgRect.Height - _groundRect.Height);
            _batch.Draw(Globals.Texture, pos, _groundRect, Color.White, 1f);
        }

        // Draw the bird with rotation
        _birb.Draw(_batch);

        // Draw score centered at the top
        _batch.DrawText(Globals.Font, _score.ToString(), new Vect2(_bgRect.Width / 2f, 10), Color.White, TextAlignment.TopCenter, Vect2.One, 1f);

        // Draw game over message when dead
        if (_gameOver)
        {
            _batch.DrawText(Globals.Font, "Game Over!\n\nPress Space\nto restart", new Vect2(_bgRect.Width / 2f, _bgRect.Height / 2f), Color.White, TextAlignment.Center, Vect2.One, 1f);
        }

        _batch.End();

        base.OnDraw(frameTime);
    }

    /// <summary>
    /// Resets the game to a fresh state.
    /// </summary>
    private void RestartGame()
    {
        _pipes.Clear();
        _pipeDelay = 0f;
        _restartDelay = 0f;
        _score = 0;
        _gameOver = false;
        _birb.Reset(new(30, 60));
    }
}