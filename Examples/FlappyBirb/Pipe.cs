// ============================================================================
//  Pipe.cs
// ============================================================================
//  A pair of pipes (top and bottom) that scroll from right to left.
//
//  Each pipe pair has:
//    - A top pipe hanging down from above
//    - A bottom pipe sticking up from below
//    - A gap between them that the bird must fly through
//
//  The Position property represents the CENTER of the gap between the pipes.
//  The top and bottom pipes are drawn relative to that center point.
//
//  Copyright (c) 2025 Void Engine Examples
//  Licensed under the MIT License.
//  See LICENSE file in the project root for full license information.
// ============================================================================

namespace FlappyBirb;

/// <summary>
/// A pipe obstacle. Moves left and checks collision with the bird.
/// </summary>
public sealed class Pipe
{
    // Source rectangles from the spritesheet
    private readonly Rect2 _topPipeRect = Globals.Sheet.GetBounds("TopPipe");
    private readonly Rect2 _bottomPipeRect = Globals.Sheet.GetBounds("BottomPipe");

    /// <summary>
    /// The top pipe's hitbox. Hangs down from above the gap.
    /// </summary>
    public Rect2 TopCollisionRect => new(
        Position.X - _topPipeRect.Width / 2f,
        Position.Y - (_topPipeRect.Height + Globals.PipeGap / 2f),
        _topPipeRect.Width,
        _topPipeRect.Height
    );

    /// <summary>
    /// The bottom pipe's hitbox. Sticks up from below the gap.
    /// </summary>
    public Rect2 BottomCollisionRect => new(
        Position.X - _bottomPipeRect.Width / 2f,
        Position.Y + Globals.PipeGap / 2f,
        _bottomPipeRect.Width,
        _bottomPipeRect.Height
    );

    /// <summary>
    /// The safe zone between the pipes. The bird flies through here.
    /// </summary>
    public Rect2 GapRect => new(
        Position.X - _topPipeRect.Width / 2f,
        Position.Y - Globals.PipeGap / 2f,
        _topPipeRect.Width,
        Globals.PipeGap
    );

    /// <summary>
    /// True when the bird has already passed this pipe (prevents double scoring).
    /// </summary>
    public bool IsPassed { get; private set; }

    /// <summary>
    /// True when the pipe has scrolled completely off screen (ready for cleanup).
    /// </summary>
    public bool IsOffScreen => Position.X < -_topPipeRect.Width;

    /// <summary>
    /// The center of the gap between the top and bottom pipes.
    /// </summary>
    public Vect2 Position { get; set; }

    /// <summary>
    /// Creates a new pipe pair at the given gap center position.
    /// </summary>
    public Pipe(Vect2 position)
    {
        Position = position;
    }

    /// <summary>
    /// Moves the pipe left every frame. Call from your game's OnUpdate.
    /// </summary>
    public void Update(FrameTime frameTime)
    {
        // Move left at the same speed as the ground
        Position = new Vect2(
            Position.X - Globals.GroundSpeed * frameTime.DeltaTime,
            Position.Y
        );
    }

    /// <summary>
    /// Checks if the bird's hitbox touches either the top or bottom pipe.
    /// </summary>
    public bool CollidesWith(Rect2 birdRect)
    {
        return birdRect.Intersects(TopCollisionRect) || birdRect.Intersects(BottomCollisionRect);
    }

    /// <summary>
    /// Checks if the bird has flown past this pipe. Returns true only once.
    /// </summary>
    public bool CheckPassed(float birdX)
    {
        // If the bird's X position is past the pipe and we haven't scored yet
        if (!IsPassed && birdX > Position.X)
        {
            IsPassed = true;
            return true;  // Bird just passed this pipe
        }
        return false;
    }

    /// <summary>
    /// Draws both pipes at their current position.
    /// </summary>
    public void Draw(SpriteBatcher batch)
    {
        // Top pipe: hangs down from above, bottom edge is above the gap
        var top = new Vect2(
            Position.X - _topPipeRect.Width / 2f,
            Position.Y - (_topPipeRect.Height + Globals.PipeGap / 2f)
        );

        // Bottom pipe: sticks up from below, top edge is below the gap
        var bottom = new Vect2(
            Position.X - _bottomPipeRect.Width / 2f,
            Position.Y + Globals.PipeGap / 2f
        );

        // Draw both pipes at depth 0.8 (between ground at 1 and bird at 0.3)
        batch.Draw(Globals.Texture, top, _topPipeRect, Color.White, 0.8f);
        batch.Draw(Globals.Texture, bottom, _bottomPipeRect, Color.White, 0.8f);
    }
}