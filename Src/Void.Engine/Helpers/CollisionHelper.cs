// ============================================================================
//  CollisionHelper.cs
// ============================================================================
//  Collision detection, queries, resolution, and swept movement for 2D shapes.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Generic;

namespace Void.Engine.Helpers;

/// <summary>
/// Provides collision tests, distance queries, raycasts, swept tests, normals,
/// overlap resolution, and simple move-and-slide helpers for rectangles and circles.
/// </summary>
/// <remarks>
/// Rectangles use <see cref="Rect2"/>. Circles are represented by a center
/// <see cref="Vect2"/> and a radius. Unless otherwise documented, touching counts
/// as a hit for circle and line queries.
/// </remarks>
public static class CollisionHelper
{
    private static float EpsilonSquared => MathHelper.Epsilon * MathHelper.Epsilon;

    /// <summary>Checks whether a point is within a radius of a center point.</summary>
    public static bool IsWithinRadius(Vect2 point, Vect2 center, float radius)
        => Vect2.DistanceSquared(point, center) <= radius * radius;

    /// <summary>Checks whether two circles overlap or touch.</summary>
    public static bool IsCircleOverlap(Vect2 centerA, float radiusA, Vect2 centerB, float radiusB)
        => IsWithinRadius(centerA, centerB, radiusA + radiusB);

    /// <summary>Gets the closest point in or on a rectangle to a point.</summary>
    public static Vect2 ClosestPointRect(Vect2 point, Rect2 rect)
        => point.Clamp(rect.TopLeft, rect.BottomRight);

    /// <summary>Gets the closest point on a circle perimeter to a point.</summary>
    public static Vect2 ClosestPointCircle(Vect2 point, Vect2 center, float radius)
    {
        Vect2 direction = point - center;
        if (direction.LengthSquared() <= EpsilonSquared)
            return center + new Vect2(0f, radius);

        return center + direction.Normalized() * radius;
    }

    /// <summary>Checks whether two finite line segments intersect or overlap.</summary>
    public static bool LineLine(Vect2 a1, Vect2 a2, Vect2 b1, Vect2 b2)
        => TrySegmentIntersection(a1, a2, b1, b2, out _, out _);

    /// <summary>Checks whether a point lies inside or on a rectangle.</summary>
    public static bool PointRect(Vect2 point, Rect2 rect)
        => point.X >= rect.Left && point.X <= rect.Right &&
           point.Y >= rect.Top && point.Y <= rect.Bottom;

    /// <summary>Checks whether a point lies inside or on a circle.</summary>
    public static bool PointCircle(Vect2 point, Vect2 center, float radius)
        => IsWithinRadius(point, center, radius);

    /// <summary>Checks whether two rectangles overlap with positive area.</summary>
    public static bool RectRect(Rect2 a, Rect2 b)
        => a.Left < b.Right && a.Right > b.Left &&
           a.Top < b.Bottom && a.Bottom > b.Top;

    /// <summary>Checks whether two circles overlap or touch.</summary>
    public static bool CircleCircle(Vect2 centerA, float radiusA, Vect2 centerB, float radiusB)
        => IsCircleOverlap(centerA, radiusA, centerB, radiusB);

    /// <summary>Checks whether a rectangle and circle overlap or touch.</summary>
    public static bool RectCircle(Rect2 rect, Vect2 center, float radius)
    {
        Vect2 closest = ClosestPointRect(center, rect);
        return IsWithinRadius(center, closest, radius);
    }

    /// <summary>Checks whether a finite line segment intersects a rectangle.</summary>
    public static bool LineRect(Vect2 start, Vect2 end, Rect2 rect)
        => LineRect(start, end, rect, out _, out _);

    /// <summary>
    /// Checks whether a finite line segment intersects a rectangle and reports the
    /// closest hit to <paramref name="start"/>.
    /// </summary>
    /// <param name="start">Segment start.</param>
    /// <param name="end">Segment end.</param>
    /// <param name="rect">Rectangle to test.</param>
    /// <param name="hitPoint">Closest hit point, or zero when no hit occurs.</param>
    /// <param name="hitNormal">Outward rectangle normal at the reported hit.</param>
    /// <returns><see langword="true"/> when the segment intersects the rectangle.</returns>
    public static bool LineRect(Vect2 start, Vect2 end, Rect2 rect, out Vect2 hitPoint, out Vect2 hitNormal)
    {
        hitPoint = Vect2.Zero;
        hitNormal = Vect2.Zero;

        if (PointRect(start, rect))
        {
            hitPoint = start;
            hitNormal = GetNormalFromInside(start, rect);
            return true;
        }

        float closestT = float.PositiveInfinity;
        bool hit = false;

        TestRectEdge(start, end, rect.TopLeft, rect.TopRight, new Vect2(0f, -1f), ref hit, ref closestT, ref hitPoint, ref hitNormal);
        TestRectEdge(start, end, rect.TopRight, rect.BottomRight, new Vect2(1f, 0f), ref hit, ref closestT, ref hitPoint, ref hitNormal);
        TestRectEdge(start, end, rect.BottomRight, rect.BottomLeft, new Vect2(0f, 1f), ref hit, ref closestT, ref hitPoint, ref hitNormal);
        TestRectEdge(start, end, rect.BottomLeft, rect.TopLeft, new Vect2(-1f, 0f), ref hit, ref closestT, ref hitPoint, ref hitNormal);

        return hit;
    }

    /// <summary>Checks whether a finite line segment intersects a circle.</summary>
    public static bool LineCircle(Vect2 start, Vect2 end, Vect2 center, float radius)
    {
        Vect2 d = end - start;
        float a = Vect2.Dot(d, d);

        if (a <= EpsilonSquared)
            return PointCircle(start, center, radius);

        Vect2 f = start - center;
        float b = 2f * Vect2.Dot(f, d);
        float c = Vect2.Dot(f, f) - radius * radius;
        float discriminant = b * b - 4f * a * c;

        if (discriminant < 0f)
            return false;

        float root = MathF.Sqrt(MathF.Max(0f, discriminant));
        float denominator = 2f * a;
        float t1 = (-b - root) / denominator;
        float t2 = (-b + root) / denominator;
        return t1 is >= 0f and <= 1f || t2 is >= 0f and <= 1f;
    }

    /// <summary>Calculates the minimum distance between two rectangles.</summary>
    public static float DistanceRectRect(Rect2 a, Rect2 b)
    {
        if (RectRect(a, b))
            return 0f;

        float dx = MathF.Max(0f, MathF.Max(a.Left - b.Right, b.Left - a.Right));
        float dy = MathF.Max(0f, MathF.Max(a.Top - b.Bottom, b.Top - a.Bottom));
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>Calculates the minimum distance between two circles.</summary>
    public static float DistanceCircleCircle(Vect2 centerA, float radiusA, Vect2 centerB, float radiusB)
        => MathF.Max(0f, Vect2.Distance(centerA, centerB) - (radiusA + radiusB));

    /// <summary>Calculates the minimum distance between a rectangle and circle.</summary>
    public static float DistanceRectCircle(Rect2 rect, Vect2 center, float radius)
    {
        Vect2 closest = ClosestPointRect(center, rect);
        return MathF.Max(0f, Vect2.Distance(center, closest) - radius);
    }

    /// <summary>
    /// Casts a ray against a rectangle.
    /// </summary>
    /// <param name="origin">Ray origin.</param>
    /// <param name="direction">Ray direction. Supply a normalized vector for distance in world units.</param>
    /// <param name="rect">Rectangle to test.</param>
    /// <param name="hitPoint">First hit point.</param>
    /// <param name="distance">Ray parameter to the first hit.</param>
    /// <returns><see langword="true"/> when the ray hits the rectangle.</returns>
    public static bool RaycastRect(Vect2 origin, Vect2 direction, Rect2 rect, out Vect2 hitPoint, out float distance)
    {
        hitPoint = Vect2.Zero;
        distance = float.MaxValue;

        if (direction.LengthSquared() <= EpsilonSquared)
        {
            if (!PointRect(origin, rect))
                return false;

            hitPoint = origin;
            distance = 0f;
            return true;
        }

        float tMin = float.NegativeInfinity;
        float tMax = float.PositiveInfinity;

        if (!UpdateRaySlab(origin.X, direction.X, rect.Left, rect.Right, ref tMin, ref tMax) ||
            !UpdateRaySlab(origin.Y, direction.Y, rect.Top, rect.Bottom, ref tMin, ref tMax) ||
            tMax < 0f)
        {
            return false;
        }

        distance = MathF.Max(0f, tMin);
        hitPoint = origin + direction * distance;
        return true;
    }

    /// <summary>
    /// Casts a ray against a circle.
    /// </summary>
    /// <param name="origin">Ray origin.</param>
    /// <param name="direction">Ray direction. Supply a normalized vector for distance in world units.</param>
    /// <param name="center">Circle center.</param>
    /// <param name="radius">Circle radius.</param>
    /// <param name="hitPoint">First hit point.</param>
    /// <param name="distance">Ray parameter to the first hit.</param>
    /// <returns><see langword="true"/> when the ray hits the circle.</returns>
    public static bool RaycastCircle(Vect2 origin, Vect2 direction, Vect2 center, float radius, out Vect2 hitPoint, out float distance)
    {
        hitPoint = Vect2.Zero;
        distance = float.MaxValue;

        if (PointCircle(origin, center, radius))
        {
            hitPoint = origin;
            distance = 0f;
            return true;
        }

        float a = Vect2.Dot(direction, direction);
        if (a <= EpsilonSquared)
            return false;

        Vect2 oc = origin - center;
        float b = 2f * Vect2.Dot(oc, direction);
        float c = Vect2.Dot(oc, oc) - radius * radius;
        float discriminant = b * b - 4f * a * c;

        if (discriminant < 0f)
            return false;

        float root = MathF.Sqrt(MathF.Max(0f, discriminant));
        float denominator = 2f * a;
        float tNear = (-b - root) / denominator;
        float tFar = (-b + root) / denominator;
        float t = tNear >= 0f ? tNear : tFar;

        if (t < 0f)
            return false;

        distance = t;
        hitPoint = origin + direction * t;
        return true;
    }

    /// <summary>Returns the closest ray hit across rectangles and circles.</summary>
    public static bool RaycastAny(
        Vect2 origin,
        Vect2 direction,
        IEnumerable<Rect2> rects,
        IEnumerable<(Vect2 center, float radius)> circles,
        out Vect2 hitPoint,
        out Vect2 hitNormal,
        out float distance,
        out object hitObject)
    {
        hitPoint = Vect2.Zero;
        hitNormal = Vect2.Zero;
        distance = float.MaxValue;
        hitObject = null;
        bool hit = false;

        foreach (Rect2 rect in rects)
        {
            if (!RaycastRect(origin, direction, rect, out Vect2 point, out float dist) || dist >= distance)
                continue;

            distance = dist;
            hitPoint = point;
            hitObject = rect;
            hitNormal = dist <= MathHelper.Epsilon && PointRect(origin, rect)
                ? GetNormalFromInside(origin, rect)
                : GetRectSurfaceNormal(point, rect);
            hit = true;
        }

        foreach ((Vect2 center, float radius) in circles)
        {
            if (!RaycastCircle(origin, direction, center, radius, out Vect2 point, out float dist) || dist >= distance)
                continue;

            distance = dist;
            hitPoint = point;
            hitObject = center;
            Vect2 normal = point - center;
            hitNormal = normal.LengthSquared() <= EpsilonSquared
                ? new Vect2(0f, -1f)
                : normal.Normalized();
            hit = true;
        }

        return hit;
    }

    /// <summary>Performs swept collision detection for a moving rectangle against a static rectangle.</summary>
    public static bool SweptRectRect(
        Rect2 moving,
        Vect2 velocity,
        Rect2 obstacle,
        out float timeOfImpact,
        out Vect2 hitPoint,
        out Vect2 hitNormal)
    {
        timeOfImpact = 1f;
        hitPoint = Vect2.Zero;
        hitNormal = Vect2.Zero;

        if (RectRect(moving, obstacle))
        {
            timeOfImpact = 0f;
            hitNormal = GetCollisionNormal(moving, obstacle);
            hitPoint = moving.Center;
            return true;
        }

        float velocityLength = velocity.Length();
        if (velocityLength <= MathHelper.Epsilon)
            return false;

        Rect2 expandedObstacle = obstacle.Inflate(moving.Width / 2f, moving.Height / 2f);
        Vect2 direction = velocity / velocityLength;

        if (!RaycastRect(moving.Center, direction, expandedObstacle, out Vect2 point, out float distance) ||
            distance > velocityLength)
        {
            return false;
        }

        timeOfImpact = distance / velocityLength;
        hitPoint = point;
        hitNormal = GetRectSurfaceNormal(point, expandedObstacle);
        return true;
    }

    /// <summary>Performs swept collision detection for a moving circle against a static rectangle.</summary>
    public static bool SweptCircleRect(
        Vect2 center,
        float radius,
        Vect2 velocity,
        Rect2 obstacle,
        out float timeOfImpact,
        out Vect2 hitPoint,
        out Vect2 hitNormal)
    {
        timeOfImpact = 1f;
        hitPoint = Vect2.Zero;
        hitNormal = Vect2.Zero;

        if (RectCircle(obstacle, center, radius))
        {
            timeOfImpact = 0f;
            hitNormal = GetCollisionNormal(center, radius, obstacle);
            hitPoint = center;
            return true;
        }

        float velocityLength = velocity.Length();
        if (velocityLength <= MathHelper.Epsilon)
            return false;

        Rect2 expandedObstacle = obstacle.Inflate(radius);
        Vect2 direction = velocity / velocityLength;

        if (!RaycastRect(center, direction, expandedObstacle, out Vect2 point, out float distance) ||
            distance > velocityLength)
        {
            return false;
        }

        timeOfImpact = distance / velocityLength;
        hitPoint = point;
        hitNormal = GetRectSurfaceNormal(point, expandedObstacle);
        return true;
    }

    /// <summary>Performs swept collision detection for a moving circle against a static circle.</summary>
    public static bool SweptCircleCircle(
        Vect2 centerA,
        float radiusA,
        Vect2 velocity,
        Vect2 centerB,
        float radiusB,
        out float timeOfImpact,
        out Vect2 hitPoint,
        out Vect2 hitNormal)
    {
        timeOfImpact = 1f;
        hitPoint = Vect2.Zero;
        hitNormal = Vect2.Zero;

        if (CircleCircle(centerA, radiusA, centerB, radiusB))
        {
            timeOfImpact = 0f;
            hitNormal = GetCollisionNormal(centerA, centerB);
            hitPoint = centerA;
            return true;
        }

        float velocityLength = velocity.Length();
        if (velocityLength <= MathHelper.Epsilon)
            return false;

        Vect2 direction = velocity / velocityLength;
        float combinedRadius = radiusA + radiusB;

        if (!RaycastCircle(centerA, direction, centerB, combinedRadius, out Vect2 point, out float distance) ||
            distance > velocityLength)
        {
            return false;
        }

        timeOfImpact = distance / velocityLength;
        hitPoint = point;
        hitNormal = (point - centerB).Normalized();
        return true;
    }

    /// <summary>Gets the minimum-axis collision normal from an obstacle rectangle toward a moving rectangle.</summary>
    public static Vect2 GetCollisionNormal(Rect2 moving, Rect2 obstacle)
    {
        if (!RectRect(moving, obstacle))
            return Vect2.Zero;

        float overlapX = MathF.Min(moving.Right - obstacle.Left, obstacle.Right - moving.Left);
        float overlapY = MathF.Min(moving.Bottom - obstacle.Top, obstacle.Bottom - moving.Top);

        if (overlapX < overlapY)
            return moving.Center.X < obstacle.Center.X ? new Vect2(-1f, 0f) : new Vect2(1f, 0f);

        return moving.Center.Y < obstacle.Center.Y ? new Vect2(0f, -1f) : new Vect2(0f, 1f);
    }

    /// <summary>Gets a collision normal from a rectangle toward an overlapping circle.</summary>
    public static Vect2 GetCollisionNormal(Vect2 circleCenter, float radius, Rect2 rect)
    {
        if (!RectCircle(rect, circleCenter, radius))
            return Vect2.Zero;

        if (PointRect(circleCenter, rect))
            return GetNormalFromInside(circleCenter, rect);

        Vect2 closest = ClosestPointRect(circleCenter, rect);
        Vect2 direction = circleCenter - closest;
        return direction.LengthSquared() <= EpsilonSquared ? new Vect2(0f, -1f) : direction.Normalized();
    }

    /// <summary>Gets a normal pointing from center B toward center A.</summary>
    public static Vect2 GetCollisionNormal(Vect2 centerA, Vect2 centerB)
    {
        Vect2 direction = centerA - centerB;
        return direction.LengthSquared() <= EpsilonSquared ? new Vect2(0f, -1f) : direction.Normalized();
    }

    /// <summary>Reflects a velocity from a normalized surface normal and scales the result by bounciness.</summary>
    public static Vect2 Reflect(Vect2 velocity, Vect2 normal, float bounciness = 1f)
    {
        float dot = Vect2.Dot(velocity, normal);
        return (velocity - 2f * dot * normal) * bounciness;
    }

    /// <summary>Checks whether one rectangle fully contains another.</summary>
    public static bool RectContainsRect(Rect2 outer, Rect2 inner)
        => inner.Left >= outer.Left && inner.Right <= outer.Right &&
           inner.Top >= outer.Top && inner.Bottom <= outer.Bottom;

    /// <summary>Checks whether a rectangle fully contains a circle.</summary>
    public static bool RectContainsCircle(Rect2 rect, Vect2 center, float radius)
        => center.X - radius >= rect.Left && center.X + radius <= rect.Right &&
           center.Y - radius >= rect.Top && center.Y + radius <= rect.Bottom;

    /// <summary>Checks whether one circle fully contains another.</summary>
    public static bool CircleContainsCircle(Vect2 outerCenter, float outerRadius, Vect2 innerCenter, float innerRadius)
        => Vect2.Distance(outerCenter, innerCenter) + innerRadius <= outerRadius;

    /// <summary>Checks whether a circle fully contains a rectangle.</summary>
    public static bool CircleContainsRect(Vect2 center, float radius, Rect2 rect)
        => PointCircle(rect.TopLeft, center, radius) &&
           PointCircle(rect.TopRight, center, radius) &&
           PointCircle(rect.BottomRight, center, radius) &&
           PointCircle(rect.BottomLeft, center, radius);

    /// <summary>Returns the minimum translation that moves an overlapping rectangle out of an obstacle rectangle.</summary>
    public static Vect2 PushRectRect(Rect2 moving, Rect2 obstacle)
    {
        if (!RectRect(moving, obstacle))
            return Vect2.Zero;

        float overlapX = MathF.Min(moving.Right - obstacle.Left, obstacle.Right - moving.Left);
        float overlapY = MathF.Min(moving.Bottom - obstacle.Top, obstacle.Bottom - moving.Top);

        if (overlapX < overlapY)
        {
            float sign = moving.Center.X < obstacle.Center.X ? -1f : 1f;
            return new Vect2(sign * overlapX, 0f);
        }

        float verticalSign = moving.Center.Y < obstacle.Center.Y ? -1f : 1f;
        return new Vect2(0f, verticalSign * overlapY);
    }

    /// <summary>Returns the minimum translation that moves an overlapping circle out of a rectangle.</summary>
    public static Vect2 PushCircleRect(Vect2 circleCenter, float radius, Rect2 rect)
    {
        if (!RectCircle(rect, circleCenter, radius))
            return Vect2.Zero;

        if (PointRect(circleCenter, rect))
        {
            float left = rect.Left - radius - circleCenter.X;
            float right = rect.Right + radius - circleCenter.X;
            float top = rect.Top - radius - circleCenter.Y;
            float bottom = rect.Bottom + radius - circleCenter.Y;

            float best = left;
            if (MathF.Abs(right) < MathF.Abs(best)) best = right;
            if (MathF.Abs(top) < MathF.Abs(best)) best = top;
            if (MathF.Abs(bottom) < MathF.Abs(best)) best = bottom;

            if (best == left || best == right)
                return new Vect2(best, 0f);

            return new Vect2(0f, best);
        }

        Vect2 closest = ClosestPointRect(circleCenter, rect);
        Vect2 direction = circleCenter - closest;
        float distance = direction.Length();

        if (distance <= MathHelper.Epsilon)
            return Vect2.Zero;

        return direction / distance * (radius - distance);
    }

    /// <summary>Returns the minimum translation that moves circle A out of circle B.</summary>
    public static Vect2 PushCircleCircle(Vect2 centerA, float radiusA, Vect2 centerB, float radiusB)
    {
        if (!CircleCircle(centerA, radiusA, centerB, radiusB))
            return Vect2.Zero;

        Vect2 direction = centerA - centerB;
        float distance = direction.Length();
        float combinedRadius = radiusA + radiusB;

        if (distance <= MathHelper.Epsilon)
            return new Vect2(0f, combinedRadius);

        return direction / distance * (combinedRadius - distance);
    }

    /// <summary>Moves a rectangle using X-then-Y axis separation against rectangle obstacles.</summary>
    public static Vect2 MoveAndSlideRect(Rect2 rect, Vect2 velocity, IEnumerable<Rect2> obstacles)
    {
        Vect2 position = rect.Position;

        position.X += velocity.X;
        foreach (Rect2 obstacle in obstacles)
        {
            Rect2 xRect = new(position, rect.Size);
            if (!RectRect(xRect, obstacle))
                continue;

            if (velocity.X > 0f)
                position.X = obstacle.Left - rect.Width;
            else if (velocity.X < 0f)
                position.X = obstacle.Right;
        }

        position.Y += velocity.Y;
        foreach (Rect2 obstacle in obstacles)
        {
            Rect2 yRect = new(position, rect.Size);
            if (!RectRect(yRect, obstacle))
                continue;

            if (velocity.Y > 0f)
                position.Y = obstacle.Top - rect.Height;
            else if (velocity.Y < 0f)
                position.Y = obstacle.Bottom;
        }

        return position;
    }

    /// <summary>Moves a circle using X-then-Y axis separation against rectangle and circle obstacles.</summary>
    public static Vect2 MoveAndSlideCircle(
        Vect2 center,
        float radius,
        Vect2 velocity,
        IEnumerable<Rect2> rects,
        IEnumerable<(Vect2 center, float radius)> circles)
    {
        Vect2 position = center;

        position.X += velocity.X;
        foreach (Rect2 rect in rects)
        {
            if (!RectCircle(rect, position, radius))
                continue;

            if (velocity.X > 0f)
                position.X = rect.Left - radius;
            else if (velocity.X < 0f)
                position.X = rect.Right + radius;
        }

        foreach ((Vect2 circleCenter, float circleRadius) in circles)
        {
            if (!CircleCircle(position, radius, circleCenter, circleRadius))
                continue;

            if (velocity.X > 0f)
                position.X = circleCenter.X - (radius + circleRadius);
            else if (velocity.X < 0f)
                position.X = circleCenter.X + (radius + circleRadius);
        }

        position.Y += velocity.Y;
        foreach (Rect2 rect in rects)
        {
            if (!RectCircle(rect, position, radius))
                continue;

            if (velocity.Y > 0f)
                position.Y = rect.Top - radius;
            else if (velocity.Y < 0f)
                position.Y = rect.Bottom + radius;
        }

        foreach ((Vect2 circleCenter, float circleRadius) in circles)
        {
            if (!CircleCircle(position, radius, circleCenter, circleRadius))
                continue;

            if (velocity.Y > 0f)
                position.Y = circleCenter.Y - (radius + circleRadius);
            else if (velocity.Y < 0f)
                position.Y = circleCenter.Y + (radius + circleRadius);
        }

        return position;
    }

    /// <summary>Gets the axis-aligned bounding rectangle of a circle.</summary>
    public static Rect2 GetCircleBounds(Vect2 center, float radius)
        => new(center.X - radius, center.Y - radius, radius * 2f, radius * 2f);

    private static float Cross(Vect2 a, Vect2 b)
        => a.X * b.Y - a.Y * b.X;

    private static bool PointOnSegment(Vect2 point, Vect2 start, Vect2 end)
    {
        Vect2 segment = end - start;
        Vect2 toPoint = point - start;

        if (segment.LengthSquared() <= EpsilonSquared)
            return Vect2.DistanceSquared(point, start) <= EpsilonSquared;

        if (MathF.Abs(Cross(toPoint, segment)) > MathHelper.Epsilon)
            return false;

        float dot = Vect2.Dot(toPoint, segment);
        return dot >= -MathHelper.Epsilon &&
               dot <= Vect2.Dot(segment, segment) + MathHelper.Epsilon;
    }

    private static bool TrySegmentIntersection(
        Vect2 a1,
        Vect2 a2,
        Vect2 b1,
        Vect2 b2,
        out float t,
        out Vect2 intersection)
    {
        t = 0f;
        intersection = Vect2.Zero;

        Vect2 r = a2 - a1;
        Vect2 s = b2 - b1;
        float rLengthSquared = r.LengthSquared();
        float sLengthSquared = s.LengthSquared();

        if (rLengthSquared <= EpsilonSquared)
        {
            if (!PointOnSegment(a1, b1, b2))
                return false;

            intersection = a1;
            return true;
        }

        if (sLengthSquared <= EpsilonSquared)
        {
            if (!PointOnSegment(b1, a1, a2))
                return false;

            t = Math.Clamp(Vect2.Dot(b1 - a1, r) / rLengthSquared, 0f, 1f);
            intersection = a1 + r * t;
            return true;
        }

        Vect2 qMinusP = b1 - a1;
        float rCrossS = Cross(r, s);
        float qCrossR = Cross(qMinusP, r);

        if (MathF.Abs(rCrossS) <= MathHelper.Epsilon)
        {
            if (MathF.Abs(qCrossR) > MathHelper.Epsilon)
                return false;

            float t0 = Vect2.Dot(qMinusP, r) / rLengthSquared;
            float t1 = t0 + Vect2.Dot(s, r) / rLengthSquared;
            float overlapStart = MathF.Max(0f, MathF.Min(t0, t1));
            float overlapEnd = MathF.Min(1f, MathF.Max(t0, t1));

            if (overlapStart > overlapEnd + MathHelper.Epsilon)
                return false;

            t = Math.Clamp(overlapStart, 0f, 1f);
            intersection = a1 + r * t;
            return true;
        }

        float candidateT = Cross(qMinusP, s) / rCrossS;
        float candidateU = Cross(qMinusP, r) / rCrossS;

        if (candidateT < -MathHelper.Epsilon || candidateT > 1f + MathHelper.Epsilon ||
            candidateU < -MathHelper.Epsilon || candidateU > 1f + MathHelper.Epsilon)
        {
            return false;
        }

        t = Math.Clamp(candidateT, 0f, 1f);
        intersection = a1 + r * t;
        return true;
    }

    private static void TestRectEdge(
        Vect2 start,
        Vect2 end,
        Vect2 edgeStart,
        Vect2 edgeEnd,
        Vect2 normal,
        ref bool hit,
        ref float closestT,
        ref Vect2 hitPoint,
        ref Vect2 hitNormal)
    {
        if (!TrySegmentIntersection(start, end, edgeStart, edgeEnd, out float t, out Vect2 point) || t >= closestT)
            return;

        closestT = t;
        hitPoint = point;
        hitNormal = normal;
        hit = true;
    }

    private static bool UpdateRaySlab(float origin, float direction, float min, float max, ref float tMin, ref float tMax)
    {
        if (MathF.Abs(direction) <= MathHelper.Epsilon)
            return origin >= min && origin <= max;

        float inverse = 1f / direction;
        float near = (min - origin) * inverse;
        float far = (max - origin) * inverse;

        if (near > far)
            (near, far) = (far, near);

        tMin = MathF.Max(tMin, near);
        tMax = MathF.Min(tMax, far);
        return tMin <= tMax;
    }

    private static Vect2 GetNormalFromInside(Vect2 point, Rect2 rect)
    {
        float left = point.X - rect.Left;
        float right = rect.Right - point.X;
        float top = point.Y - rect.Top;
        float bottom = rect.Bottom - point.Y;
        float minimum = MathF.Min(MathF.Min(left, right), MathF.Min(top, bottom));

        if (minimum == left) return new Vect2(-1f, 0f);
        if (minimum == right) return new Vect2(1f, 0f);
        if (minimum == top) return new Vect2(0f, -1f);
        return new Vect2(0f, 1f);
    }

    private static Vect2 GetRectSurfaceNormal(Vect2 point, Rect2 rect)
    {
        float left = MathF.Abs(point.X - rect.Left);
        float right = MathF.Abs(point.X - rect.Right);
        float top = MathF.Abs(point.Y - rect.Top);
        float bottom = MathF.Abs(point.Y - rect.Bottom);
        float minimum = MathF.Min(MathF.Min(left, right), MathF.Min(top, bottom));

        if (minimum == left) return new Vect2(-1f, 0f);
        if (minimum == right) return new Vect2(1f, 0f);
        if (minimum == top) return new Vect2(0f, -1f);
        return new Vect2(0f, 1f);
    }
}
