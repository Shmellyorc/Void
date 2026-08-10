namespace Void.Engine.Graphics;

public sealed partial class SpriteBatcher : BaseBatcher
{
    private const int VerticesPerQuad = 6;
    private const int InitialCapacity = 1024;

    private DrawCommand[] _cmds;
    private readonly DrawCommandComparer _comparer;

    public override string Name => "SpriteBatcher";
    protected override int VerticesPerCommand => VerticesPerQuad;

    public SpriteBatcher(int capacity = 0) : base(capacity)
    {
        if(capacity <= 0)
            _capacity = GetDefaultCapacity();

        _cmds = new DrawCommand[_capacity];
        _comparer = new DrawCommandComparer(_sortMode);
    }

    protected override int GetDefaultCapacity() => GameSettings.Instance.SpriteBatchCapacity;
    protected override void OnBegin() => _currentTexture = null;
    protected override void OnEnd() { }
    protected override void OnFlush() { }

    protected override void SortCommands()
    {
        _comparer.UpdateMode(_sortMode);
        Array.Sort(_cmds, 0, _cmdCount, _comparer);
    }

    protected override unsafe void BuildVertices()
    {
        fixed (SFVertex* vertexPtr = _vertexBuffer)
        {
            SFVertex* currentPtr = vertexPtr;
            for (int i = 0; i < _cmdCount; i++)
            {
                WriteQuadUnsafe(currentPtr, _cmds[i]);
                currentPtr += VerticesPerQuad;
            }
        }
    }

    protected override bool CanBatchTogether(int indexA, int indexB)
        => _cmds[indexA].Texture.NativeHandle == _cmds[indexB].Texture.NativeHandle;

    protected override void SetRenderStateForGroup(int commandIndex)
        => _renderStates.Texture = _cmds[commandIndex].Texture;

    protected override void ResizeBuffers()
    {
        int newSize = _cmds.Length * 2;
        Array.Resize(ref _cmds, newSize);

        var newVertexSize = newSize * VerticesPerQuad;
        Array.Resize(ref _vertexBuffer, newVertexSize);
        _vertexBufferSize = newVertexSize;

        _gpuBuffer.Dispose();
        _gpuBuffer = new SFVertexBuffer(
            (uint)newVertexSize,
            SFPrimitiveType.Triangles,
            SFUsageSpecifier.Stream
        );

        _capacity = newSize;
    }

    public void Draw(Texture texture, Rect2 dstRect, Rect2 srcRect, Color color, float depth = 0f)
        => EngineDraw(texture, dstRect, srcRect, color, 0f, Vect2.One, Vect2.Zero, TextureEffects.None, depth);
    public void Draw(Texture texture, Rect2 rect, Color color, float depth = 0f)
        => EngineDraw(texture, rect, texture.Bounds, color, 0f, Vect2.One, Vect2.Zero, TextureEffects.None, depth);
    public void Draw(Texture texture, Vect2 position, Rect2 srcRect, Color color, float depth = 0f)
        => EngineDraw(texture, new(position, srcRect.Size), srcRect, color, 0f, Vect2.One, Vect2.Zero, TextureEffects.None, depth);
    public void Draw(Texture texture, Rect2 dstRect, Rect2 srcRect, Color color, float rotation, Vect2 scale, Vect2 origin, TextureEffects effects, float depth)
        => EngineDraw(texture, dstRect, srcRect, color, rotation, scale, origin, effects, depth);
    public void Draw(Texture texture, Rect2 rect, Color color, float rotation, Vect2 scale, Vect2 origin, TextureEffects effects, float depth)
        => EngineDraw(texture, rect, texture.Bounds, color, rotation, scale, origin, effects, depth);
    public void Draw(Texture texture, Vect2 position, Rect2 srcRect, Color color, float rotation, Vect2 scale, Vect2 origin, TextureEffects effects, float depth)
        => EngineDraw(texture, new(position, srcRect.Size), srcRect, color, rotation, scale, origin, effects, depth);




    public void DrawBypassAtlas(Texture texture, Rect2 dstRect, Rect2 srcRect, Color color, float depth = 0f)
       => EngineDrawBypassAtlas(texture, dstRect, srcRect, color, 0f, Vect2.One, Vect2.Zero, TextureEffects.None, depth);
    public void DrawBypassAtlas(Texture texture, Rect2 rect, Color color, float depth = 0f)
        => EngineDrawBypassAtlas(texture, rect, texture.Bounds, color, 0f, Vect2.One, Vect2.Zero, TextureEffects.None, depth);
    public void DrawBypassAtlas(Texture texture, Vect2 position, Rect2 srcRect, Color color, float depth = 0f)
        => EngineDrawBypassAtlas(texture, new(position, srcRect.Size), srcRect, color, 0f, Vect2.One, Vect2.Zero, TextureEffects.None, depth);
    public void DrawBypassAtlas(Texture texture, Rect2 dstRect, Rect2 srcRect, Color color, float rotation, Vect2 scale, Vect2 origin, TextureEffects effects, float depth)
        => EngineDrawBypassAtlas(texture, dstRect, srcRect, color, rotation, scale, origin, effects, depth);
    public void DrawBypassAtlas(Texture texture, Rect2 rect, Color color, float rotation, Vect2 scale, Vect2 origin, TextureEffects effects, float depth)
        => EngineDrawBypassAtlas(texture, rect, texture.Bounds, color, rotation, scale, origin, effects, depth);
    public void DrawBypassAtlas(Texture texture, Vect2 position, Rect2 srcRect, Color color, float rotation, Vect2 scale, Vect2 origin, TextureEffects effects, float depth)
        => EngineDrawBypassAtlas(texture, new(position, srcRect.Size), srcRect, color, rotation, scale, origin, effects, depth);



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EngineDraw(Texture texture, Rect2 dstRect, Rect2 srcRect, Color color, float rotation, Vect2 scale,
        Vect2 origin, TextureEffects effects, float depth)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SpriteBatcher));
        if (!_isDrawing)
            throw new InvalidOperationException("cannot draw outside Begin/End");

        if (_cmdCount >= _cmds.Length)
            ResizeBuffers();

        if (AtlasManager.Instance.TryPack(texture, srcRect, out var packedRect, out var pageId))
        {
            var atlasTexture = AtlasManager.Instance.GetPageTexture(pageId);

            _cmds[_cmdCount] = new DrawCommand
            {
                Texture = atlasTexture,
                Depth = depth,
                DstRect = new Rect2(dstRect.X, dstRect.Y, dstRect.Width * scale.X, dstRect.Height * scale.Y),
                SrcRect = packedRect,
                Color = color,
                Rotation = rotation,
                Scale = scale,
                Origin = origin,
                Effects = effects
            };
        }
        else
        {
            _cmds[_cmdCount] = new DrawCommand
            {
                Texture = texture,
                Depth = depth,
                DstRect = new Rect2(dstRect.X, dstRect.Y, dstRect.Width * scale.X, dstRect.Height * scale.Y),
                SrcRect = srcRect,
                Color = color,
                Rotation = rotation,
                Scale = scale,
                Origin = origin,
                Effects = effects
            };
        }

        _cmdCount++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EngineDrawBypassAtlas(Texture texture, Rect2 dstRect, Rect2 srcRect, Color color, float rotation, Vect2 scale,
        Vect2 origin, TextureEffects effects, float depth)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SpriteBatcher));
        if (!_isDrawing)
            throw new InvalidOperationException("cannot draw outside Begin/End");

        if (_cmdCount >= _cmds.Length)
            ResizeBuffers();

        _cmds[_cmdCount] = new DrawCommand
        {
            Texture = texture,
            Depth = depth,
            DstRect = new Rect2(dstRect.X, dstRect.Y, dstRect.Width * scale.X, dstRect.Height * scale.Y),
            SrcRect = srcRect,
            Color = color,
            Rotation = rotation,
            Scale = scale,
            Origin = origin,
            Effects = effects
        };

        _cmdCount++;
    }

    private unsafe void WriteQuadUnsafe(SFVertex* ptr, in DrawCommand cmd)
    {
        Vect2* corners = stackalloc Vect2[4];
        float left = MathF.Round(cmd.DstRect.Left, 3);
        float top = MathF.Round(cmd.DstRect.Top, 3);
        float right = MathF.Round(cmd.DstRect.Right, 3);
        float bottom = MathF.Round(cmd.DstRect.Bottom, 3);

        corners[0] = new Vect2(left, top);
        corners[1] = new Vect2(right, top);
        corners[2] = new Vect2(left, bottom);
        corners[3] = new Vect2(right, bottom);

        if (cmd.Rotation != 0f)
        {
            float cos = MathF.Cos(cmd.Rotation);
            float sin = MathF.Sin(cmd.Rotation);
            float centerX = left + cmd.Origin.X * cmd.Scale.X;
            float centerY = top + cmd.Origin.Y * cmd.Scale.Y;

            for (int i = 0; i < 4; i++)
            {
                float dx = corners[i].X - centerX;
                float dy = corners[i].Y - centerY;

                corners[i] = new Vect2(
                    centerX + dx * cos - dy * sin,
                    centerY + dx * sin + dy * cos
                );
            }
        }

        float srcLeft = cmd.SrcRect.Left;
        float srcRight = cmd.SrcRect.Right;
        float srcTop = cmd.SrcRect.Top;
        float srcBottom = cmd.SrcRect.Bottom;

        if (GameSettings.Instance.UseHalfTexelOffset)
        {
            float texWidth = cmd.Texture.Size.X;
            float texHeight = cmd.Texture.Size.Y;

            float offsetX = 0.5f / texWidth;
            float offsetY = 0.5f / texHeight;

            srcLeft -= offsetX;
            srcRight += offsetX;
            srcTop -= offsetY;
            srcBottom += offsetY;
        }

        if (cmd.Effects.HasFlag(TextureEffects.Horizontal))
            (srcLeft, srcRight) = (srcRight, srcLeft);
        if (cmd.Effects.HasFlag(TextureEffects.Vertical))
            (srcTop, srcBottom) = (srcBottom, srcTop);

        var color = cmd.Color;

        ptr[0] = new SFVertex(corners[0], color, new(srcLeft, srcTop));
        ptr[1] = new SFVertex(corners[1], color, new(srcRight, srcTop));
        ptr[2] = new SFVertex(corners[2], color, new(srcLeft, srcBottom));
        ptr[3] = new SFVertex(corners[1], color, new(srcRight, srcTop));
        ptr[4] = new SFVertex(corners[3], color, new(srcRight, srcBottom));
        ptr[5] = new SFVertex(corners[2], color, new(srcLeft, srcBottom));
    }

    private sealed class DrawCommandComparer : IComparer<DrawCommand>
    {
        private SortMode _sortMode;

        public DrawCommandComparer(SortMode sortMode)
        {
            _sortMode = sortMode;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare(DrawCommand a, DrawCommand b)
        {
            uint texA = a.Texture.NativeHandle;
            uint texB = b.Texture.NativeHandle;

            if (texA < texB) return -1;
            if (texA > texB) return 1;

            if (_sortMode == SortMode.BackToFront)
            {
                if (a.Depth < b.Depth) return -1;
                if (a.Depth > b.Depth) return 1;
                return 0;
            }
            else if (_sortMode == SortMode.FrontToBack)
            {
                if (b.Depth < a.Depth) return -1;
                if (b.Depth > a.Depth) return 1;
                return 0;
            }

            return 0;
        }

        public void UpdateMode(SortMode sortMode) => _sortMode = sortMode;
    }
}