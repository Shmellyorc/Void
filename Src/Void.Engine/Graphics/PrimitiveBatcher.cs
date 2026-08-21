// ============================================================================
//  PrimitiveBatcher.cs
// ============================================================================
//  Batch rendering for primitives including lines, rectangles, circles,
//  polygons, and triangles. Supports transformations, color modulation,
//  depth sorting, and multiple primitive types (points, lines, triangles).
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Graphics;

/// <summary>
/// Batch rendering for primitives including lines, rectangles, circles,
/// polygons, and triangles.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="PrimitiveBatcher"/> class provides efficient batch rendering
/// for 2D primitives. It supports:
/// <list type="bullet">
///   <item><description>Lines, line strips, and points</description></item>
///   <item><description>Filled and outlined rectangles</description></item>
///   <item><description>Filled and outlined circles with configurable segments</description></item>
///   <item><description>Filled and outlined polygons</description></item>
///   <item><description>Filled and outlined triangles</description></item>
///   <item><description>Transformations (position, rotation, scale, origin)</description></item>
///   <item><description>Color modulation and depth sorting</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// var batcher = new PrimitiveBatcher();
/// batcher.Begin(SortMode.BackToFront);
/// 
/// // Draw a filled rectangle
/// batcher.DrawRect(new Vect2(100, 100), new Vect2(200, 150), Color.Red);
/// 
/// // Draw a circle outline
/// batcher.DrawCircleOutline(new Vect2(300, 300), 50, Color.Blue, 32);
/// 
/// // Draw a line
/// batcher.DrawLine(new Vect2(0, 0), new Vect2(100, 100), Color.Green);
/// 
/// batcher.End();
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe and should be accessed from the main thread.
/// </para>
/// </remarks>
public sealed class PrimitiveBatcher : BaseBatcher
{
    private const int MaxCircleSegments = 256;
    private const int MinCircleSegments = 3;
    private const int DefaultCircleSegments = 32;

    private struct PrimitiveCommand
    {
        public SFPrimitiveType PrimitiveType;
        public int VertexCount;
        public int VertexOffset;
        public float Depth;
    }

    private PrimitiveCommand[] _cmds;
    private readonly PrimitiveCommandComparer _comparer;
    private int _vertexIndex;
    private SFVertex[] _sortedVertexData;

    /// <summary>
    /// Gets the name of the batcher.
    /// </summary>
    public override string Name => "PrimitiveBatcher";

    /// <summary>
    /// Gets the number of vertices per command.
    /// </summary>
    protected override int VerticesPerCommand => 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrimitiveBatcher"/> class.
    /// </summary>
    /// <param name="capacity">The initial capacity of the batch.</param>
    public PrimitiveBatcher(int capacity = 0) : base(capacity)
    {
        if (capacity <= 0)
            _capacity = GetDefaultCapacity();

        _cmds = new PrimitiveCommand[_capacity];
        _sortedVertexData = new SFVertex[_capacity];
        _comparer = new PrimitiveCommandComparer(_sortMode);
        _vertexIndex = 0;
    }

    /// <summary>
    /// Gets the default capacity for the primitive batch.
    /// </summary>
    protected override int GetDefaultCapacity() => GameSettings.Instance.PrimitiveBatchCapacity;

    /// <summary>
    /// Called when batching begins.
    /// </summary>
    protected override void OnBegin() => _vertexIndex = 0;

    /// <summary>
    /// Called when batching ends.
    /// </summary>
    protected override void OnEnd() { }

    /// <summary>
    /// Called when the batch is flushed.
    /// </summary>
    protected override void OnFlush() => _vertexIndex = 0;

    /// <summary>
    /// Sorts the commands for optimal rendering.
    /// </summary>
    protected override void SortCommands()
    {
        _comparer.UpdateMode(_sortMode);
        Array.Sort(_cmds, 0, _cmdCount, _comparer);
        RebuildSortedVertices();
    }

    private void RebuildSortedVertices()
    {
        if (_sortedVertexData.Length < _vertexIndex)
        {
            Array.Resize(ref _sortedVertexData, _vertexIndex);
        }

        int sortedIndex = 0;

        for (int i = 0; i < _cmdCount; i++)
        {
            var cmd = _cmds[i];
            int sourceOffset = cmd.VertexOffset;

            for (int j = 0; j < cmd.VertexCount; j++)
            {
                _sortedVertexData[sortedIndex++] = _vertexData[sourceOffset + j];
            }

            _cmds[i].VertexOffset = sortedIndex - cmd.VertexCount;
        }

        Array.Copy(_sortedVertexData, _vertexData, sortedIndex);
    }

    /// <summary>
    /// Builds the vertices for rendering. (No-op for primitive batcher.)
    /// </summary>
    protected override void BuildVertices() { }

    /// <summary>
    /// Flushes all batched commands to the GPU.
    /// </summary>
    public override void Flush()
    {
        if (_cmdCount == 0) return;

        var sw = System.Diagnostics.Stopwatch.StartNew();

        if (_sortMode != SortMode.Immediate && _sortMode != SortMode.Deferred)
            SortCommands();

        _vertexBuffer.Update(_vertexData, (uint)_vertexIndex, 0);

        _renderStates.Texture = null;

        int drawCalls = 0;
        int index = 0;
        while (index < _cmdCount)
        {
            SFPrimitiveType currentType = _cmds[index].PrimitiveType;
            int vertexStart = _cmds[index].VertexOffset;
            int vertexCount = 0;

            while (index < _cmdCount && _cmds[index].PrimitiveType == currentType)
            {
                vertexCount += _cmds[index].VertexCount;
                index++;
            }

            _vertexBuffer.PrimitiveType = currentType;
            _vertexBuffer.Draw(_renderTarget, (uint)vertexStart, (uint)vertexCount, _renderStates);
            drawCalls++;
        }

        sw.Stop();
        _stats.GPUTime = (float)sw.Elapsed.TotalMilliseconds;
        _stats.DrawCalls = drawCalls;
        _stats.Vertices = _vertexIndex;
        _stats.Triangles = 0;
        _stats.Commands = _cmdCount;

        _cmdCount = 0;
        _vertexIndex = 0;

        OnFlush();
    }

    /// <summary>
    /// Resizes the vertex and command buffers.
    /// </summary>
    protected override void ResizeBuffers()
    {
        Logger.Instance.DebugWithCategory("PrimitiveBatcher",
            "Resizing buffers: {0} -> {1} commands", _vertexBufferSize, _vertexBufferSize * 2);

        int newSize = _vertexBufferSize * 2;
        Array.Resize(ref _vertexData, newSize);
        Array.Resize(ref _sortedVertexData, newSize);
        _vertexBufferSize = newSize;

        _vertexBuffer?.Dispose();
        _vertexBuffer = new VertexBuffer(newSize);

        int newCmdSize = _cmds.Length * 2;
        Array.Resize(ref _cmds, newCmdSize);
        _capacity = newCmdSize;
    }

    /// <summary>
    /// Sets the render state for a group of commands.
    /// </summary>
    protected override void SetRenderStateForGroup(int commandIndex)
    {
        base.SetRenderStateForGroup(commandIndex);
        ApplyShader();
    }

    private void EnsureVertexCapacity(int needed)
    {
        while (_vertexIndex + needed > _vertexBufferSize)
            ResizeBuffers();
    }

    private void AddCommand(SFPrimitiveType primitiveType, int vertexCount, float depth)
    {
        if (_cmdCount >= _cmds.Length)
        {
            int newCmdSize = _cmds.Length * 2;
            Array.Resize(ref _cmds, newCmdSize);
            _capacity = newCmdSize;
        }

        _cmds[_cmdCount] = new PrimitiveCommand
        {
            PrimitiveType = primitiveType,
            VertexCount = vertexCount,
            VertexOffset = _vertexIndex - vertexCount,
            Depth = depth
        };
        _cmdCount++;
    }

    #region Draw Methods

    /// <summary>
    /// Draws a line between two points.
    /// </summary>
    /// <param name="start">The starting point.</param>
    /// <param name="end">The ending point.</param>
    /// <param name="color">The color of the line.</param>
    /// <param name="depth">The depth for sorting.</param>
    /// <exception cref="ObjectDisposedException">Thrown when the batcher has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when drawing outside of Begin/End.</exception>
    public void DrawLine(Vect2 start, Vect2 end, Color color, float depth = 0f)
    {
        if (_isDisposed) throw new ObjectDisposedException(Name);
        if (!_isDrawing) throw new InvalidOperationException("Cannot draw outside Begin/End");

        EnsureVertexCapacity(2);

        _vertexData[_vertexIndex++] = new SFVertex(start, color);
        _vertexData[_vertexIndex++] = new SFVertex(end, color);

        AddCommand(SFPrimitiveType.Lines, 2, depth);
    }

    /// <summary>
    /// Draws a line strip connecting multiple points.
    /// </summary>
    /// <param name="points">The points to connect.</param>
    /// <param name="color">The color of the lines.</param>
    /// <param name="depth">The depth for sorting.</param>
    /// <exception cref="ObjectDisposedException">Thrown when the batcher has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when drawing outside of Begin/End.</exception>
    public void DrawLineStrip(ReadOnlySpan<Vect2> points, Color color, float depth = 0f)
    {
        if (_isDisposed) throw new ObjectDisposedException(Name);
        if (!_isDrawing) throw new InvalidOperationException("Cannot draw outside Begin/End");
        if (points.Length < 2) return;

        EnsureVertexCapacity(points.Length);

        for (int i = 0; i < points.Length; i++)
            _vertexData[_vertexIndex++] = new SFVertex(points[i], color);

        AddCommand(SFPrimitiveType.LineStrip, points.Length, depth);
    }

    /// <summary>
    /// Draws a single point.
    /// </summary>
    /// <param name="position">The position of the point.</param>
    /// <param name="color">The color of the point.</param>
    /// <param name="depth">The depth for sorting.</param>
    /// <exception cref="ObjectDisposedException">Thrown when the batcher has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when drawing outside of Begin/End.</exception>
    public void DrawPoint(Vect2 position, Color color, float depth = 0f)
    {
        if (_isDisposed) throw new ObjectDisposedException(Name);
        if (!_isDrawing) throw new InvalidOperationException("Cannot draw outside Begin/End");

        EnsureVertexCapacity(1);

        _vertexData[_vertexIndex++] = new SFVertex(position, color);

        AddCommand(SFPrimitiveType.Points, 1, depth);
    }

    /// <summary>
    /// Draws multiple points.
    /// </summary>
    /// <param name="positions">The positions of the points.</param>
    /// <param name="color">The color of the points.</param>
    /// <param name="depth">The depth for sorting.</param>
    /// <exception cref="ObjectDisposedException">Thrown when the batcher has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when drawing outside of Begin/End.</exception>
    public void DrawPoints(ReadOnlySpan<Vect2> positions, Color color, float depth = 0f)
    {
        if (_isDisposed) throw new ObjectDisposedException(Name);
        if (!_isDrawing) throw new InvalidOperationException("Cannot draw outside Begin/End");

        EnsureVertexCapacity(positions.Length);

        for (int i = 0; i < positions.Length; i++)
            _vertexData[_vertexIndex++] = new SFVertex(positions[i], color);

        AddCommand(SFPrimitiveType.Points, positions.Length, depth);
    }

    /// <summary>
    /// Draws a filled rectangle.
    /// </summary>
    /// <param name="position">The position of the rectangle.</param>
    /// <param name="size">The size of the rectangle.</param>
    /// <param name="color">The color of the rectangle.</param>
    /// <param name="depth">The depth for sorting.</param>
    public void DrawRect(Vect2 position, Vect2 size, Color color, float depth = 0f)
    {
        DrawRect(position, size, color, 0f, Vect2.One, Vect2.Zero, depth);
    }

    /// <summary>
    /// Draws a filled rectangle with transformations.
    /// </summary>
    /// <param name="position">The position of the rectangle.</param>
    /// <param name="size">The size of the rectangle.</param>
    /// <param name="color">The color of the rectangle.</param>
    /// <param name="rotation">The rotation in radians.</param>
    /// <param name="scale">The scale factor.</param>
    /// <param name="origin">The origin for rotation and scaling.</param>
    /// <param name="depth">The depth for sorting.</param>
    public void DrawRect(Vect2 position, Vect2 size, Color color, float rotation, Vect2 scale, Vect2 origin, float depth = 0f)
    {
        if (_isDisposed) throw new ObjectDisposedException(Name);
        if (!_isDrawing) throw new InvalidOperationException("Cannot draw outside Begin/End");

        EnsureVertexCapacity(6);

        Vect2 p0 = new(0, 0);
        Vect2 p1 = new(size.X, 0);
        Vect2 p2 = new(size.X, size.Y);
        Vect2 p3 = new(0, size.Y);

        if (rotation != 0f || scale != Vect2.One || origin != Vect2.Zero)
        {
            float cos = MathF.Cos(rotation);
            float sin = MathF.Sin(rotation);
            float ox = origin.X * scale.X;
            float oy = origin.Y * scale.Y;

            Vect2 Transform(Vect2 p)
            {
                float sx = p.X * scale.X - ox;
                float sy = p.Y * scale.Y - oy;
                return new Vect2(
                    position.X + sx * cos - sy * sin + ox,
                    position.Y + sx * sin + sy * cos + oy
                );
            }

            p0 = Transform(p0);
            p1 = Transform(p1);
            p2 = Transform(p2);
            p3 = Transform(p3);
        }
        else
        {
            p0 += position;
            p1 += position;
            p2 += position;
            p3 += position;
        }

        _vertexData[_vertexIndex++] = new SFVertex(p0, color);
        _vertexData[_vertexIndex++] = new SFVertex(p1, color);
        _vertexData[_vertexIndex++] = new SFVertex(p2, color);
        _vertexData[_vertexIndex++] = new SFVertex(p0, color);
        _vertexData[_vertexIndex++] = new SFVertex(p2, color);
        _vertexData[_vertexIndex++] = new SFVertex(p3, color);

        AddCommand(SFPrimitiveType.Triangles, 6, depth);
    }

    /// <summary>
    /// Draws a rectangle outline.
    /// </summary>
    /// <param name="position">The position of the rectangle.</param>
    /// <param name="size">The size of the rectangle.</param>
    /// <param name="color">The color of the outline.</param>
    /// <param name="depth">The depth for sorting.</param>
    public void DrawRectOutline(Vect2 position, Vect2 size, Color color, float depth = 0f)
        => DrawRectOutline(position, size, color, 0f, Vect2.One, Vect2.Zero, depth);

    /// <summary>
    /// Draws a rectangle outline with transformations.
    /// </summary>
    /// <param name="position">The position of the rectangle.</param>
    /// <param name="size">The size of the rectangle.</param>
    /// <param name="color">The color of the outline.</param>
    /// <param name="rotation">The rotation in radians.</param>
    /// <param name="scale">The scale factor.</param>
    /// <param name="origin">The origin for rotation and scaling.</param>
    /// <param name="depth">The depth for sorting.</param>
    public void DrawRectOutline(Vect2 position, Vect2 size, Color color, float rotation, Vect2 scale, Vect2 origin, float depth = 0f)
    {
        if (_isDisposed) throw new ObjectDisposedException(Name);
        if (!_isDrawing) throw new InvalidOperationException("Cannot draw outside Begin/End");

        EnsureVertexCapacity(8);

        Vect2 p0 = new(0, 0);
        Vect2 p1 = new(size.X, 0);
        Vect2 p2 = new(size.X, size.Y);
        Vect2 p3 = new(0, size.Y);

        if (rotation != 0f || scale != Vect2.One || origin != Vect2.Zero)
        {
            float cos = MathF.Cos(rotation);
            float sin = MathF.Sin(rotation);
            float ox = origin.X * scale.X;
            float oy = origin.Y * scale.Y;

            Vect2 Transform(Vect2 p)
            {
                float sx = p.X * scale.X - ox;
                float sy = p.Y * scale.Y - oy;
                return new Vect2(
                    position.X + sx * cos - sy * sin + ox,
                    position.Y + sx * sin + sy * cos + oy
                );
            }

            p0 = Transform(p0);
            p1 = Transform(p1);
            p2 = Transform(p2);
            p3 = Transform(p3);
        }
        else
        {
            p0 += position;
            p1 += position;
            p2 += position;
            p3 += position;
        }

        _vertexData[_vertexIndex++] = new SFVertex(p0, color);
        _vertexData[_vertexIndex++] = new SFVertex(p1, color);
        _vertexData[_vertexIndex++] = new SFVertex(p1, color);
        _vertexData[_vertexIndex++] = new SFVertex(p2, color);
        _vertexData[_vertexIndex++] = new SFVertex(p2, color);
        _vertexData[_vertexIndex++] = new SFVertex(p3, color);
        _vertexData[_vertexIndex++] = new SFVertex(p3, color);
        _vertexData[_vertexIndex++] = new SFVertex(p0, color);

        AddCommand(SFPrimitiveType.Lines, 8, depth);
    }

    /// <summary>
    /// Draws a filled circle.
    /// </summary>
    /// <param name="center">The center of the circle.</param>
    /// <param name="radius">The radius of the circle.</param>
    /// <param name="color">The color of the circle.</param>
    /// <param name="segments">The number of segments (3-256).</param>
    /// <param name="depth">The depth for sorting.</param>
    public void DrawCircle(Vect2 center, float radius, Color color, int segments = DefaultCircleSegments, float depth = 0f)
        => DrawCircle(center, radius, color, 0f, Vect2.One, Vect2.Zero, segments, depth);

    /// <summary>
    /// Draws a filled circle with transformations.
    /// </summary>
    /// <param name="center">The center of the circle.</param>
    /// <param name="radius">The radius of the circle.</param>
    /// <param name="color">The color of the circle.</param>
    /// <param name="rotation">The rotation in radians.</param>
    /// <param name="scale">The scale factor.</param>
    /// <param name="origin">The origin for rotation and scaling.</param>
    /// <param name="segments">The number of segments (3-256).</param>
    /// <param name="depth">The depth for sorting.</param>
    public void DrawCircle(Vect2 center, float radius, Color color, float rotation, Vect2 scale, Vect2 origin, int segments = DefaultCircleSegments, float depth = 0f)
    {
        if (_isDisposed) throw new ObjectDisposedException(Name);
        if (!_isDrawing) throw new InvalidOperationException("Cannot draw outside Begin/End");

        segments = Math.Clamp(segments, MinCircleSegments, MaxCircleSegments);

        int vertexCount = segments + 2;
        EnsureVertexCapacity(vertexCount);

        float cos = MathF.Cos(rotation);
        float sin = MathF.Sin(rotation);

        _vertexData[_vertexIndex++] = new SFVertex(center, color);

        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * MathF.PI * 2f;
            float x = MathF.Cos(angle) * radius;
            float y = MathF.Sin(angle) * radius;

            float sx = x * scale.X - origin.X;
            float sy = y * scale.Y - origin.Y;
            float rx = sx * cos - sy * sin + origin.X;
            float ry = sx * sin + sy * cos + origin.Y;

            _vertexData[_vertexIndex++] = new SFVertex(
                new Vect2(center.X + rx, center.Y + ry),
                color
            );
        }

        AddCommand(SFPrimitiveType.TriangleFan, vertexCount, depth);
    }

    /// <summary>
    /// Draws a circle outline.
    /// </summary>
    /// <param name="center">The center of the circle.</param>
    /// <param name="radius">The radius of the circle.</param>
    /// <param name="color">The color of the outline.</param>
    /// <param name="segments">The number of segments (3-256).</param>
    /// <param name="depth">The depth for sorting.</param>
    public void DrawCircleOutline(Vect2 center, float radius, Color color, int segments = DefaultCircleSegments, float depth = 0f)
        => DrawCircleOutline(center, radius, color, 0f, Vect2.One, Vect2.Zero, segments, depth);

    /// <summary>
    /// Draws a circle outline with transformations.
    /// </summary>
    /// <param name="center">The center of the circle.</param>
    /// <param name="radius">The radius of the circle.</param>
    /// <param name="color">The color of the outline.</param>
    /// <param name="rotation">The rotation in radians.</param>
    /// <param name="scale">The scale factor.</param>
    /// <param name="origin">The origin for rotation and scaling.</param>
    /// <param name="segments">The number of segments (3-256).</param>
    /// <param name="depth">The depth for sorting.</param>
    public void DrawCircleOutline(Vect2 center, float radius, Color color, float rotation, Vect2 scale, Vect2 origin, int segments = DefaultCircleSegments, float depth = 0f)
    {
        if (_isDisposed) throw new ObjectDisposedException(Name);
        if (!_isDrawing) throw new InvalidOperationException("Cannot draw outside Begin/End");

        segments = Math.Clamp(segments, MinCircleSegments, MaxCircleSegments);

        int vertexCount = segments * 2;
        EnsureVertexCapacity(vertexCount);

        float cos = MathF.Cos(rotation);
        float sin = MathF.Sin(rotation);

        Vect2 firstPoint = default;
        Vect2 prevPoint = default;

        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * MathF.PI * 2f;
            float x = MathF.Cos(angle) * radius;
            float y = MathF.Sin(angle) * radius;

            float sx = x * scale.X - origin.X;
            float sy = y * scale.Y - origin.Y;
            float rx = sx * cos - sy * sin + origin.X;
            float ry = sx * sin + sy * cos + origin.Y;

            var point = new Vect2(center.X + rx, center.Y + ry);

            if (i == 0)
            {
                firstPoint = point;
            }
            else
            {
                _vertexData[_vertexIndex++] = new SFVertex(prevPoint, color);
                _vertexData[_vertexIndex++] = new SFVertex(point, color);
            }

            prevPoint = point;
        }

        _vertexData[_vertexIndex++] = new SFVertex(prevPoint, color);
        _vertexData[_vertexIndex++] = new SFVertex(firstPoint, color);

        AddCommand(SFPrimitiveType.Lines, vertexCount, depth);
    }

    /// <summary>
    /// Draws a filled polygon.
    /// </summary>
    /// <param name="vertices">The vertices of the polygon.</param>
    /// <param name="color">The color of the polygon.</param>
    /// <param name="depth">The depth for sorting.</param>
    public void DrawPolygon(ReadOnlySpan<Vect2> vertices, Color color, float depth = 0f)
        => DrawPolygon(vertices, color, Vect2.Zero, 0f, Vect2.One, Vect2.Zero, depth);

    /// <summary>
    /// Draws a filled polygon with transformations.
    /// </summary>
    /// <param name="vertices">The vertices of the polygon.</param>
    /// <param name="color">The color of the polygon.</param>
    /// <param name="position">The position offset.</param>
    /// <param name="rotation">The rotation in radians.</param>
    /// <param name="scale">The scale factor.</param>
    /// <param name="origin">The origin for rotation and scaling.</param>
    /// <param name="depth">The depth for sorting.</param>
    public void DrawPolygon(ReadOnlySpan<Vect2> vertices, Color color, Vect2 position, float rotation, Vect2 scale, Vect2 origin, float depth = 0f)
    {
        if (_isDisposed) throw new ObjectDisposedException(Name);
        if (!_isDrawing) throw new InvalidOperationException("Cannot draw outside Begin/End");
        if (vertices.Length < 3) return;

        int vertexCount = (vertices.Length - 2) * 3;
        EnsureVertexCapacity(vertexCount);

        float cos = MathF.Cos(rotation);
        float sin = MathF.Sin(rotation);

        Vect2 Transform(Vect2 p)
        {
            float sx = p.X * scale.X - origin.X;
            float sy = p.Y * scale.Y - origin.Y;
            return new Vect2(
                position.X + sx * cos - sy * sin + origin.X,
                position.Y + sx * sin + sy * cos + origin.Y
            );
        }

        Vect2 v0 = Transform(vertices[0]);

        for (int i = 1; i < vertices.Length - 1; i++)
        {
            Vect2 v1 = Transform(vertices[i]);
            Vect2 v2 = Transform(vertices[i + 1]);

            _vertexData[_vertexIndex++] = new SFVertex(v0, color);
            _vertexData[_vertexIndex++] = new SFVertex(v1, color);
            _vertexData[_vertexIndex++] = new SFVertex(v2, color);
        }

        AddCommand(SFPrimitiveType.Triangles, vertexCount, depth);
    }

    /// <summary>
    /// Draws a polygon outline.
    /// </summary>
    /// <param name="vertices">The vertices of the polygon.</param>
    /// <param name="color">The color of the outline.</param>
    /// <param name="depth">The depth for sorting.</param>
    public void DrawPolygonOutline(ReadOnlySpan<Vect2> vertices, Color color, float depth = 0f)
        => DrawPolygonOutline(vertices, color, Vect2.Zero, 0f, Vect2.One, Vect2.Zero, depth);

    /// <summary>
    /// Draws a polygon outline with transformations.
    /// </summary>
    /// <param name="vertices">The vertices of the polygon.</param>
    /// <param name="color">The color of the outline.</param>
    /// <param name="position">The position offset.</param>
    /// <param name="rotation">The rotation in radians.</param>
    /// <param name="scale">The scale factor.</param>
    /// <param name="origin">The origin for rotation and scaling.</param>
    /// <param name="depth">The depth for sorting.</param>
    public void DrawPolygonOutline(ReadOnlySpan<Vect2> vertices, Color color, Vect2 position, float rotation, Vect2 scale, Vect2 origin, float depth = 0f)
    {
        if (_isDisposed) throw new ObjectDisposedException(Name);
        if (!_isDrawing) throw new InvalidOperationException("Cannot draw outside Begin/End");
        if (vertices.Length < 2) return;

        int vertexCount = vertices.Length * 2;
        EnsureVertexCapacity(vertexCount);

        float cos = MathF.Cos(rotation);
        float sin = MathF.Sin(rotation);

        Vect2 Transform(Vect2 p)
        {
            float sx = p.X * scale.X - origin.X;
            float sy = p.Y * scale.Y - origin.Y;
            return new Vect2(
                position.X + sx * cos - sy * sin + origin.X,
                position.Y + sx * sin + sy * cos + origin.Y
            );
        }

        Vect2 first = Transform(vertices[0]);
        Vect2 prev = first;

        for (int i = 1; i < vertices.Length; i++)
        {
            Vect2 curr = Transform(vertices[i]);
            _vertexData[_vertexIndex++] = new SFVertex(prev, color);
            _vertexData[_vertexIndex++] = new SFVertex(curr, color);
            prev = curr;
        }

        _vertexData[_vertexIndex++] = new SFVertex(prev, color);
        _vertexData[_vertexIndex++] = new SFVertex(first, color);

        AddCommand(SFPrimitiveType.Lines, vertexCount, depth);
    }

    /// <summary>
    /// Draws a filled triangle.
    /// </summary>
    /// <param name="a">The first vertex.</param>
    /// <param name="b">The second vertex.</param>
    /// <param name="c">The third vertex.</param>
    /// <param name="color">The color of the triangle.</param>
    /// <param name="depth">The depth for sorting.</param>
    public void DrawTriangle(Vect2 a, Vect2 b, Vect2 c, Color color, float depth = 0f)
    {
        if (_isDisposed) throw new ObjectDisposedException(Name);
        if (!_isDrawing) throw new InvalidOperationException("Cannot draw outside Begin/End");

        EnsureVertexCapacity(3);

        _vertexData[_vertexIndex++] = new SFVertex(a, color);
        _vertexData[_vertexIndex++] = new SFVertex(b, color);
        _vertexData[_vertexIndex++] = new SFVertex(c, color);

        AddCommand(SFPrimitiveType.Triangles, 3, depth);
    }

    /// <summary>
    /// Draws a triangle outline.
    /// </summary>
    /// <param name="a">The first vertex.</param>
    /// <param name="b">The second vertex.</param>
    /// <param name="c">The third vertex.</param>
    /// <param name="color">The color of the outline.</param>
    /// <param name="depth">The depth for sorting.</param>
    public void DrawTriangleOutline(Vect2 a, Vect2 b, Vect2 c, Color color, float depth = 0f)
    {
        if (_isDisposed) throw new ObjectDisposedException(Name);
        if (!_isDrawing) throw new InvalidOperationException("Cannot draw outside Begin/End");

        EnsureVertexCapacity(6);

        _vertexData[_vertexIndex++] = new SFVertex(a, color);
        _vertexData[_vertexIndex++] = new SFVertex(b, color);
        _vertexData[_vertexIndex++] = new SFVertex(b, color);
        _vertexData[_vertexIndex++] = new SFVertex(c, color);
        _vertexData[_vertexIndex++] = new SFVertex(c, color);
        _vertexData[_vertexIndex++] = new SFVertex(a, color);

        AddCommand(SFPrimitiveType.Lines, 6, depth);
    }

    #endregion

    private sealed class PrimitiveCommandComparer : IComparer<PrimitiveCommand>
    {
        private SortMode _sortMode;

        public PrimitiveCommandComparer(SortMode sortMode)
        {
            _sortMode = sortMode;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare(PrimitiveCommand a, PrimitiveCommand b)
        {
            if (_sortMode == SortMode.BackToFront)
            {
                if (a.Depth < b.Depth) return -1;
                if (a.Depth > b.Depth) return 1;
            }
            else if (_sortMode == SortMode.FrontToBack)
            {
                if (b.Depth < a.Depth) return -1;
                if (b.Depth > a.Depth) return 1;
            }

            if (a.PrimitiveType < b.PrimitiveType) return -1;
            if (a.PrimitiveType > b.PrimitiveType) return 1;

            return 0;
        }

        public void UpdateMode(SortMode sortMode) => _sortMode = sortMode;
    }
}