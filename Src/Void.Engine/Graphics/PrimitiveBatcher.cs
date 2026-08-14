using Void.Engine.Graphics.RenderTargets;

namespace Void.Engine.Graphics;

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

    public override string Name => "PrimitiveBatcher";
    protected override int VerticesPerCommand => 1;

    public PrimitiveBatcher(int capacity = 0) : base(capacity)
    {
        if (capacity <= 0)
            _capacity = GetDefaultCapacity();

        _cmds = new PrimitiveCommand[_capacity];
        _comparer = new PrimitiveCommandComparer(_sortMode);
        _vertexIndex = 0;
    }

    protected override int GetDefaultCapacity() => GameSettings.Instance.PrimitiveBatchCapacity;
    protected override void OnBegin() => _vertexIndex = 0;
    protected override void OnEnd() { }
    protected override void OnFlush() => _vertexIndex = 0;

    protected override void SortCommands()
    {
        _comparer.UpdateMode(_sortMode);
        Array.Sort(_cmds, 0, _cmdCount, _comparer);
    }

    protected override void BuildVertices() { }

    public override void Flush()
    {
        if (_cmdCount == 0) return;

        var sw = System.Diagnostics.Stopwatch.StartNew();

        if (_sortMode != SortMode.Immediate && _sortMode != SortMode.Deferred)
            SortCommands();

        // _gpuBuffer.Update(_vertexBuffer, (uint)_vertexIndex, 0);
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

            // _gpuBuffer.PrimitiveType = currentType;
            // _gpuBuffer.Draw(
            //     RenderTexture,
            //     (uint)vertexStart,
            //     (uint)vertexCount,
            //     _renderStates
            // );
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

    protected override void ResizeBuffers()
    {
        int newSize = _vertexBufferSize * 2;
        Array.Resize(ref _vertexData, newSize);
        _vertexBufferSize = newSize;

        // _gpuBuffer.Dispose();
        // _gpuBuffer = new SFVertexBuffer(
        //     (uint)newSize,
        //     SFPrimitiveType.Triangles,
        //     SFUsageSpecifier.Stream
        // );
        _vertexBuffer?.Dispose();
        _vertexBuffer = new VertexBuffer(newSize);

        int newCmdSize = _cmds.Length * 2;
        Array.Resize(ref _cmds, newCmdSize);
        _capacity = newCmdSize;
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

    public void DrawLine(Vect2 start, Vect2 end, Color color, float depth = 0f)
    {
        if (_isDisposed) throw new ObjectDisposedException(Name);
        if (!_isDrawing) throw new InvalidOperationException("Cannot draw outside Begin/End");

        EnsureVertexCapacity(2);

        _vertexData[_vertexIndex++] = new SFVertex(start, color);
        _vertexData[_vertexIndex++] = new SFVertex(end, color);

        AddCommand(SFPrimitiveType.Lines, 2, depth);
    }

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

    public void DrawPoint(Vect2 position, Color color, float depth = 0f)
    {
        if (_isDisposed) throw new ObjectDisposedException(Name);
        if (!_isDrawing) throw new InvalidOperationException("Cannot draw outside Begin/End");

        EnsureVertexCapacity(1);

        _vertexData[_vertexIndex++] = new SFVertex(position, color);

        AddCommand(SFPrimitiveType.Points, 1, depth);
    }

    public void DrawPoints(ReadOnlySpan<Vect2> positions, Color color, float depth = 0f)
    {
        if (_isDisposed) throw new ObjectDisposedException(Name);
        if (!_isDrawing) throw new InvalidOperationException("Cannot draw outside Begin/End");

        EnsureVertexCapacity(positions.Length);

        for (int i = 0; i < positions.Length; i++)
            _vertexData[_vertexIndex++] = new SFVertex(positions[i], color);

        AddCommand(SFPrimitiveType.Points, positions.Length, depth);
    }

    public void DrawRect(Vect2 position, Vect2 size, Color color, float depth = 0f)
    {
        DrawRect(position, size, color, 0f, Vect2.One, Vect2.Zero, depth);
    }

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

    public void DrawRectOutline(Vect2 position, Vect2 size, Color color, float depth = 0f)
        => DrawRectOutline(position, size, color, 0f, Vect2.One, Vect2.Zero, depth);

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

    public void DrawCircle(Vect2 center, float radius, Color color, int segments = DefaultCircleSegments, float depth = 0f)
        => DrawCircle(center, radius, color, 0f, Vect2.One, Vect2.Zero, segments, depth);

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

    public void DrawCircleOutline(Vect2 center, float radius, Color color, int segments = DefaultCircleSegments, float depth = 0f)
        => DrawCircleOutline(center, radius, color, 0f, Vect2.One, Vect2.Zero, segments, depth);

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

    public void DrawPolygon(ReadOnlySpan<Vect2> vertices, Color color, float depth = 0f)
        => DrawPolygon(vertices, color, Vect2.Zero, 0f, Vect2.One, Vect2.Zero, depth);

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

    public void DrawPolygonOutline(ReadOnlySpan<Vect2> vertices, Color color, float depth = 0f)
        => DrawPolygonOutline(vertices, color, Vect2.Zero, 0f, Vect2.One, Vect2.Zero, depth);

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
            // Sort by depth FIRST
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

            // Then by primitive type for batching
            if (a.PrimitiveType < b.PrimitiveType) return -1;
            if (a.PrimitiveType > b.PrimitiveType) return 1;

            return 0;
        }

        public void UpdateMode(SortMode sortMode) => _sortMode = sortMode;
    }
}