using System.Runtime.CompilerServices;

using Void.Engine.Graphics.RenderTargets;
using Void.Engine.Logs;

namespace Void.Engine.Graphics;

public enum TextAlignment
{
    TopLeft,
    TopCenter,
    TopRight,
    CenterLeft,
    Center,
    CenterRight,
    BottomLeft,
    BottomCenter,
    BottomRight
}

public enum TextWrapMode
{
    /// <summary>No wrapping. Text may extend beyond the bounds.</summary>
    None,
    /// <summary>Wraps at word boundaries (spaces).</summary>
    Word,
    /// <summary>Wraps at character boundaries (any character).</summary>
    Character
}

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
        _capacity = capacity > 0 ? capacity : GetDefaultCapacity();
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
        fixed (SFVertex* vertexPtr = _vertexData)
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
    {
        base.SetRenderStateForGroup(commandIndex);

        var texture = _cmds[commandIndex].Texture;
        _renderStates.Texture = texture;

        if (_currentShader is Shader shader)
            shader.SetUniform("uTexture", texture);
    }

    protected override void ResizeBuffers()
    {
        Logger.Instance.DebugWithCategory("SpriteBatcher",
            "Resizing buffers: {0} -> {1} commands", _cmds.Length, _cmds.Length * 2);

        int newSize = _cmds.Length * 2;
        Array.Resize(ref _cmds, newSize);

        int newVertexSize = newSize * VerticesPerQuad;
        Array.Resize(ref _vertexData, newVertexSize);
        _vertexBufferSize = newVertexSize;

        _vertexBuffer?.Dispose();
        _vertexBuffer = new VertexBuffer(newVertexSize);
        _capacity = newSize;
    }

    #region Draw Methods

    public void Draw(Texture texture, Rect2 dstRect, Rect2 srcRect, Color color, float depth = 0f)
        => EngineDraw(texture, dstRect, srcRect, color, 0f, Vect2.One, Vect2.Zero, TextureEffects.None, depth, texture.Type == AssetType.Normal);

    public void Draw(Texture texture, Rect2 rect, Color color, float depth = 0f)
        => EngineDraw(texture, rect, texture.Bounds, color, 0f, Vect2.One, Vect2.Zero, TextureEffects.None, depth, texture.Type == AssetType.Normal);

    public void Draw(Texture texture, Vect2 position, Rect2 srcRect, Color color, float depth = 0f)
        => EngineDraw(texture, new(position, srcRect.Size), srcRect, color, 0f, Vect2.One, Vect2.Zero, TextureEffects.None, depth, texture.Type == AssetType.Normal);

    public void Draw(Texture texture, Rect2 dstRect, Rect2 srcRect, Color color, float rotation, Vect2 scale, Vect2 origin, TextureEffects effects, float depth)
        => EngineDraw(texture, dstRect, srcRect, color, rotation, scale, origin, effects, depth, texture.Type == AssetType.Normal);

    public void Draw(Texture texture, Rect2 rect, Color color, float rotation, Vect2 scale, Vect2 origin, TextureEffects effects, float depth)
        => EngineDraw(texture, rect, texture.Bounds, color, rotation, scale, origin, effects, depth, texture.Type == AssetType.Normal);

    public void Draw(Texture texture, Vect2 position, Rect2 srcRect, Color color, float rotation, Vect2 scale, Vect2 origin, TextureEffects effects, float depth)
        => EngineDraw(texture, new(position, srcRect.Size), srcRect, color, rotation, scale, origin, effects, depth, texture.Type == AssetType.Normal);

    public void Draw(Texture texture, Vect2 position, Color color, float depth = 0f)
        => EngineDraw(texture, new Rect2(position.X, position.Y, texture.Size.X, texture.Size.Y), texture.Bounds, color, 0f, Vect2.One, Vect2.Zero, TextureEffects.None, depth, texture.Type == AssetType.Normal);

    public void Draw(Texture texture, Vect2 position, Rect2 srcRect, Color color, float rotation, float depth = 0f)
        => EngineDraw(texture, new Rect2(position.X, position.Y, srcRect.Width, srcRect.Height), srcRect, color, rotation, Vect2.One, Vect2.Zero, TextureEffects.None, depth, texture.Type == AssetType.Normal);

    public void Draw(Texture texture, Rect2 dstRect, Color color, float rotation, float depth = 0f)
        => EngineDraw(texture, dstRect, texture.Bounds, color, rotation, Vect2.One, Vect2.Zero, TextureEffects.None, depth, texture.Type == AssetType.Normal);

    public void Draw(Texture texture, Vect2 position, Color color, float rotation, Vect2 scale, float depth = 0f)
        => EngineDraw(texture, new Rect2(position.X, position.Y, texture.Size.X * scale.X, texture.Size.Y * scale.Y), texture.Bounds, color, rotation, scale, Vect2.Zero, TextureEffects.None, depth, texture.Type == AssetType.Normal);

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

    public void DrawBypassAtlas(Texture texture, Vect2 position, Color color, float depth = 0f)
        => EngineDrawBypassAtlas(texture, new Rect2(position.X, position.Y, texture.Size.X, texture.Size.Y), texture.Bounds, color, 0f, Vect2.One, Vect2.Zero, TextureEffects.None, depth);

    public void DrawBypassAtlas(Texture texture, Vect2 position, Rect2 srcRect, Color color, float rotation, float depth = 0f)
        => EngineDrawBypassAtlas(texture, new Rect2(position.X, position.Y, srcRect.Width, srcRect.Height), srcRect, color, rotation, Vect2.One, Vect2.Zero, TextureEffects.None, depth);

    public void DrawBypassAtlas(Texture texture, Rect2 dstRect, Color color, float rotation, float depth = 0f)
        => EngineDrawBypassAtlas(texture, dstRect, texture.Bounds, color, rotation, Vect2.One, Vect2.Zero, TextureEffects.None, depth);

    public void DrawBypassAtlas(Texture texture, Vect2 position, Color color, float rotation, Vect2 scale, float depth = 0f)
        => EngineDrawBypassAtlas(texture, new Rect2(position.X, position.Y, texture.Size.X * scale.X, texture.Size.Y * scale.Y), texture.Bounds, color, rotation, scale, Vect2.Zero, TextureEffects.None, depth);

    #endregion

    #region DrawText Methods

    public void DrawText(Font font, string text, Vect2 position, Color color)
        => DrawTextPosition(font, text, position, color, 0f, Vect2.One, TextAlignment.TopLeft);

    public void DrawText(Font font, string text, Vect2 position, Color color, Vect2 scale)
        => DrawTextPosition(font, text, position, color, 0f, scale, TextAlignment.TopLeft);

    public void DrawText(Font font, string text, Vect2 position, Color color, float depth)
        => DrawTextPosition(font, text, position, color, depth, Vect2.One, TextAlignment.TopLeft);

    public void DrawText(Font font, string text, Vect2 position, Color color, Vect2 scale, float depth)
        => DrawTextPosition(font, text, position, color, depth, scale, TextAlignment.TopLeft);

    public void DrawText(Font font, string text, Vect2 position, Color color, TextAlignment alignment)
        => DrawTextPosition(font, text, position, color, 0f, Vect2.One, alignment);

    public void DrawText(Font font, string text, Vect2 position, Color color, TextAlignment alignment, Vect2 scale)
        => DrawTextPosition(font, text, position, color, 0f, scale, alignment);

    public void DrawText(Font font, string text, Vect2 position, Color color, TextAlignment alignment, Vect2 scale, float depth)
        => DrawTextPosition(font, text, position, color, depth, scale, alignment);

    public void DrawText(Font font, string text, Rect2 bounds, Color color)
        => DrawTextBounds(font, text, bounds, color, 0f, Vect2.One, TextAlignment.TopLeft, TextWrapMode.None);

    public void DrawText(Font font, string text, Rect2 bounds, Color color, Vect2 scale)
        => DrawTextBounds(font, text, bounds, color, 0f, scale, TextAlignment.TopLeft, TextWrapMode.None);

    public void DrawText(Font font, string text, Rect2 bounds, Color color, float depth)
        => DrawTextBounds(font, text, bounds, color, depth, Vect2.One, TextAlignment.TopLeft, TextWrapMode.None);

    public void DrawText(Font font, string text, Rect2 bounds, Color color, Vect2 scale, float depth,
        TextAlignment alignment = TextAlignment.TopLeft, TextWrapMode wrapMode = TextWrapMode.None)
        => DrawTextBounds(font, text, bounds, color, depth, scale, alignment, wrapMode);

    #endregion

    public void DrawNinePatch(Texture texture, Rect2 dstRect, Rect2 sourceRect, Rect2 corners, Color color, float depth = 0f)
    {
        var dstRects = CalculateNinePatchRects(dstRect, corners);
        var srcRects = GetNinePatchSourceRects(sourceRect, corners);

        for (int i = 0; i < 9; i++)
        {
            EngineDrawBypassAtlas(texture, dstRects[i], srcRects[i], color, 0f, Vect2.One, Vect2.Zero, TextureEffects.None, depth);
        }
    }

    #region Private Methods

    private Rect2[] CalculateNinePatchRects(Rect2 dstRect, Rect2 corners)
    {
        var result = new Rect2[9];
        float leftBorder = corners.X, topBorder = corners.Y;
        float rightBorder = corners.Width, bottomBorder = corners.Height;
        float dstX = dstRect.X, dstY = dstRect.Y;
        float dstWidth = dstRect.Width, dstHeight = dstRect.Height;
        float middleWidth = dstWidth - leftBorder - rightBorder;
        float middleHeight = dstHeight - topBorder - bottomBorder;

        // Top row
        result[0] = new Rect2(dstX, dstY, leftBorder, topBorder);
        result[1] = new Rect2(dstX + leftBorder, dstY, middleWidth, topBorder);
        result[2] = new Rect2(dstX + leftBorder + middleWidth, dstY, rightBorder, topBorder);
        // Middle row
        result[3] = new Rect2(dstX, dstY + topBorder, leftBorder, middleHeight);
        result[4] = new Rect2(dstX + leftBorder, dstY + topBorder, middleWidth, middleHeight);
        result[5] = new Rect2(dstX + leftBorder + middleWidth, dstY + topBorder, rightBorder, middleHeight);
        // Bottom row
        result[6] = new Rect2(dstX, dstY + topBorder + middleHeight, leftBorder, bottomBorder);
        result[7] = new Rect2(dstX + leftBorder, dstY + topBorder + middleHeight, middleWidth, bottomBorder);
        result[8] = new Rect2(dstX + leftBorder + middleWidth, dstY + topBorder + middleHeight, rightBorder, bottomBorder);

        return result;
    }

    private Rect2[] GetNinePatchSourceRects(Rect2 sourceRect, Rect2 corners)
    {
        var result = new Rect2[9];
        float leftBorder = corners.X, topBorder = corners.Y;
        float rightBorder = corners.Width, bottomBorder = corners.Height;
        float srcX = sourceRect.X, srcY = sourceRect.Y;
        float srcW = sourceRect.Width, srcH = sourceRect.Height;
        float middleWidth = srcW - leftBorder - rightBorder;
        float middleHeight = srcH - topBorder - bottomBorder;

        // Top row
        result[0] = new Rect2(srcX, srcY, leftBorder, topBorder);
        result[1] = new Rect2(srcX + leftBorder, srcY, middleWidth, topBorder);
        result[2] = new Rect2(srcX + leftBorder + middleWidth, srcY, rightBorder, topBorder);
        // Middle row
        result[3] = new Rect2(srcX, srcY + topBorder, leftBorder, middleHeight);
        result[4] = new Rect2(srcX + leftBorder, srcY + topBorder, middleWidth, middleHeight);
        result[5] = new Rect2(srcX + leftBorder + middleWidth, srcY + topBorder, rightBorder, middleHeight);
        // Bottom row
        result[6] = new Rect2(srcX, srcY + topBorder + middleHeight, leftBorder, bottomBorder);
        result[7] = new Rect2(srcX + leftBorder, srcY + topBorder + middleHeight, middleWidth, bottomBorder);
        result[8] = new Rect2(srcX + leftBorder + middleWidth, srcY + topBorder + middleHeight, rightBorder, bottomBorder);

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EngineDraw(SFTexture texture, Rect2 dstRect, Rect2 srcRect, Color color, float rotation, Vect2 scale,
        Vect2 origin, TextureEffects effects, float depth, bool canPack)
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(SpriteBatcher));
        if (!_isDrawing) throw new InvalidOperationException("Cannot draw outside Begin/End");
        if (!IsVisible(dstRect)) return;
        if (_cmdCount >= _cmds.Length) ResizeBuffers();

        // if (canPack)
        // {
        //     Logger.Instance.DebugWithCategory("SpriteBatcher",
        //         "Attempting to pack: valid={0}, size={1}x{2}",
        //         !texture.IsInvalid, srcRect.Width, srcRect.Height);
        // }

        var scaledDstRect = new Rect2(dstRect.X, dstRect.Y, dstRect.Width * scale.X, dstRect.Height * scale.Y);

        if (canPack && AtlasManager.Instance.TryPack(texture, srcRect, out var packedRect, out var pageId))
        {
            _cmds[_cmdCount] = new DrawCommand
            {
                Texture = AtlasManager.Instance.GetPageTexture(pageId),
                Depth = depth,
                DstRect = scaledDstRect,
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
                DstRect = scaledDstRect,
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
        if (_isDisposed) throw new ObjectDisposedException(nameof(SpriteBatcher));
        if (!_isDrawing) throw new InvalidOperationException("Cannot draw outside Begin/End");
        if (!IsVisible(dstRect)) return;
        if (_cmdCount >= _cmds.Length) ResizeBuffers();

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
                corners[i] = new Vect2(centerX + dx * cos - dy * sin, centerY + dx * sin + dy * cos);
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

    #endregion

    #region Text Processing

    private void DrawTextPosition(Font font, string text, Vect2 position, Color color, float depth, Vect2 scale, TextAlignment alignment)
    {
        if (string.IsNullOrEmpty(text) || font == null || !font.IsValid) return;

        var textSize = font.Measure(text) * scale;
        var textRect = new Rect2(position, textSize);
        if (!IsVisible(textRect)) return;

        Vect2 topLeft = alignment switch
        {
            TextAlignment.TopLeft => position,
            TextAlignment.TopCenter => new(position.X - textSize.X / 2f, position.Y),
            TextAlignment.TopRight => new(position.X - textSize.X, position.Y),
            TextAlignment.CenterLeft => new(position.X, position.Y - textSize.Y / 2f),
            TextAlignment.Center => new(position.X - textSize.X / 2f, position.Y - textSize.Y / 2f),
            TextAlignment.CenterRight => new(position.X - textSize.X, position.Y - textSize.Y / 2f),
            TextAlignment.BottomLeft => new(position.X, position.Y - textSize.Y),
            TextAlignment.BottomCenter => new(position.X - textSize.X / 2f, position.Y - textSize.Y),
            TextAlignment.BottomRight => new(position.X - textSize.X, position.Y - textSize.Y),
            _ => position
        };

        DrawTextBounds(font, text, new Rect2(topLeft, textSize), color, depth, scale, alignment, TextWrapMode.None);
    }

    private void DrawTextBounds(Font font, string text, Rect2 bounds, Color color, float depth, Vect2 scale, TextAlignment alignment, TextWrapMode wrapMode)
    {
        if (string.IsNullOrEmpty(text) || font == null || !font.IsValid) return;
        if (!IsVisible(bounds)) return;

        string[] lines = text.Split('\n');
        float lineHeight = (font.LineHeight + font.LineSpacing) * scale.Y;
        float totalHeight = lines.Length * lineHeight;

        float startY = bounds.Y;
        if (alignment is TextAlignment.CenterLeft or TextAlignment.Center or TextAlignment.CenterRight)
            startY = bounds.Y + (bounds.Height - totalHeight) / 2f;
        else if (alignment is TextAlignment.BottomLeft or TextAlignment.BottomCenter or TextAlignment.BottomRight)
            startY = bounds.Y + bounds.Height - totalHeight;

        float currentY = startY;
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrEmpty(line))
            {
                currentY += lineHeight;
                continue;
            }

            switch (wrapMode)
            {
                case TextWrapMode.Word:
                    ProcessWordWrappedLine(line, font, bounds, color, depth, scale, alignment, ref currentY, lineHeight);
                    break;
                case TextWrapMode.Character:
                    ProcessCharWrappedLine(line, font, bounds, color, depth, scale, alignment, ref currentY, lineHeight);
                    break;
                default:
                    ProcessLine(line, font, bounds, color, depth, scale, alignment, currentY);
                    currentY += lineHeight;
                    break;
            }
        }
    }

    private void ProcessLine(string line, Font font, Rect2 bounds, Color color, float depth, Vect2 scale, TextAlignment alignment, float y)
    {
        float lineWidth = 0;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            lineWidth += c == '\t' ? font.GetGlyph(' ').Advance * 4 * scale.X : font.GetGlyph(c).Advance * scale.X;
        }

        float startX = bounds.X;
        if (alignment is TextAlignment.TopCenter or TextAlignment.Center or TextAlignment.BottomCenter)
            startX = bounds.X + (bounds.Width - lineWidth) / 2f;
        else if (alignment is TextAlignment.TopRight or TextAlignment.CenterRight or TextAlignment.BottomRight)
            startX = bounds.X + bounds.Width - lineWidth;

        float currentX = startX;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '\t')
            {
                currentX += font.GetGlyph(' ').Advance * 4 * scale.X;
                continue;
            }

            Glyph glyph = font.GetGlyph(c);
            if (glyph.IsEmpty) continue;

            Rect2 dstRect = new(currentX + glyph.Offset.X * scale.X, y + glyph.Offset.Y * scale.Y, glyph.Size.X * scale.X, glyph.Size.Y * scale.Y);
            EngineDraw(font, dstRect, new Rect2(glyph.Position.X, glyph.Position.Y, glyph.Size.X, glyph.Size.Y), color, 0f, Vect2.One, Vect2.Zero, TextureEffects.None, depth, font.Type == AssetType.Normal);
            currentX += glyph.Advance * scale.X;
        }
    }

    private void ProcessWordWrappedLine(string line, Font font, Rect2 bounds, Color color, float depth, Vect2 scale, TextAlignment alignment, ref float y, float lineHeight)
    {
        string[] words = line.Split(' ');
        float currentX = bounds.X;
        float currentY = y;
        float spaceWidth = font.GetGlyph(' ').Advance * scale.X;

        for (int i = 0; i < words.Length; i++)
        {
            string word = words[i];
            if (string.IsNullOrEmpty(word)) continue;

            float wordWidth = 0;
            for (int j = 0; j < word.Length; j++)
                wordWidth += font.GetGlyph(word[j]).Advance * scale.X;

            if (currentX + wordWidth > bounds.X + bounds.Width && i > 0)
            {
                currentY += lineHeight;
                currentX = bounds.X;
                if (currentY + lineHeight > bounds.Y + bounds.Height) break;
            }

            for (int j = 0; j < word.Length; j++)
            {
                Glyph glyph = font.GetGlyph(word[j]);
                if (glyph.IsEmpty) continue;
                Rect2 dstRect = new(currentX + glyph.Offset.X * scale.X, currentY + glyph.Offset.Y * scale.Y, glyph.Size.X * scale.X, glyph.Size.Y * scale.Y);
                EngineDraw(font, dstRect, new Rect2(glyph.Position.X, glyph.Position.Y, glyph.Size.X, glyph.Size.Y), color, 0f, Vect2.One, Vect2.Zero, TextureEffects.None, depth, font.Type == AssetType.Normal);
                currentX += glyph.Advance * scale.X;
            }
            currentX += spaceWidth;
        }

        y = currentY + lineHeight;
    }

    private void ProcessCharWrappedLine(string line, Font font, Rect2 bounds, Color color, float depth, Vect2 scale, TextAlignment alignment, ref float y, float lineHeight)
    {
        float currentX = bounds.X;
        float currentY = y;

        for (int i = 0; i < line.Length; i++)
        {
            Glyph glyph = font.GetGlyph(line[i]);
            if (glyph.IsEmpty) continue;

            float charWidth = glyph.Advance * scale.X;
            if (currentX + charWidth > bounds.X + bounds.Width)
            {
                currentY += lineHeight;
                currentX = bounds.X;
                if (currentY + lineHeight > bounds.Y + bounds.Height) break;
            }

            Rect2 dstRect = new(currentX + glyph.Offset.X * scale.X, currentY + glyph.Offset.Y * scale.Y, glyph.Size.X * scale.X, glyph.Size.Y * scale.Y);
            EngineDraw(font, dstRect, new Rect2(glyph.Position.X, glyph.Position.Y, glyph.Size.X, glyph.Size.Y), color, 0f, Vect2.One, Vect2.Zero, TextureEffects.None, depth, font.Type == AssetType.Normal);
            currentX += charWidth;
        }

        y = currentY + lineHeight;
    }

    private bool IsVisible(Rect2 dstRect)
        => _currentCamera == null || dstRect.Intersects(_currentCamera.ViewBounds);

    #endregion

    private sealed class DrawCommandComparer : IComparer<DrawCommand>
    {
        private SortMode _sortMode;

        public DrawCommandComparer(SortMode sortMode) => _sortMode = sortMode;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare(DrawCommand a, DrawCommand b)
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

            uint texA = a.Texture.NativeHandle;
            uint texB = b.Texture.NativeHandle;
            if (texA < texB) return -1;
            if (texA > texB) return 1;
            return 0;
        }

        public void UpdateMode(SortMode sortMode) => _sortMode = sortMode;
    }
}