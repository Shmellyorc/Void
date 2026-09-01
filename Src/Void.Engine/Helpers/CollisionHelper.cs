namespace Void.Engine.Helpers;

/// <summary>
/// Provides comprehensive collision detection and resolution for 2D games using <see cref="Rect2"/> and <see cref="Vect2"/>.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="CollisionHelper"/> class offers a complete suite of collision utilities built on public primitive methods:
/// <list type="bullet">
///   <item><description><b>Primitives:</b> Distance checks, closest point calculations, line intersection</description></item>
///   <item><description><b>Detection:</b> Rect-Rect, Circle-Circle, Rect-Circle, Point-Rect, Point-Circle, Line-Rect, Line-Circle, Line-Line</description></item>
///   <item><description><b>Containment:</b> Check if one shape fully contains another</description></item>
///   <item><description><b>Distance:</b> Calculate distance between shapes</description></item>
///   <item><description><b>Closest Point:</b> Find the nearest point on a shape to a given point</description></item>
///   <item><description><b>Raycasting:</b> Cast rays against rectangles and circles with hit point, normal, and distance</description></item>
///   <item><description><b>Swept Collision:</b> Detect collisions for fast-moving objects to prevent tunneling</description></item>
///   <item><description><b>Collision Normals:</b> Get the direction of impact for collision resolution</description></item>
///   <item><description><b>Reflection:</b> Bounce velocities off surfaces</description></item>
///   <item><description><b>Pushback/Resolution:</b> Resolve overlaps by pushing objects out of each other</description></item>
///   <item><description><b>Move &amp; Slide:</b> Move objects with automatic collision resolution against obstacles</description></item>
///   <item><description><b>Bounds Conversion:</b> Convert shapes to bounding boxes for broadphase optimization</description></item>
/// </list>
/// </para>
/// <para>
/// All methods use <see cref="Rect2"/> for rectangles and <see cref="Vect2"/> with a <c>float</c> radius for circles — no separate <c>Circle</c> struct is required.
/// </para>
/// <para>
/// <b>Basic Collision Detection:</b>
/// <code>
/// // Create shapes
/// Rect2 player = new Rect2(100, 100, 32, 32);
/// Rect2 wall = new Rect2(200, 100, 64, 64);
/// Vect2 circleCenter = new Vect2(300, 300);
/// float circleRadius = 20f;
/// 
/// // Check for overlaps
/// if (CollisionHelper.RectRect(player, wall))
/// {
///     Console.WriteLine("Player hit the wall!");
/// }
/// 
/// if (CollisionHelper.RectCircle(wall, circleCenter, circleRadius))
/// {
///     Console.WriteLine("Circle overlaps the wall!");
/// }
/// </code>
/// </para>
/// <para>
/// <b>Collision Resolution:</b>
/// <code>
/// // Resolve rectangle overlap
/// if (CollisionHelper.RectRect(player, wall))
/// {
///     Vect2 push = CollisionHelper.PushRectRect(player, wall);
///     player.Position += push;
/// }
/// 
/// // Get collision normal for sliding/bouncing
/// Vect2 normal = CollisionHelper.GetCollisionNormal(player, wall);
/// Vect2 velocity = new Vect2(5, 0);
/// Vect2 reflected = CollisionHelper.Reflect(velocity, normal, 0.8f); // 80% bounciness
/// </code>
/// </para>
/// <para>
/// <b>Raycasting:</b>
/// <code>
/// // Raycast from player to mouse
/// Vect2 mousePos = new Vect2(400, 200);
/// Vect2 direction = (mousePos - player.Center).Normalized();
/// 
/// if (CollisionHelper.RaycastRect(player.Center, direction, wall, out Vect2 hit, out float distance))
/// {
///     Console.WriteLine($"Hit wall at {hit}, distance: {distance}");
/// }
/// </code>
/// </para>
/// <para>
/// <b>Move with Collision Resolution:</b>
/// <code>
/// // Move player with slide along walls
/// Vect2 velocity = new Vect2(5, 0);
/// List&lt;Rect2&gt; obstacles = new List&lt;Rect2&gt; { wall };
/// player.Position = CollisionHelper.MoveAndSlideRect(player, velocity, obstacles);
/// 
/// // Move circle with collision against rects and circles
/// Vect2 circlePos = new Vect2(150, 150);
/// float circleRadius = 16f;
/// List&lt;(Vect2 center, float radius)&gt; circleObstacles = new()
/// {
///     (new Vect2(250, 250), 20f)
/// };
/// circlePos = CollisionHelper.MoveAndSlideCircle(circlePos, circleRadius, velocity, obstacles, circleObstacles);
/// </code>
/// </para>
/// <para>
/// <b>Swept Collision (prevents tunneling):</b>
/// <code>
/// // Fast-moving bullet
/// Rect2 bullet = new Rect2(100, 100, 4, 4);
/// Vect2 bulletVelocity = new Vect2(1000, 0); // Very fast!
/// 
/// if (CollisionHelper.SweptRectRect(bullet, bulletVelocity, wall, 
///     out float timeOfImpact, out Vect2 hitPoint, out Vect2 hitNormal))
/// {
///     // Bullet will hit wall at timeOfImpact (0-1)
///     Console.WriteLine($"Bullet will hit at time {timeOfImpact}, point {hitPoint}");
/// }
/// </code>
/// </para>
/// <para>
/// <b>Distance Checks (for AI/proximity):</b>
/// <code>
/// // Check if enemy is close to player
/// Rect2 enemy = new Rect2(300, 100, 32, 32);
/// float distanceToPlayer = CollisionHelper.DistanceRectRect(enemy, player);
/// 
/// if (distanceToPlayer &lt; 100f)
/// {
///     Console.WriteLine("Enemy is within 100 pixels of player!");
/// }
/// </code>
/// </para>
/// <para>
/// <b>Bounds Conversion (for broadphase):</b>
/// <code>
/// // Convert circle to AABB for broadphase check
/// Vect2 circlePos = new Vect2(400, 300);
/// float circleRadius = 25f;
/// Rect2 circleBounds = CollisionHelper.GetCircleBounds(circlePos, circleRadius);
/// 
/// // Quick broadphase check
/// if (CollisionHelper.RectRect(circleBounds, wall))
/// {
///     // Do precise circle-rect check
///     if (CollisionHelper.RectCircle(wall, circlePos, circleRadius))
///     {
///         Console.WriteLine("Circle actually hit the wall!");
///     }
/// }
/// </code>
/// </para>
/// </remarks>
public static class CollisionHelper
{
    #region Public Primitives
    /// <summary>
    /// Checks if a point is within a specified radius of another point.
    /// This is the fundamental distance check used by all circle and point collisions.
    /// </summary>
    /// <param name="point">The point to check.</param>
    /// <param name="center">The center point.</param>
    /// <param name="radius">The radius to check within.</param>
    /// <returns><see langword="true"/> if the point is within the radius; otherwise, <see langword="false"/>.</returns>
    public static bool IsWithinRadius(Vect2 point, Vect2 center, float radius)
        => Vect2.DistanceSquared(point, center) <= radius * radius;

    /// <summary>
    /// Checks if two circles overlap based on their centers and combined radius.
    /// </summary>
    /// <param name="centerA">Center of the first circle.</param>
    /// <param name="radiusA">Radius of the first circle.</param>
    /// <param name="centerB">Center of the second circle.</param>
    /// <param name="radiusB">Radius of the second circle.</param>
    /// <returns><see langword="true"/> if the circles overlap; otherwise, <see langword="false"/>.</returns>
    public static bool IsCircleOverlap(Vect2 centerA, float radiusA, Vect2 centerB, float radiusB)
        => IsWithinRadius(centerA, centerB, radiusA + radiusB);

    /// <summary>
    /// Gets the closest point on a rectangle to a given point.
    /// </summary>
    /// <param name="point">The point to find the closest point to.</param>
    /// <param name="rect">The rectangle.</param>
    /// <returns>The closest point on the rectangle to the given point.</returns>
    public static Vect2 ClosestPointRect(Vect2 point, Rect2 rect)
        => point.Clamp(rect.TopLeft, rect.BottomRight);

    /// <summary>
    /// Gets the closest point on a circle to a given point.
    /// </summary>
    /// <param name="point">The point to find the closest point to.</param>
    /// <param name="center">The center of the circle.</param>
    /// <param name="radius">The radius of the circle.</param>
    /// <returns>The closest point on the circle to the given point.</returns>
    public static Vect2 ClosestPointCircle(Vect2 point, Vect2 center, float radius)
    {
        Vect2 direction = (point - center).Normalized();
        return center + direction * radius;
    }

    /// <summary>
    /// Checks if two line segments intersect.
    /// </summary>
    /// <param name="a1">Start point of the first line.</param>
    /// <param name="a2">End point of the first line.</param>
    /// <param name="b1">Start point of the second line.</param>
    /// <param name="b2">End point of the second line.</param>
    /// <returns><see langword="true"/> if the line segments intersect; otherwise, <see langword="false"/>.</returns>
    public static bool LineLine(Vect2 a1, Vect2 a2, Vect2 b1, Vect2 b2)
    {
        float d1 = Cross(b1 - a1, a2 - a1);
        float d2 = Cross(b2 - a1, a2 - a1);
        float d3 = Cross(a1 - b1, b2 - b1);
        float d4 = Cross(a2 - b1, b2 - b1);

        return (d1 > 0 && d2 < 0 || d1 < 0 && d2 > 0) &&
               (d3 > 0 && d4 < 0 || d3 < 0 && d4 > 0);
    }
    #endregion



    #region Point <---> Shape
    /// <summary>
    /// Checks if a point is inside a rectangle.
    /// </summary>
    /// <param name="point">The point to check.</param>
    /// <param name="rect">The rectangle.</param>
    /// <returns><see langword="true"/> if the point is inside the rectangle; otherwise, <see langword="false"/>.</returns>
    public static bool PointRect(Vect2 point, Rect2 rect)
        => point.X >= rect.Left && point.X <= rect.Right &&
           point.Y >= rect.Top && point.Y <= rect.Bottom;

    /// <summary>
    /// Checks if a point is inside a circle.
    /// </summary>
    /// <param name="point">The point to check.</param>
    /// <param name="center">The center of the circle.</param>
    /// <param name="radius">The radius of the circle.</param>
    /// <returns><see langword="true"/> if the point is inside the circle; otherwise, <see langword="false"/>.</returns>
    public static bool PointCircle(Vect2 point, Vect2 center, float radius)
        => IsWithinRadius(point, center, radius);
    #endregion



    #region Shape <---> Shape
    /// <summary>
    /// Checks if two rectangles overlap.
    /// </summary>
    /// <param name="a">First rectangle.</param>
    /// <param name="b">Second rectangle.</param>
    /// <returns><see langword="true"/> if the rectangles overlap; otherwise, <see langword="false"/>.</returns>
    public static bool RectRect(Rect2 a, Rect2 b)
        => a.Left < b.Right && a.Right > b.Left &&
           a.Top < b.Bottom && a.Bottom > b.Top;

    /// <summary>
    /// Checks if two circles overlap.
    /// </summary>
    /// <param name="centerA">Center of the first circle.</param>
    /// <param name="radiusA">Radius of the first circle.</param>
    /// <param name="centerB">Center of the second circle.</param>
    /// <param name="radiusB">Radius of the second circle.</param>
    /// <returns><see langword="true"/> if the circles overlap; otherwise, <see langword="false"/>.</returns>
    public static bool CircleCircle(Vect2 centerA, float radiusA, Vect2 centerB, float radiusB)
        => IsCircleOverlap(centerA, radiusA, centerB, radiusB);

    /// <summary>
    /// Checks if a rectangle and a circle overlap.
    /// </summary>
    /// <param name="rect">The rectangle.</param>
    /// <param name="center">The center of the circle.</param>
    /// <param name="radius">The radius of the circle.</param>
    /// <returns><see langword="true"/> if the rectangle and circle overlap; otherwise, <see langword="false"/>.</returns>
    public static bool RectCircle(Rect2 rect, Vect2 center, float radius)
    {
        Vect2 closest = ClosestPointRect(center, rect);
        return IsWithinRadius(center, closest, radius);
    }
    #endregion



    #region Line <---> Shape
    /// <summary>
    /// Computes the 2D cross product of two vectors.
    /// </summary>
    private static float Cross(Vect2 a, Vect2 b)
        => a.X * b.Y - a.Y * b.X;

    /// <summary>
    /// Checks if a line segment intersects a rectangle.
    /// </summary>
    /// <param name="start">Start point of the line segment.</param>
    /// <param name="end">End point of the line segment.</param>
    /// <param name="rect">The rectangle.</param>
    /// <returns><see langword="true"/> if the line segment intersects the rectangle; otherwise, <see langword="false"/>.</returns>
    public static bool LineRect(Vect2 start, Vect2 end, Rect2 rect)
    {
        if (PointRect(start, rect) || PointRect(end, rect))
            return true;

        return LineLine(start, end, rect.TopLeft, rect.TopRight) ||
               LineLine(start, end, rect.TopRight, rect.BottomRight) ||
               LineLine(start, end, rect.BottomRight, rect.BottomLeft) ||
               LineLine(start, end, rect.BottomLeft, rect.TopLeft);
    }

    /// <summary>
    /// Checks if a line segment intersects a circle.
    /// </summary>
    /// <param name="start">Start point of the line segment.</param>
    /// <param name="end">End point of the line segment.</param>
    /// <param name="center">The center of the circle.</param>
    /// <param name="radius">The radius of the circle.</param>
    /// <returns><see langword="true"/> if the line segment intersects the circle; otherwise, <see langword="false"/>.</returns>
    public static bool LineCircle(Vect2 start, Vect2 end, Vect2 center, float radius)
    {
        Vect2 d = end - start;
        Vect2 f = start - center;

        float a = Vect2.Dot(d, d);
        float b = 2 * Vect2.Dot(f, d);
        float c = Vect2.Dot(f, f) - radius * radius;

        float discriminant = b * b - 4 * a * c;

        if (discriminant < 0)
            return false;

        discriminant = MathF.Sqrt(discriminant);

        float t1 = (-b - discriminant) / (2 * a);
        float t2 = (-b + discriminant) / (2 * a);

        return (t1 >= 0 && t1 <= 1) || (t2 >= 0 && t2 <= 1);
    }
    #endregion



    #region Distance Between Shapes
    /// <summary>
    /// Calculates the minimum distance between two rectangles.
    /// Returns 0 if the rectangles overlap.
    /// </summary>
    /// <param name="a">First rectangle.</param>
    /// <param name="b">Second rectangle.</param>
    /// <returns>The minimum distance between the rectangles, or 0 if they overlap.</returns>
    public static float DistanceRectRect(Rect2 a, Rect2 b)
    {
        if (RectRect(a, b))
            return 0f;

        float dx = MathF.Max(0f, MathF.Max(a.Left - b.Right, b.Left - a.Right));
        float dy = MathF.Max(0f, MathF.Max(a.Top - b.Bottom, b.Top - a.Bottom));
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Calculates the minimum distance between two circles.
    /// Returns 0 if the circles overlap.
    /// </summary>
    /// <param name="centerA">Center of the first circle.</param>
    /// <param name="radiusA">Radius of the first circle.</param>
    /// <param name="centerB">Center of the second circle.</param>
    /// <param name="radiusB">Radius of the second circle.</param>
    /// <returns>The minimum distance between the circles, or 0 if they overlap.</returns>
    public static float DistanceCircleCircle(Vect2 centerA, float radiusA, Vect2 centerB, float radiusB)
    {
        float distance = Vect2.Distance(centerA, centerB);
        float radiiSum = radiusA + radiusB;
        return MathF.Max(0f, distance - radiiSum);
    }

    /// <summary>
    /// Calculates the minimum distance between a rectangle and a circle.
    /// Returns 0 if they overlap.
    /// </summary>
    /// <param name="rect">The rectangle.</param>
    /// <param name="center">The center of the circle.</param>
    /// <param name="radius">The radius of the circle.</param>
    /// <returns>The minimum distance between the rectangle and circle, or 0 if they overlap.</returns>
    public static float DistanceRectCircle(Rect2 rect, Vect2 center, float radius)
    {
        Vect2 closest = ClosestPointRect(center, rect);
        float distance = Vect2.Distance(center, closest);
        return MathF.Max(0f, distance - radius);
    }
    #endregion



    #region Raycast
    /// <summary>
    /// Casts a ray and returns the first hit against a rectangle.
    /// </summary>
    /// <param name="origin">The origin of the ray.</param>
    /// <param name="direction">The direction of the ray (must be normalized).</param>
    /// <param name="rect">The rectangle to test against.</param>
    /// <param name="hitPoint">The point where the ray hits the rectangle.</param>
    /// <param name="distance">The distance from the origin to the hit point.</param>
    /// <returns><see langword="true"/> if the ray hits the rectangle; otherwise, <see langword="false"/>.</returns>
    public static bool RaycastRect(Vect2 origin, Vect2 direction, Rect2 rect, out Vect2 hitPoint, out float distance)
    {
        hitPoint = Vect2.Zero;
        distance = float.MaxValue;

        Vect2 invDir = new(1f / direction.X, 1f / direction.Y);

        float t1 = (rect.Left - origin.X) * invDir.X;
        float t2 = (rect.Right - origin.X) * invDir.X;
        float t3 = (rect.Top - origin.Y) * invDir.Y;
        float t4 = (rect.Bottom - origin.Y) * invDir.Y;

        float tMin = MathF.Max(MathF.Min(t1, t2), MathF.Min(t3, t4));
        float tMax = MathF.Min(MathF.Max(t1, t2), MathF.Max(t3, t4));

        if (tMax < 0 || tMin > tMax)
            return false;

        distance = tMin > 0 ? tMin : 0;
        hitPoint = origin + direction * distance;
        return true;
    }

    /// <summary>
    /// Casts a ray and returns the first hit against a circle.
    /// </summary>
    /// <param name="origin">The origin of the ray.</param>
    /// <param name="direction">The direction of the ray (must be normalized).</param>
    /// <param name="center">The center of the circle.</param>
    /// <param name="radius">The radius of the circle.</param>
    /// <param name="hitPoint">The point where the ray hits the circle.</param>
    /// <param name="distance">The distance from the origin to the hit point.</param>
    /// <returns><see langword="true"/> if the ray hits the circle; otherwise, <see langword="false"/>.</returns>
    public static bool RaycastCircle(Vect2 origin, Vect2 direction, Vect2 center, float radius, out Vect2 hitPoint, out float distance)
    {
        hitPoint = Vect2.Zero;
        distance = float.MaxValue;

        Vect2 oc = origin - center;
        float a = Vect2.Dot(direction, direction);
        float b = 2f * Vect2.Dot(oc, direction);
        float c = Vect2.Dot(oc, oc) - radius * radius;

        float discriminant = b * b - 4f * a * c;

        if (discriminant < 0)
            return false;

        float t = (-b - MathF.Sqrt(discriminant)) / (2f * a);

        if (t < 0)
            return false;

        distance = t;
        hitPoint = origin + direction * t;
        return true;
    }

    /// <summary>
    /// Casts a ray and returns the first hit against any collision shape.
    /// </summary>
    /// <param name="origin">The origin of the ray.</param>
    /// <param name="direction">The direction of the ray (must be normalized).</param>
    /// <param name="rects">The rectangles to test against.</param>
    /// <param name="circles">The circles to test against (center, radius tuples).</param>
    /// <param name="hitPoint">The point where the ray hits the shape.</param>
    /// <param name="hitNormal">The normal at the hit point.</param>
    /// <param name="distance">The distance from the origin to the hit point.</param>
    /// <param name="hitObject">The object that was hit (either a <see cref="Rect2"/> or a <see cref="Vect2"/> center).</param>
    /// <returns><see langword="true"/> if the ray hits any shape; otherwise, <see langword="false"/>.</returns>
    public static bool RaycastAny(Vect2 origin, Vect2 direction,
        IEnumerable<Rect2> rects,
        IEnumerable<(Vect2 center, float radius)> circles,
        out Vect2 hitPoint, out Vect2 hitNormal, out float distance, out object hitObject)
    {
        hitPoint = Vect2.Zero;
        hitNormal = Vect2.Zero;
        distance = float.MaxValue;
        hitObject = null;
        bool hit = false;

        foreach (var rect in rects)
        {
            if (RaycastRect(origin, direction, rect, out Vect2 point, out float dist))
            {
                if (dist < distance)
                {
                    distance = dist;
                    hitPoint = point;
                    hitObject = rect;
                    hit = true;
                    Vect2 center = rect.Center;
                    hitNormal = (center - point).Normalized();
                }
            }
        }

        foreach (var (center, radius) in circles)
        {
            if (RaycastCircle(origin, direction, center, radius, out Vect2 point, out float dist))
            {
                if (dist < distance)
                {
                    distance = dist;
                    hitPoint = point;
                    hitObject = center;
                    hit = true;
                    hitNormal = (point - center).Normalized();
                }
            }
        }

        return hit;
    }
    #endregion



    #region Swept Collision
    /// <summary>
    /// Performs swept collision detection between a moving rectangle and a static rectangle.
    /// Prevents tunneling for fast-moving objects.
    /// </summary>
    /// <param name="moving">The moving rectangle at its starting position.</param>
    /// <param name="velocity">The movement vector for this frame.</param>
    /// <param name="obstacle">The static rectangle to test against.</param>
    /// <param name="timeOfImpact">The normalized time (0-1) along the velocity vector when the collision occurs.</param>
    /// <param name="hitPoint">The point of impact.</param>
    /// <param name="hitNormal">The normal at the point of impact.</param>
    /// <returns><see langword="true"/> if a collision will occur during the movement; otherwise, <see langword="false"/>.</returns>
    public static bool SweptRectRect(Rect2 moving, Vect2 velocity, Rect2 obstacle, 
        out float timeOfImpact, out Vect2 hitPoint, out Vect2 hitNormal)
    {
        timeOfImpact = 1f;
        hitPoint = Vect2.Zero;
        hitNormal = Vect2.Zero;

        // Check if already overlapping
        if (RectRect(moving, obstacle))
        {
            timeOfImpact = 0f;
            hitNormal = GetCollisionNormal(moving, obstacle);
            hitPoint = moving.Center;
            return true;
        }

        // Expanded obstacle (Minkowski sum)
        Rect2 expandedObstacle = obstacle.Inflate(moving.Width / 2f, moving.Height / 2f);

        Vect2 movingCenter = moving.Center;

        // Raycast from moving center against expanded obstacle
        if (RaycastRect(movingCenter, velocity.Normalized(), expandedObstacle, out Vect2 point, out float distance))
        {
            float velocityLength = velocity.Length();
            
            if (distance <= velocityLength)
            {
                timeOfImpact = distance / velocityLength;
                hitPoint = point;
                
                // Calculate normal based on which side was hit
                if (MathF.Abs(point.X - expandedObstacle.Left) < 0.001f)
                    hitNormal = new Vect2(-1, 0);
                else if (MathF.Abs(point.X - expandedObstacle.Right) < 0.001f)
                    hitNormal = new Vect2(1, 0);
                else if (MathF.Abs(point.Y - expandedObstacle.Top) < 0.001f)
                    hitNormal = new Vect2(0, -1);
                else if (MathF.Abs(point.Y - expandedObstacle.Bottom) < 0.001f)
                    hitNormal = new Vect2(0, 1);
                
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Performs swept collision detection between a moving circle and a static rectangle.
    /// Prevents tunneling for fast-moving objects.
    /// </summary>
    /// <param name="center">The center of the moving circle.</param>
    /// <param name="radius">The radius of the moving circle.</param>
    /// <param name="velocity">The movement vector for this frame.</param>
    /// <param name="obstacle">The static rectangle to test against.</param>
    /// <param name="timeOfImpact">The normalized time (0-1) along the velocity vector when the collision occurs.</param>
    /// <param name="hitPoint">The point of impact.</param>
    /// <param name="hitNormal">The normal at the point of impact.</param>
    /// <returns><see langword="true"/> if a collision will occur during the movement; otherwise, <see langword="false"/>.</returns>
    public static bool SweptCircleRect(Vect2 center, float radius, Vect2 velocity, Rect2 obstacle,
        out float timeOfImpact, out Vect2 hitPoint, out Vect2 hitNormal)
    {
        timeOfImpact = 1f;
        hitPoint = Vect2.Zero;
        hitNormal = Vect2.Zero;

        // Check if already overlapping
        if (RectCircle(obstacle, center, radius))
        {
            timeOfImpact = 0f;
            hitNormal = GetCollisionNormal(center, radius, obstacle);
            hitPoint = center;
            return true;
        }

        // Expand obstacle by circle radius
        Rect2 expandedObstacle = obstacle.Inflate(radius);

        // Raycast from circle center against expanded obstacle
        if (RaycastRect(center, velocity.Normalized(), expandedObstacle, out Vect2 point, out float distance))
        {
            float velocityLength = velocity.Length();
            
            if (distance <= velocityLength)
            {
                timeOfImpact = distance / velocityLength;
                hitPoint = point;
                
                // Calculate normal based on which side was hit
                if (MathF.Abs(point.X - expandedObstacle.Left) < 0.001f)
                    hitNormal = new Vect2(-1, 0);
                else if (MathF.Abs(point.X - expandedObstacle.Right) < 0.001f)
                    hitNormal = new Vect2(1, 0);
                else if (MathF.Abs(point.Y - expandedObstacle.Top) < 0.001f)
                    hitNormal = new Vect2(0, -1);
                else if (MathF.Abs(point.Y - expandedObstacle.Bottom) < 0.001f)
                    hitNormal = new Vect2(0, 1);
                
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Performs swept collision detection between a moving circle and a static circle.
    /// Prevents tunneling for fast-moving objects.
    /// </summary>
    /// <param name="centerA">The center of the moving circle.</param>
    /// <param name="radiusA">The radius of the moving circle.</param>
    /// <param name="velocity">The movement vector for this frame.</param>
    /// <param name="centerB">The center of the static circle.</param>
    /// <param name="radiusB">The radius of the static circle.</param>
    /// <param name="timeOfImpact">The normalized time (0-1) along the velocity vector when the collision occurs.</param>
    /// <param name="hitPoint">The point of impact.</param>
    /// <param name="hitNormal">The normal at the point of impact.</param>
    /// <returns><see langword="true"/> if a collision will occur during the movement; otherwise, <see langword="false"/>.</returns>
    public static bool SweptCircleCircle(Vect2 centerA, float radiusA, Vect2 velocity, 
        Vect2 centerB, float radiusB,
        out float timeOfImpact, out Vect2 hitPoint, out Vect2 hitNormal)
    {
        timeOfImpact = 1f;
        hitPoint = Vect2.Zero;
        hitNormal = Vect2.Zero;

        // Check if already overlapping
        if (CircleCircle(centerA, radiusA, centerB, radiusB))
        {
            timeOfImpact = 0f;
            hitNormal = (centerA - centerB).Normalized();
            hitPoint = centerA;
            return true;
        }

        float combinedRadius = radiusA + radiusB;

        // Raycast from centerA against centerB with combined radius
        if (RaycastCircle(centerA, velocity.Normalized(), centerB, combinedRadius, out Vect2 point, out float distance))
        {
            float velocityLength = velocity.Length();
            
            if (distance <= velocityLength)
            {
                timeOfImpact = distance / velocityLength;
                hitPoint = point;
                hitNormal = (point - centerB).Normalized();
                return true;
            }
        }

        return false;
    }
    #endregion



    #region Collision Normals
    /// <summary>
    /// Gets the collision normal between two overlapping rectangles.
    /// The normal points from the obstacle to the moving rectangle.
    /// </summary>
    /// <param name="moving">The moving rectangle.</param>
    /// <param name="obstacle">The obstacle rectangle.</param>
    /// <returns>The collision normal, or <see cref="Vect2.Zero"/> if no collision.</returns>
    public static Vect2 GetCollisionNormal(Rect2 moving, Rect2 obstacle)
    {
        if (!RectRect(moving, obstacle))
            return Vect2.Zero;

        float overlapX = MathF.Min(moving.Right - obstacle.Left, obstacle.Right - moving.Left);
        float overlapY = MathF.Min(moving.Bottom - obstacle.Top, obstacle.Bottom - moving.Top);

        if (overlapX < overlapY)
        {
            return moving.Center.X < obstacle.Center.X ? new Vect2(-1, 0) : new Vect2(1, 0);
        }
        else
        {
            return moving.Center.Y < obstacle.Center.Y ? new Vect2(0, -1) : new Vect2(0, 1);
        }
    }

    /// <summary>
    /// Gets the collision normal between a circle and a rectangle.
    /// The normal points from the rectangle to the circle.
    /// </summary>
    /// <param name="circleCenter">The center of the circle.</param>
    /// <param name="radius">The radius of the circle.</param>
    /// <param name="rect">The rectangle.</param>
    /// <returns>The collision normal, or <see cref="Vect2.Zero"/> if no collision.</returns>
    public static Vect2 GetCollisionNormal(Vect2 circleCenter, float radius, Rect2 rect)
    {
        if (!RectCircle(rect, circleCenter, radius))
            return Vect2.Zero;

        Vect2 closest = ClosestPointRect(circleCenter, rect);
        Vect2 direction = circleCenter - closest;

        if (direction.LengthSquared() == 0)
            return new Vect2(0, -1); // Circle center is inside rectangle, push up

        return direction.Normalized();
    }

    /// <summary>
    /// Gets the collision normal between two overlapping circles.
    /// The normal points from circle B to circle A.
    /// </summary>
    /// <param name="centerA">The center of the first circle.</param>
    /// <param name="centerB">The center of the second circle.</param>
    /// <returns>The collision normal, or <see cref="Vect2.Zero"/> if no collision.</returns>
    public static Vect2 GetCollisionNormal(Vect2 centerA, Vect2 centerB)
    {
        if (centerA == centerB)
            return new Vect2(0, -1);

        return (centerA - centerB).Normalized();
    }
    #endregion



    #region Reflection
    /// <summary>
    /// Reflects a velocity vector off a surface normal.
    /// </summary>
    /// <param name="velocity">The incoming velocity vector.</param>
    /// <param name="normal">The surface normal (must be normalized).</param>
    /// <param name="bounciness">The bounciness factor (0 = no bounce, 1 = perfect bounce).</param>
    /// <returns>The reflected velocity vector.</returns>
    public static Vect2 Reflect(Vect2 velocity, Vect2 normal, float bounciness = 1f)
    {
        float dot = Vect2.Dot(velocity, normal);
        Vect2 reflection = velocity - 2f * dot * normal;
        return reflection * bounciness;
    }
    #endregion



    #region Contains
    /// <summary>
    /// Checks if one rectangle fully contains another rectangle.
    /// </summary>
    /// <param name="outer">The outer rectangle.</param>
    /// <param name="inner">The inner rectangle to test.</param>
    /// <returns><see langword="true"/> if <paramref name="outer"/> fully contains <paramref name="inner"/>; otherwise, <see langword="false"/>.</returns>
    public static bool RectContainsRect(Rect2 outer, Rect2 inner)
        => inner.Left >= outer.Left && inner.Right <= outer.Right &&
           inner.Top >= outer.Top && inner.Bottom <= outer.Bottom;

    /// <summary>
    /// Checks if a rectangle fully contains a circle.
    /// </summary>
    /// <param name="rect">The rectangle.</param>
    /// <param name="center">The center of the circle.</param>
    /// <param name="radius">The radius of the circle.</param>
    /// <returns><see langword="true"/> if the rectangle fully contains the circle; otherwise, <see langword="false"/>.</returns>
    public static bool RectContainsCircle(Rect2 rect, Vect2 center, float radius)
    {
        return center.X - radius >= rect.Left &&
               center.X + radius <= rect.Right &&
               center.Y - radius >= rect.Top &&
               center.Y + radius <= rect.Bottom;
    }

    /// <summary>
    /// Checks if a circle fully contains another circle.
    /// </summary>
    /// <param name="outerCenter">The center of the outer circle.</param>
    /// <param name="outerRadius">The radius of the outer circle.</param>
    /// <param name="innerCenter">The center of the inner circle.</param>
    /// <param name="innerRadius">The radius of the inner circle.</param>
    /// <returns><see langword="true"/> if the outer circle fully contains the inner circle; otherwise, <see langword="false"/>.</returns>
    public static bool CircleContainsCircle(Vect2 outerCenter, float outerRadius, Vect2 innerCenter, float innerRadius)
    {
        float dist = Vect2.Distance(outerCenter, innerCenter);
        return dist + innerRadius <= outerRadius;
    }

    /// <summary>
    /// Checks if a circle fully contains a rectangle.
    /// </summary>
    /// <param name="center">The center of the circle.</param>
    /// <param name="radius">The radius of the circle.</param>
    /// <param name="rect">The rectangle to test.</param>
    /// <returns><see langword="true"/> if the circle fully contains the rectangle; otherwise, <see langword="false"/>.</returns>
    public static bool CircleContainsRect(Vect2 center, float radius, Rect2 rect)
    {
        return PointCircle(rect.TopLeft, center, radius) &&
               PointCircle(rect.TopRight, center, radius) &&
               PointCircle(rect.BottomRight, center, radius) &&
               PointCircle(rect.BottomLeft, center, radius);
    }
    #endregion



    #region Pushback / Resolution
    /// <summary>
    /// Calculates the push vector to move a rectangle out of another rectangle.
    /// </summary>
    /// <param name="moving">The rectangle that is moving (and overlapping).</param>
    /// <param name="obstacle">The obstacle rectangle.</param>
    /// <returns>A vector representing the minimum translation needed to resolve the overlap.</returns>
    /// <remarks>
    /// The push vector will push the moving rectangle out of the obstacle on the axis with the smallest overlap.
    /// </remarks>
    public static Vect2 PushRectRect(Rect2 moving, Rect2 obstacle)
    {
        if (!RectRect(moving, obstacle))
            return Vect2.Zero;

        float overlapX = MathF.Min(moving.Right - obstacle.Left, obstacle.Right - moving.Left);
        float overlapY = MathF.Min(moving.Bottom - obstacle.Top, obstacle.Bottom - moving.Top);

        if (overlapX < overlapY)
        {
            float sign = (moving.Center.X < obstacle.Center.X) ? -1f : 1f;
            return new Vect2(sign * overlapX, 0);
        }
        else
        {
            float sign = (moving.Center.Y < obstacle.Center.Y) ? -1f : 1f;
            return new Vect2(0, sign * overlapY);
        }
    }

    /// <summary>
    /// Calculates the push vector to move a circle out of a rectangle.
    /// </summary>
    /// <param name="circleCenter">The center of the circle.</param>
    /// <param name="radius">The radius of the circle.</param>
    /// <param name="rect">The obstacle rectangle.</param>
    /// <returns>A vector representing the minimum translation needed to resolve the overlap.</returns>
    public static Vect2 PushCircleRect(Vect2 circleCenter, float radius, Rect2 rect)
    {
        if (!RectCircle(rect, circleCenter, radius))
            return Vect2.Zero;

        Vect2 closest = ClosestPointRect(circleCenter, rect);
        Vect2 direction = circleCenter - closest;

        if (direction.LengthSquared() == 0)
            return new Vect2(0, -1);

        float distance = Vect2.Distance(circleCenter, closest);
        float overlap = radius - distance;

        return direction.Normalized() * overlap;
    }

    /// <summary>
    /// Calculates the push vector to move a circle out of another circle.
    /// </summary>
    /// <param name="centerA">The center of the first circle.</param>
    /// <param name="radiusA">The radius of the first circle.</param>
    /// <param name="centerB">The center of the second circle.</param>
    /// <param name="radiusB">The radius of the second circle.</param>
    /// <returns>A vector representing the minimum translation needed to resolve the overlap.</returns>
    public static Vect2 PushCircleCircle(Vect2 centerA, float radiusA, Vect2 centerB, float radiusB)
    {
        if (!CircleCircle(centerA, radiusA, centerB, radiusB))
            return Vect2.Zero;

        Vect2 direction = centerA - centerB;
        float distance = direction.Length();

        if (distance == 0)
            return new Vect2(0, radiusA + radiusB);

        float overlap = (radiusA + radiusB) - distance;
        return direction.Normalized() * overlap;
    }
    #endregion



    #region Move & Slide
    /// <summary>
    /// Moves a rectangle with collision resolution against a list of obstacles.
    /// </summary>
    /// <param name="rect">The rectangle to move.</param>
    /// <param name="velocity">The desired movement velocity.</param>
    /// <param name="obstacles">The list of obstacle rectangles.</param>
    /// <param name="iterations">The number of resolution iterations (default: 4).</param>
    /// <returns>The new position after resolving collisions.</returns>
    /// <remarks>
    /// This method performs iterative collision resolution, making it suitable for games
    /// where objects need to slide along walls. The velocity is modified during resolution
    /// to prevent continuous collisions.
    /// </remarks>
    public static Vect2 MoveAndSlideRect(Rect2 rect, Vect2 velocity, IEnumerable<Rect2> obstacles, int iterations = 4)
    {
        Vect2 position = rect.Position;

        for (int i = 0; i < iterations; i++)
        {
            Vect2 newPos = position + velocity;
            Rect2 newRect = new(newPos, rect.Size);

            bool collided = false;

            foreach (var obstacle in obstacles)
            {
                if (RectRect(newRect, obstacle))
                {
                    Vect2 push = PushRectRect(newRect, obstacle);
                    newPos += push;
                    newRect = new(newPos, rect.Size);

                    if (push.X != 0) velocity.X = 0;
                    if (push.Y != 0) velocity.Y = 0;

                    collided = true;
                }
            }

            position = newPos;

            if (!collided)
                break;
        }

        return position;
    }

    /// <summary>
    /// Moves a circle with collision resolution against a list of obstacles.
    /// </summary>
    /// <param name="center">The center of the circle.</param>
    /// <param name="radius">The radius of the circle.</param>
    /// <param name="velocity">The desired movement velocity.</param>
    /// <param name="rects">The list of obstacle rectangles.</param>
    /// <param name="circles">The list of obstacle circles (center, radius tuples).</param>
    /// <param name="iterations">The number of resolution iterations (default: 4).</param>
    /// <returns>The new position after resolving collisions.</returns>
    /// <remarks>
    /// This method performs iterative collision resolution, making it suitable for games
    /// where objects need to slide along walls. The velocity is modified during resolution
    /// to prevent continuous collisions.
    /// </remarks>
    public static Vect2 MoveAndSlideCircle(Vect2 center, float radius, Vect2 velocity,
        IEnumerable<Rect2> rects, IEnumerable<(Vect2 center, float radius)> circles, int iterations = 4)
    {
        Vect2 position = center;

        for (int i = 0; i < iterations; i++)
        {
            Vect2 newPos = position + velocity;
            bool collided = false;

            foreach (var rect in rects)
            {
                if (RectCircle(rect, newPos, radius))
                {
                    Vect2 push = PushCircleRect(newPos, radius, rect);
                    newPos += push;
                    if (push.X != 0) velocity.X = 0;
                    if (push.Y != 0) velocity.Y = 0;
                    collided = true;
                }
            }

            foreach (var (circleCenter, circleRadius) in circles)
            {
                if (CircleCircle(newPos, radius, circleCenter, circleRadius))
                {
                    Vect2 push = PushCircleCircle(newPos, radius, circleCenter, circleRadius);
                    newPos += push;
                    if (push.X != 0) velocity.X = 0;
                    if (push.Y != 0) velocity.Y = 0;
                    collided = true;
                }
            }

            position = newPos;

            if (!collided)
                break;
        }

        return position;
    }
    #endregion



    #region Bounds Conversion
    /// <summary>
    /// Converts a circle to its bounding box (AABB).
    /// Useful for broadphase collision detection.
    /// </summary>
    /// <param name="center">The center of the circle.</param>
    /// <param name="radius">The radius of the circle.</param>
    /// <returns>The bounding box of the circle.</returns>
    public static Rect2 GetCircleBounds(Vect2 center, float radius)
        => new(center.X - radius, center.Y - radius, radius * 2f, radius * 2f);
    #endregion
}