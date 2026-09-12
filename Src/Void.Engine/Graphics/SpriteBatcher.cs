using Void.Engine.Graphics.RenderTargets;
using RenderVertex = Void.Engine.Graphics.Rendering.Vertex;

// ============================================================================
//  SpriteBatcher.cs
// ============================================================================
//  Batched sprite, text, and nine-patch rendering with optional atlas packing.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Graphics;

/// <summary>Specifies how text is positioned relative to a point or bounds.</summary>
public enum TextAlignment
{
    /// <summary>Aligns text to the top-left.</summary>
    TopLeft,
    /// <summary>Centers text horizontally at the top.</summary>
    TopCenter,
    /// <summary>Aligns text to the top-right.</summary>
    TopRight,
    /// <summary>Centers text vertically on the left.</summary>
    CenterLeft,
    /// <summary>Centers text horizontally and vertically.</summary>
    Center,
    /// <summary>Centers text vertically on the right.</summary>
    CenterRight,
    /// <summary>Aligns text to the bottom-left.</summary>
    BottomLeft,
    /// <summary>Centers text horizontally at the bottom.</summary>
    BottomCenter,
    /// <summary>Aligns text to the bottom-right.</summary>
    BottomRight
}

/// <summary>Specifies how text is wrapped inside bounds.</summary>
public enum TextWrapMode
{
    /// <summary>Does not wrap text.</summary>
    None,
    /// <summary>Wraps at spaces between words.</summary>
    Word,
    /// <summary>Wraps at individual character boundaries.</summary>
    Character
}

/// <summary>
/// Batches textured quads, bitmap-font glyphs, and nine-patch UI geometry.
/// </summary>
/// <remarks>
/// <para>
/// Normal textures and fonts may be packed through <see cref="AtlasManager"/>.
/// The <c>DrawBypassAtlas</c> overloads and nine-patch drawing keep the original
/// texture source instead. A camera supplied to <see cref="BaseBatcher.Begin"/>
/// also enables coarse destination-rectangle culling for this batch.
/// </para>
/// <para>
/// Sprites are uploaded as indexed quads. Compatible commands are grouped by
/// texture source after any requested depth sort.
/// </para>
/// <para>
/// Text rendering ignores carriage returns. Tab characters advance by four spaces
/// so drawing and <see cref="Font.Measure"/> use the same whitespace semantics.
/// </para>
/// <para><code>
/// using var batcher = new SpriteBatcher();
/// batcher.Begin(camera: camera);
/// batcher.Draw(texture, new Vect2(32, 32), Color.White);
/// batcher.DrawText(font, "VOID", new Vect2(160, 20), Color.White, TextAlignment.TopCenter);
/// batcher.End();
/// </code></para>
/// </remarks>
public sealed class SpriteBatcher : BaseBatcher
{
    private struct DrawCommand
    {
        public Texture Texture;
        public Font Font;
        public float Depth;
        public Rect2 DstRect;
        public Rect2 SrcRect;
        public Color Color;
        public float Rotation;
        public Vect2 Scale;
        public Vect2 Origin;
        public TextureEffects Effects;
    }

    private readonly struct TextLineLayout
    {
        internal string Text { get; }
        internal float Width { get; }

        internal TextLineLayout(string text, float width)
        {
            Text = text;
            Width = width;
        }
    }

    private const int VerticesPerQuad = 4;
    private const int IndicesPerQuad = 6;

    private DrawCommand[] _cmds;
    private readonly DrawCommandComparer _comparer;
    private IndexBuffer _indexBuffer;
    private Rect2 _batchViewBounds;
    private bool _hasBatchViewBounds;

    /// <summary>Gets the diagnostic name of this batcher.</summary>
    public override string Name => "SpriteBatcher";

    /// <summary>Gets the number of unique vertices stored for each indexed quad.</summary>
    protected override int VerticesPerCommand => VerticesPerQuad;

    /// <summary>Initializes a sprite batcher.</summary>
    /// <param name="capacity">Initial command capacity, or zero to use the configured default.</param>
    public SpriteBatcher(int capacity = 0) : base(capacity)
    {
        _cmds = new DrawCommand[_capacity];
        _comparer = new DrawCommandComparer(_sortMode);
        _indexBuffer = new IndexBuffer(_capacity);
    }

    #region Protected

    /// <summary>Gets the configured default sprite command capacity.</summary>
    /// <returns><see cref="GameSettings.SpriteBatchCapacity"/>.</returns>
    protected override int GetDefaultCapacity() => GameSettings.Instance.SpriteBatchCapacity;

    /// <summary>Prepares atlas usage, optional defrag work, and camera culling state.</summary>
    protected override void OnBegin()
    {
        _hasBatchViewBounds = _currentCamera != null;
        _batchViewBounds = _hasBatchViewBounds ? _currentCamera.ViewBounds : default;

        AtlasManager.Instance.BeginBatchUsage();
        AtlasManager.Instance.ProcessPendingDefragMoves(
            GameSettings.Instance.AtlasDefragMovesPerFrame);

        base.OnBegin();
    }

    /// <summary>Handles the end-of-batch hook.</summary>
    protected override void OnEnd() { }

    /// <summary>Handles the post-flush hook.</summary>
    protected override void OnFlush() { }

    /// <summary>Sorts queued sprite commands using the active sort mode.</summary>
    protected override void SortCommands()
    {
        _comparer.UpdateMode(_sortMode);
        Array.Sort(_cmds, 0, _cmdCount, _comparer);
    }

    /// <summary>Builds four vertices for each queued sprite command.</summary>
    protected override unsafe void BuildVertices()
    {
        fixed (RenderVertex* vertexPtr = _vertexData)
        {
            RenderVertex* currentPtr = vertexPtr;
            for (int i = 0; i < _cmdCount; i++)
            {
                WriteQuadUnsafe(currentPtr, _cmds[i]);
                currentPtr += VerticesPerQuad;
            }
        }
    }

    /// <summary>Determines whether two commands resolve to the same texture source.</summary>
    /// <param name="indexA">Index of the first command.</param>
    /// <param name="indexB">Index of the command being tested.</param>
    /// <returns>True when both commands use the same texture key.</returns>
    protected override bool CanBatchTogether(int indexA, int indexB)
        => GetTextureKey(_cmds[indexA]) == GetTextureKey(_cmds[indexB]);

    /// <summary>Converts indexed-quad vertex count into a triangle count.</summary>
    /// <param name="totalVertices">Number of unique quad vertices.</param>
    /// <returns>Two triangles per four vertices.</returns>
    protected override int GetTriangleCount(int totalVertices)
        => totalVertices / 2;

    /// <summary>Submits a compatible group with the shared quad index buffer.</summary>
    /// <param name="commandStart">First command in the group.</param>
    /// <param name="commandCount">Number of commands in the group.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the batcher's vertex buffer is not VOID's renderer-neutral <see cref="VertexBuffer"/>.
    /// </exception>
    protected override void SubmitGroup(int commandStart, int commandCount)
    {
        if (_vertexBuffer is not VertexBuffer vertexBuffer)
            throw new InvalidOperationException("SpriteBatcher requires the renderer-neutral VertexBuffer implementation.");

        int vertexStart = checked(commandStart * VerticesPerQuad);
        int vertexCount = checked(commandCount * VerticesPerQuad);
        int indexStart = checked(commandStart * IndicesPerQuad);
        int indexCount = checked(commandCount * IndicesPerQuad);

        vertexBuffer.DrawIndexed(
            _renderTarget,
            _indexBuffer,
            checked((uint)vertexStart),
            checked((uint)vertexCount),
            checked((uint)indexStart),
            checked((uint)indexCount),
            _renderStates);
    }

    /// <summary>Applies the texture or font source for a command group.</summary>
    /// <param name="commandIndex">Index of the first command in the group.</param>
    protected override void SetRenderStateForGroup(int commandIndex)
    {
        base.SetRenderStateForGroup(commandIndex);

        var cmd = _cmds[commandIndex];
        _renderStates.Texture = cmd.Texture;
        _renderStates.Font = cmd.Font;

    }

    /// <summary>Doubles command, vertex, and quad-index storage.</summary>
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

        _indexBuffer?.Dispose();
        _indexBuffer = new IndexBuffer(newSize);

        _capacity = newSize;
    }
    #endregion

    #region DrawAtlasDebugPage

    /// <summary>Draws one atlas page directly, bypassing atlas lookup for the draw itself.</summary>
    /// <param name="pageId">Atlas page index.</param>
    /// <param name="dstRect">Destination rectangle.</param>
    /// <param name="depth">Depth value used by sorted modes.</param>
    /// <remarks>A missing atlas page is ignored.</remarks>
    public void DrawAtlasDebugPage(int pageId, Rect2 dstRect, float depth = 0.999f)
    {
        var pageTexture = AtlasManager.Instance.GetPageTexture(pageId);
        if (pageTexture == null)
            return;

        EngineDrawBypassAtlas(pageTexture, dstRect, new Rect2(Vect2.Zero, pageTexture.Size), Color.White, 0f, Vect2.One, Vect2.Zero, TextureEffects.None, depth);
    }
    #endregion

    #region Draw Methods

    /// <summary>Draws a source rectangle into a destination rectangle.</summary>
    public void Draw(Texture texture, Rect2 dstRect, Rect2 srcRect, Color color, float depth = 0f)
        => EngineDraw(texture, dstRect, srcRect, color, 0f, Vect2.One, Vect2.Zero, TextureEffects.None, depth, texture.Type == AssetType.Normal);

    /// <summary>Draws the full texture into a destination rectangle.</summary>
    public void Draw(Texture texture, Rect2 rect, Color color, float depth = 0f)
        => EngineDraw(texture, rect, texture.Bounds, color, 0f, Vect2.One, Vect2.Zero, TextureEffects.None, depth, texture.Type == AssetType.Normal);

    /// <summary>Draws a source rectangle at the supplied position using its source size.</summary>
    public void Draw(Texture texture, Vect2 position, Rect2 srcRect, Color color, float depth = 0f)
        => EngineDraw(texture, new(position, srcRect.Size), srcRect, color, 0f, Vect2.One, Vect2.Zero, TextureEffects.None, depth, texture.Type == AssetType.Normal);

    /// <summary>Draws a transformed source rectangle into a destination rectangle.</summary>
    public void Draw(Texture texture, Rect2 dstRect, Rect2 srcRect, Color color, Vect2 scale, float rotation, Vect2 origin, TextureEffects effects, float depth)
        => EngineDraw(texture, dstRect, srcRect, color, rotation, scale, origin, effects, depth, texture.Type == AssetType.Normal);

    /// <summary>Draws the transformed full texture into a destination rectangle.</summary>
    public void Draw(Texture texture, Rect2 rect, Color color, Vect2 scale, float rotation, Vect2 origin, TextureEffects effects, float depth)
        => EngineDraw(texture, rect, texture.Bounds, color, rotation, scale, origin, effects, depth, texture.Type == AssetType.Normal);

    /// <summary>Draws a transformed source rectangle at the supplied position.</summary>
    public void Draw(Texture texture, Vect2 position, Rect2 srcRect, Color color, Vect2 scale, float rotation, Vect2 origin, TextureEffects effects, float depth)
        => EngineDraw(texture, new(position, srcRect.Size), srcRect, color, rotation, scale, origin, effects, depth, texture.Type == AssetType.Normal);

    /// <summary>Draws the full texture at its natural size.</summary>
    public void Draw(Texture texture, Vect2 position, Color color, float depth = 0f)
        => EngineDraw(texture, new Rect2(position.X, position.Y, texture.Size.X, texture.Size.Y), texture.Bounds, color, 0f, Vect2.One, Vect2.Zero, TextureEffects.None, depth, texture.Type == AssetType.Normal);

    /// <summary>Draws and rotates a source rectangle at the supplied position.</summary>
    public void Draw(Texture texture, Vect2 position, Rect2 srcRect, Color color, float rotation, float depth = 0f)
        => EngineDraw(texture, new Rect2(position.X, position.Y, srcRect.Width, srcRect.Height), srcRect, color, rotation, Vect2.One, Vect2.Zero, TextureEffects.None, depth, texture.Type == AssetType.Normal);

    /// <summary>Draws and rotates the full texture into a destination rectangle.</summary>
    public void Draw(Texture texture, Rect2 dstRect, Color color, float rotation, float depth = 0f)
        => EngineDraw(texture, dstRect, texture.Bounds, color, rotation, Vect2.One, Vect2.Zero, TextureEffects.None, depth, texture.Type == AssetType.Normal);

    /// <summary>Draws the full texture with scale and rotation.</summary>
    public void Draw(Texture texture, Vect2 position, Color color, Vect2 scale, float rotation, float depth = 0f)
        => EngineDraw(texture, new Rect2(position.X, position.Y, texture.Size.X, texture.Size.Y), texture.Bounds, color, rotation, scale, Vect2.Zero, TextureEffects.None, depth, texture.Type == AssetType.Normal);

    #endregion

    #region DrawBypassAtlas Methods

    /// <summary>Draws a source rectangle without attempting atlas packing.</summary>
    public void DrawBypassAtlas(Texture texture, Rect2 dstRect, Rect2 srcRect, Color color, float depth = 0f)
        => EngineDrawBypassAtlas(texture, dstRect, srcRect, color, 0f, Vect2.One, Vect2.Zero, TextureEffects.None, depth);

    /// <summary>Draws the full texture without attempting atlas packing.</summary>
    public void DrawBypassAtlas(Texture texture, Rect2 rect, Color color, float depth = 0f)
        => EngineDrawBypassAtlas(texture, rect, texture.Bounds, color, 0f, Vect2.One, Vect2.Zero, TextureEffects.None, depth);

    /// <summary>Draws a source rectangle at a position without attempting atlas packing.</summary>
    public void DrawBypassAtlas(Texture texture, Vect2 position, Rect2 srcRect, Color color, float depth = 0f)
        => EngineDrawBypassAtlas(texture, new(position, srcRect.Size), srcRect, color, 0f, Vect2.One, Vect2.Zero, TextureEffects.None, depth);

    /// <summary>Draws transformed texture data without attempting atlas packing.</summary>
    public void DrawBypassAtlas(Texture texture, Rect2 dstRect, Rect2 srcRect, Color color, Vect2 scale, float rotation, Vect2 origin, TextureEffects effects, float depth)
        => EngineDrawBypassAtlas(texture, dstRect, srcRect, color, rotation, scale, origin, effects, depth);

    /// <summary>Draws the transformed full texture without attempting atlas packing.</summary>
    public void DrawBypassAtlas(Texture texture, Rect2 rect, Color color, Vect2 scale, float rotation, Vect2 origin, TextureEffects effects, float depth)
        => EngineDrawBypassAtlas(texture, rect, texture.Bounds, color, rotation, scale, origin, effects, depth);

    /// <summary>Draws a transformed source rectangle at a position without atlas packing.</summary>
    public void DrawBypassAtlas(Texture texture, Vect2 position, Rect2 srcRect, Color color, Vect2 scale, float rotation, Vect2 origin, TextureEffects effects, float depth)
        => EngineDrawBypassAtlas(texture, new(position, srcRect.Size), srcRect, color, rotation, scale, origin, effects, depth);

    /// <summary>Draws the full texture at natural size without atlas packing.</summary>
    public void DrawBypassAtlas(Texture texture, Vect2 position, Color color, float depth = 0f)
        => EngineDrawBypassAtlas(texture, new Rect2(position.X, position.Y, texture.Size.X, texture.Size.Y), texture.Bounds, color, 0f, Vect2.One, Vect2.Zero, TextureEffects.None, depth);

    /// <summary>Draws and rotates a source rectangle without atlas packing.</summary>
    public void DrawBypassAtlas(Texture texture, Vect2 position, Rect2 srcRect, Color color, float rotation, float depth = 0f)
        => EngineDrawBypassAtlas(texture, new Rect2(position.X, position.Y, srcRect.Width, srcRect.Height), srcRect, color, rotation, Vect2.One, Vect2.Zero, TextureEffects.None, depth);

    /// <summary>Draws and rotates the full texture without atlas packing.</summary>
    public void DrawBypassAtlas(Texture texture, Rect2 dstRect, Color color, float rotation, float depth = 0f)
        => EngineDrawBypassAtlas(texture, dstRect, texture.Bounds, color, rotation, Vect2.One, Vect2.Zero, TextureEffects.None, depth);

    /// <summary>Draws the full texture with scale and rotation without atlas packing.</summary>
    public void DrawBypassAtlas(Texture texture, Vect2 position, Color color, Vect2 scale, float rotation, float depth = 0f)
        => EngineDrawBypassAtlas(texture, new Rect2(position.X, position.Y, texture.Size.X, texture.Size.Y), texture.Bounds, color, rotation, scale,
            Vect2.Zero, TextureEffects.None, depth);

    #endregion

    #region DrawText Methods

    /// <summary>Draws text from the supplied position using top-left alignment.</summary>
    public void DrawText(Font font, string text, Vect2 position, Color color)
        => DrawTextPosition(font, text, position, color, 0f, Vect2.One, TextAlignment.TopLeft);

    /// <summary>Draws scaled text from the supplied position using top-left alignment.</summary>
    public void DrawText(Font font, string text, Vect2 position, Color color, Vect2 scale)
        => DrawTextPosition(font, text, position, color, 0f, scale, TextAlignment.TopLeft);

    /// <summary>Draws text positioned according to the requested alignment.</summary>
    public void DrawText(Font font, string text, Vect2 position, Color color, TextAlignment alignment)
        => DrawTextPosition(font, text, position, color, 0f, Vect2.One, alignment);

    /// <summary>Draws scaled text positioned according to the requested alignment.</summary>
    public void DrawText(Font font, string text, Vect2 position, Color color, Vect2 scale, TextAlignment alignment)
        => DrawTextPosition(font, text, position, color, 0f, scale, alignment);

    /// <summary>Draws text from the supplied position at the requested depth.</summary>
    public void DrawText(Font font, string text, Vect2 position, Color color, float depth)
        => DrawTextPosition(font, text, position, color, depth, Vect2.One, TextAlignment.TopLeft);

    /// <summary>Draws scaled text from the supplied position at the requested depth.</summary>
    public void DrawText(Font font, string text, Vect2 position, Color color, Vect2 scale, float depth)
        => DrawTextPosition(font, text, position, color, depth, scale, TextAlignment.TopLeft);

    /// <summary>Draws aligned text from the supplied position at the requested depth.</summary>
    public void DrawText(Font font, string text, Vect2 position, Color color, TextAlignment alignment, float depth)
        => DrawTextPosition(font, text, position, color, depth, Vect2.One, alignment);

    /// <summary>Draws scaled, aligned text from the supplied position at the requested depth.</summary>
    public void DrawText(Font font, string text, Vect2 position, Color color, Vect2 scale, TextAlignment alignment, float depth)
        => DrawTextPosition(font, text, position, color, depth, scale, alignment);

    /// <summary>Draws unwrapped text inside bounds using top-left alignment.</summary>
    public void DrawText(Font font, string text, Rect2 bounds, Color color)
        => DrawTextBounds(font, text, bounds, color, 0f, Vect2.One, TextAlignment.TopLeft, TextWrapMode.None);

    /// <summary>Draws scaled, unwrapped text inside bounds using top-left alignment.</summary>
    public void DrawText(Font font, string text, Rect2 bounds, Color color, Vect2 scale)
        => DrawTextBounds(font, text, bounds, color, 0f, scale, TextAlignment.TopLeft, TextWrapMode.None);

    /// <summary>Draws unwrapped text inside bounds with the requested alignment.</summary>
    public void DrawText(Font font, string text, Rect2 bounds, Color color, TextAlignment alignment)
        => DrawTextBounds(font, text, bounds, color, 0f, Vect2.One, alignment, TextWrapMode.None);

    /// <summary>Draws text inside bounds using the requested wrap mode.</summary>
    public void DrawText(Font font, string text, Rect2 bounds, Color color, TextWrapMode wrapMode)
        => DrawTextBounds(font, text, bounds, color, 0f, Vect2.One, TextAlignment.TopLeft, wrapMode);

    /// <summary>Draws scaled, aligned, unwrapped text inside bounds.</summary>
    public void DrawText(Font font, string text, Rect2 bounds, Color color, Vect2 scale, TextAlignment alignment)
        => DrawTextBounds(font, text, bounds, color, 0f, scale, alignment, TextWrapMode.None);

    /// <summary>Draws scaled text inside bounds with alignment and wrapping.</summary>
    /// <remarks>
    /// Wrapped lines are measured before glyph submission so horizontal alignment
    /// applies to each generated line and vertical alignment uses the final visible
    /// wrapped block. Once there is no room for another wrapped line, remaining
    /// lines are not submitted.
    /// </remarks>
    public void DrawText(Font font, string text, Rect2 bounds, Color color, Vect2 scale, TextAlignment alignment, TextWrapMode wrapMode)
        => DrawTextBounds(font, text, bounds, color, 0f, scale, alignment, wrapMode);

    /// <summary>Draws unwrapped text inside bounds at the requested depth.</summary>
    public void DrawText(Font font, string text, Rect2 bounds, Color color, float depth)
        => DrawTextBounds(font, text, bounds, color, depth, Vect2.One, TextAlignment.TopLeft, TextWrapMode.None);

    /// <summary>Draws scaled, unwrapped text inside bounds at the requested depth.</summary>
    public void DrawText(Font font, string text, Rect2 bounds, Color color, Vect2 scale, float depth)
        => DrawTextBounds(font, text, bounds, color, depth, scale, TextAlignment.TopLeft, TextWrapMode.None);

    /// <summary>Draws scaled text inside bounds with alignment, wrapping, and depth.</summary>
    public void DrawText(Font font, string text, Rect2 bounds, Color color, Vect2 scale, TextAlignment alignment, TextWrapMode wrapMode, float depth)
        => DrawTextBounds(font, text, bounds, color, depth, scale, alignment, wrapMode);

    #endregion

    #region Ninepatch

    /// <summary>Draws a nine-patch using the supplied source rectangle and border sizes.</summary>
    /// <param name="texture">Nine-patch texture.</param>
    /// <param name="dstRect">Destination rectangle.</param>
    /// <param name="sourceRect">Source rectangle.</param>
    /// <param name="corners">Border sizes stored as left, top, right, and bottom.</param>
    /// <param name="color">Color modulation.</param>
    /// <param name="depth">Depth value used by sorted modes.</param>
    public void DrawNinePatch(Texture texture, Rect2 dstRect, Rect2 sourceRect, Rect2 corners, Color color, float depth = 0f)
        => EngineDrawNinePatch(texture, dstRect, sourceRect, corners, color, depth);

    /// <summary>Draws a nine-patch using the texture's full bounds as the source.</summary>
    /// <param name="texture">Nine-patch texture.</param>
    /// <param name="dstRect">Destination rectangle.</param>
    /// <param name="corners">Border sizes stored as left, top, right, and bottom.</param>
    /// <param name="color">Color modulation.</param>
    /// <param name="depth">Depth value used by sorted modes.</param>
    public void DrawNinePatch(Texture texture, Rect2 dstRect, Rect2 corners, Color color, float depth = 0f)
        => EngineDrawNinePatch(texture, dstRect, texture.Bounds, corners, color, depth);

    /// <summary>Draws a positioned and sized nine-patch from the supplied source rectangle.</summary>
    /// <param name="texture">Nine-patch texture.</param>
    /// <param name="position">Destination position.</param>
    /// <param name="size">Destination size.</param>
    /// <param name="srcRect">Source rectangle.</param>
    /// <param name="corners">Border sizes stored as left, top, right, and bottom.</param>
    /// <param name="color">Color modulation.</param>
    /// <param name="depth">Depth value used by sorted modes.</param>
    public void DrawNinePatch(Texture texture, Vect2 position, Vect2 size, Rect2 srcRect, Rect2 corners, Color color, float depth = 0f)
        => EngineDrawNinePatch(texture, new Rect2(position, size), srcRect, corners, color, depth);

    /// <summary>Draws a positioned and sized nine-patch using the full texture bounds.</summary>
    /// <param name="texture">Nine-patch texture.</param>
    /// <param name="position">Destination position.</param>
    /// <param name="size">Destination size.</param>
    /// <param name="corners">Border sizes stored as left, top, right, and bottom.</param>
    /// <param name="color">Color modulation.</param>
    /// <param name="depth">Depth value used by sorted modes.</param>
    public void DrawNinePatch(Texture texture, Vect2 position, Vect2 size, Rect2 corners, Color color, float depth = 0f)
        => EngineDrawNinePatch(texture, new Rect2(position, size), texture.Bounds, corners, color, depth);
    #endregion

    #region Private Methods

    private void DrawTextPosition(Font font, string text, Vect2 position, Color color, float depth, Vect2 scale, TextAlignment alignment)
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(SpriteBatcher));
        if (!_isDrawing) throw new InvalidOperationException("Cannot draw outside Begin/End");
        if (string.IsNullOrEmpty(text) || font == null) return;

        if (!font.IsValid)
            font.Load();

        var textSize = font.Measure(text) * scale;

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
        if (_isDisposed) throw new ObjectDisposedException(nameof(SpriteBatcher));
        if (!_isDrawing) throw new InvalidOperationException("Cannot draw outside Begin/End");
        if (string.IsNullOrEmpty(text) || font == null) return;
        if (!IsVisible(bounds)) return;

        text = RemoveCarriageReturns(text);
        if (text.Length == 0) return;

        if (!font.IsValid)
            font.Load();

        float lineHeight = (font.LineHeight + font.LineSpacing) * scale.Y;

        if (wrapMode == TextWrapMode.None)
        {
            string[] lines = text.Split('\n');
            float totalHeight = lines.Length * lineHeight;
            float currentY = GetAlignedStartY(bounds, alignment, totalHeight);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (!string.IsNullOrEmpty(line))
                    ProcessLine(line, font, bounds, color, depth, scale, alignment, currentY);

                currentY += lineHeight;
            }

            return;
        }

        List<TextLineLayout> layout = BuildWrappedTextLayout(text, font, bounds.Width, scale, wrapMode);
        int visibleLineCount = GetVisibleWrappedLineCount(layout.Count, bounds.Height, lineHeight);
        if (visibleLineCount == 0)
            return;

        float wrappedHeight = visibleLineCount * lineHeight;
        float startY = GetAlignedStartY(bounds, alignment, wrappedHeight);

        for (int i = 0; i < visibleLineCount; i++)
        {
            float y = startY + i * lineHeight;
            TextLineLayout line = layout[i];

            ProcessLayoutLine(line, font, bounds, color, depth, scale, alignment, y);
        }
    }

    private static float GetAlignedStartY(Rect2 bounds, TextAlignment alignment, float totalHeight)
    {
        if (alignment is TextAlignment.CenterLeft or TextAlignment.Center or TextAlignment.CenterRight)
            return bounds.Y + (bounds.Height - totalHeight) / 2f;

        if (alignment is TextAlignment.BottomLeft or TextAlignment.BottomCenter or TextAlignment.BottomRight)
            return bounds.Y + bounds.Height - totalHeight;

        return bounds.Y;
    }

    private static int GetVisibleWrappedLineCount(int lineCount, float boundsHeight, float lineHeight)
    {
        if (lineCount <= 0 || boundsHeight <= 0f)
            return 0;

        if (lineHeight <= 0f)
            return lineCount;

        int maxLines = Math.Max(1, (int)MathF.Floor(boundsHeight / lineHeight));
        return Math.Min(lineCount, maxLines);
    }

    private static string RemoveCarriageReturns(string text)
        => text.IndexOf('\r') < 0 ? text : text.Replace("\r", string.Empty);

    private static List<TextLineLayout> BuildWrappedTextLayout(
        string text,
        Font font,
        float maxWidth,
        Vect2 scale,
        TextWrapMode wrapMode)
    {
        string[] sourceLines = text.Split('\n');
        var layout = new List<TextLineLayout>(sourceLines.Length);

        for (int i = 0; i < sourceLines.Length; i++)
        {
            if (wrapMode == TextWrapMode.Word)
                AddWordWrappedLayoutLines(layout, sourceLines[i], font, maxWidth, scale.X);
            else
                AddCharWrappedLayoutLines(layout, sourceLines[i], font, maxWidth, scale.X);
        }

        return layout;
    }

    private static void AddWordWrappedLayoutLines(
        List<TextLineLayout> layout,
        string line,
        Font font,
        float maxWidth,
        float scaleX)
    {
        string[] words = line.Split(' ');
        float spaceWidth = GetTextAdvance(font, ' ', scaleX);
        var currentLine = new System.Text.StringBuilder();
        float cursorWidth = 0f;
        float visibleWidth = 0f;

        for (int i = 0; i < words.Length; i++)
        {
            string word = words[i];
            if (string.IsNullOrEmpty(word))
                continue;

            float wordWidth = MeasureLineWidth(word, font, scaleX);

            if (currentLine.Length > 0 && cursorWidth + wordWidth > maxWidth)
            {
                layout.Add(new TextLineLayout(currentLine.ToString(), visibleWidth));
                currentLine.Clear();
                cursorWidth = 0f;
                visibleWidth = 0f;
            }

            if (currentLine.Length > 0)
                currentLine.Append(' ');

            currentLine.Append(word);
            visibleWidth = cursorWidth + wordWidth;
            cursorWidth = visibleWidth + spaceWidth;
        }

        if (currentLine.Length > 0 || string.IsNullOrWhiteSpace(line))
            layout.Add(new TextLineLayout(currentLine.ToString(), visibleWidth));
    }

    private static void AddCharWrappedLayoutLines(
        List<TextLineLayout> layout,
        string line,
        Font font,
        float maxWidth,
        float scaleX)
    {
        if (line.Length == 0)
        {
            layout.Add(new TextLineLayout(string.Empty, 0f));
            return;
        }

        int lineStart = 0;
        float lineWidth = 0f;

        for (int i = 0; i < line.Length; i++)
        {
            float characterWidth = GetTextAdvance(font, line[i], scaleX);

            if (lineWidth > 0f && lineWidth + characterWidth > maxWidth)
            {
                layout.Add(new TextLineLayout(
                    line.Substring(lineStart, i - lineStart),
                    lineWidth));

                lineStart = i;
                lineWidth = 0f;
            }

            lineWidth += characterWidth;
        }

        layout.Add(new TextLineLayout(line.Substring(lineStart), lineWidth));
    }

    private void ProcessLine(string line, Font font, Rect2 bounds, Color color, float depth, Vect2 scale, TextAlignment alignment, float y)
    {
        float lineWidth = MeasureLineWidth(line, font, scale.X);
        ProcessLayoutLine(new TextLineLayout(line, lineWidth), font, bounds, color, depth, scale, alignment, y);
    }

    private void ProcessLayoutLine(
        TextLineLayout line,
        Font font,
        Rect2 bounds,
        Color color,
        float depth,
        Vect2 scale,
        TextAlignment alignment,
        float y)
    {
        float currentX = GetAlignedStartX(bounds, alignment, line.Width);

        for (int i = 0; i < line.Text.Length; i++)
        {
            char character = line.Text[i];
            if (character == '\r')
                continue;

            float advance = GetTextAdvance(font, character, scale.X);

            if (character is ' ' or '\t')
            {
                currentX += advance;
                continue;
            }

            Glyph glyph = font.GetGlyph(character);
            if (!glyph.IsEmpty)
            {
                Rect2 dstRect = new(
                    currentX + glyph.Offset.X * scale.X,
                    y + glyph.Offset.Y * scale.Y,
                    glyph.Size.X * scale.X,
                    glyph.Size.Y * scale.Y);

                EngineDraw(
                    font,
                    dstRect,
                    new Rect2(glyph.Position.X, glyph.Position.Y, glyph.Size.X, glyph.Size.Y),
                    color,
                    0f,
                    Vect2.One,
                    Vect2.Zero,
                    TextureEffects.None,
                    depth,
                    font.Type == AssetType.Normal);
            }

            currentX += advance;
        }
    }

    private static float MeasureLineWidth(string line, Font font, float scaleX)
    {
        float width = 0f;

        for (int i = 0; i < line.Length; i++)
            width += GetTextAdvance(font, line[i], scaleX);

        return width;
    }

    private static float GetTextAdvance(Font font, char character, float scaleX)
    {
        if (character == '\r')
            return 0f;

        if (character == '\t')
            return font.GetGlyph(' ').Advance * Font.TabSpaces * scaleX;

        return font.GetGlyph(character).Advance * scaleX;
    }

    private static float GetAlignedStartX(Rect2 bounds, TextAlignment alignment, float lineWidth)
    {
        if (alignment is TextAlignment.TopCenter or TextAlignment.Center or TextAlignment.BottomCenter)
            return bounds.X + (bounds.Width - lineWidth) / 2f;

        if (alignment is TextAlignment.TopRight or TextAlignment.CenterRight or TextAlignment.BottomRight)
            return bounds.X + bounds.Width - lineWidth;

        return bounds.X;
    }

    private bool IsVisible(Rect2 dstRect)
        => !_hasBatchViewBounds || dstRect.Intersects(_batchViewBounds);

    private static Rect2 GetTransformedBounds(Rect2 dstRect, Vect2 scale, Vect2 origin, float rotation)
    {
        float width = dstRect.Width * scale.X;
        float height = dstRect.Height * scale.Y;
        float left = dstRect.X - origin.X * scale.X;
        float top = dstRect.Y - origin.Y * scale.Y;
        float right = left + width;
        float bottom = top + height;

        if (rotation == 0f)
        {
            float minX = MathF.Min(left, right);
            float minY = MathF.Min(top, bottom);
            float maxX = MathF.Max(left, right);
            float maxY = MathF.Max(top, bottom);
            return new Rect2(minX, minY, maxX - minX, maxY - minY);
        }

        Span<Vect2> corners = stackalloc Vect2[4]
        {
            new(left, top),
            new(right, top),
            new(left, bottom),
            new(right, bottom)
        };

        float cos = MathF.Cos(rotation);
        float sin = MathF.Sin(rotation);
        float centerX = dstRect.X;
        float centerY = dstRect.Y;

        float boundsLeft = float.PositiveInfinity;
        float boundsTop = float.PositiveInfinity;
        float boundsRight = float.NegativeInfinity;
        float boundsBottom = float.NegativeInfinity;

        for (int i = 0; i < corners.Length; i++)
        {
            float dx = corners[i].X - centerX;
            float dy = corners[i].Y - centerY;
            float x = centerX + dx * cos - dy * sin;
            float y = centerY + dx * sin + dy * cos;

            boundsLeft = MathF.Min(boundsLeft, x);
            boundsTop = MathF.Min(boundsTop, y);
            boundsRight = MathF.Max(boundsRight, x);
            boundsBottom = MathF.Max(boundsBottom, y);
        }

        return new Rect2(
            boundsLeft,
            boundsTop,
            boundsRight - boundsLeft,
            boundsBottom - boundsTop);
    }

    private sealed class DrawCommandComparer : IComparer<DrawCommand>
    {
        private SortMode _sortMode;

        public DrawCommandComparer(SortMode sortMode) => _sortMode = sortMode;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare(DrawCommand a, DrawCommand b)
        {
            bool aValue = IsTextureSourceValid(a);
            bool bValue = IsTextureSourceValid(b);

            if (!aValue && !bValue) return 0;
            if (!aValue) return -1;
            if (!bValue) return 1;

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

            uint texA = GetTextureKey(a);
            uint texB = GetTextureKey(b);
            if (texA < texB) return -1;
            if (texA > texB) return 1;

            return 0;
        }

        public void UpdateMode(SortMode sortMode) => _sortMode = sortMode;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EngineDrawNinePatch(Texture texture, Rect2 dstRect, Rect2 sourceRect, Rect2 corners, Color color, float depth)
    {
        var dstRects = CalculateNinePatchRects(dstRect, corners);
        var srcRects = GetNinePatchSourceRects(sourceRect, corners);

        for (int i = 0; i < 9; i++)
        {
            EngineDrawBypassAtlas(texture, dstRects[i], srcRects[i], color, 0f, Vect2.One, Vect2.Zero, TextureEffects.None, depth);
        }
    }

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
    private void EngineDraw(Font font, Rect2 dstRect, Rect2 srcRect, Color color, float rotation, Vect2 scale,
        Vect2 origin, TextureEffects effects, float depth, bool canPack)
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(SpriteBatcher));
        if (!_isDrawing) throw new InvalidOperationException("Cannot draw outside Begin/End");
        if (font == null) return;

        if (!font.IsValid)
            font.Load();

        Rect2 visibleRect = GetTransformedBounds(dstRect, scale, origin, rotation);

        if (!IsVisible(visibleRect)) return;
        if (_cmdCount >= _cmds.Length) ResizeBuffers();

        if (canPack && AtlasManager.Instance.TryPack(font, srcRect, out var packedRect, out var pageId))
        {
            _cmds[_cmdCount] = new DrawCommand
            {
                Texture = AtlasManager.Instance.GetPageTexture(pageId),
                Font = null,
                Depth = depth,
                DstRect = dstRect,
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
                Texture = null,
                Font = font,
                Depth = depth,
                DstRect = dstRect,
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
    private static uint GetTextureKey(in DrawCommand command)
        => command.Texture?.Id ?? command.Font?.Id ?? 0u;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsTextureSourceValid(in DrawCommand command)
        => command.Texture?.IsValid ?? command.Font?.IsValid ?? false;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EngineDraw(Texture texture, Rect2 dstRect, Rect2 srcRect, Color color, float rotation, Vect2 scale,
        Vect2 origin, TextureEffects effects, float depth, bool canPack)
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(SpriteBatcher));
        if (!_isDrawing) throw new InvalidOperationException("Cannot draw outside Begin/End");

        Rect2 visibleRect = GetTransformedBounds(dstRect, scale, origin, rotation);

        if (!IsVisible(visibleRect)) return;
        if (_cmdCount >= _cmds.Length) ResizeBuffers();

        if (canPack && AtlasManager.Instance.TryPack(texture, srcRect, out var packedRect, out var pageId))
        {
            _cmds[_cmdCount] = new DrawCommand
            {
                Texture = AtlasManager.Instance.GetPageTexture(pageId),
                Font = null,
                Depth = depth,
                DstRect = dstRect,
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
                Font = null,
                Depth = depth,
                DstRect = dstRect,
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

        Rect2 visibleRect = GetTransformedBounds(dstRect, scale, origin, rotation);

        if (!IsVisible(visibleRect)) return;
        if (_cmdCount >= _cmds.Length) ResizeBuffers();

        _cmds[_cmdCount] = new DrawCommand
        {
            Texture = texture,
            Font = null,
            Depth = depth,
            DstRect = dstRect,
            SrcRect = srcRect,
            Color = color,
            Rotation = rotation,
            Scale = scale,
            Origin = origin,
            Effects = effects
        };

        _cmdCount++;
    }

    private unsafe void WriteQuadUnsafe(RenderVertex* ptr, in DrawCommand cmd)
    {
        Vect2* corners = stackalloc Vect2[4];
        float width = cmd.DstRect.Width * cmd.Scale.X;
        float height = cmd.DstRect.Height * cmd.Scale.Y;

        float left = MathF.Round(cmd.DstRect.X - cmd.Origin.X * cmd.Scale.X, 3);
        float top = MathF.Round(cmd.DstRect.Y - cmd.Origin.Y * cmd.Scale.Y, 3);
        float right = MathF.Round(left + width, 3);
        float bottom = MathF.Round(top + height, 3);

        corners[0] = new Vect2(left, top);
        corners[1] = new Vect2(right, top);
        corners[2] = new Vect2(left, bottom);
        corners[3] = new Vect2(right, bottom);

        if (cmd.Rotation != 0f)
        {
            float cos = MathF.Cos(cmd.Rotation);
            float sin = MathF.Sin(cmd.Rotation);

            float centerX = cmd.DstRect.X;
            float centerY = cmd.DstRect.Y;

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

        if (GameSettings.Instance.UseHalfTexelOffset && (cmd.Texture != null || cmd.Font != null))
        {
            // VOID stores 2D texture coordinates in pixel space. The default shader
            // normalizes them by texture size, so a half-texel adjustment is exactly
            // half a source pixel here rather than half a normalized texel.
            const float HalfTexel = 0.5f;
            srcLeft += HalfTexel;
            srcRight -= HalfTexel;
            srcTop += HalfTexel;
            srcBottom -= HalfTexel;
        }

        if (cmd.Effects.HasFlag(TextureEffects.Horizontal))
            (srcLeft, srcRight) = (srcRight, srcLeft);
        if (cmd.Effects.HasFlag(TextureEffects.Vertical))
            (srcTop, srcBottom) = (srcBottom, srcTop);

        var color = cmd.Color;

        ptr[0] = new RenderVertex(corners[0], color, new(srcLeft, srcTop));
        ptr[1] = new RenderVertex(corners[1], color, new(srcRight, srcTop));
        ptr[2] = new RenderVertex(corners[2], color, new(srcLeft, srcBottom));
        ptr[3] = new RenderVertex(corners[3], color, new(srcRight, srcBottom));
    }

    #endregion

    #region Dispose

    /// <summary>Clears queued command references and disposes the quad index buffer.</summary>
    protected override void OnDispose()
    {
        if (_isDisposed) return;

        if (_cmds != null)
            Array.Clear(_cmds, 0, _cmds.Length);

        _indexBuffer?.Dispose();
        _indexBuffer = null;

        base.OnDispose();
    }
    #endregion
}
