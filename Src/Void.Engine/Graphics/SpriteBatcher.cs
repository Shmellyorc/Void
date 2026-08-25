// ============================================================================
//  SpriteBatcher.cs
// ============================================================================
//  Batch rendering for sprites with support for textures, transformations,
//  color modulation, texture effects (flip), depth sorting, and atlas packing.
//  Also provides text rendering with alignment and wrapping support.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Graphics;

/// <summary>
/// Specifies text alignment relative to a position or bounding box.
/// </summary>
public enum TextAlignment
{
    /// <summary>Text is aligned to the top-left.</summary>
    TopLeft,
    /// <summary>Text is centered horizontally at the top.</summary>
    TopCenter,
    /// <summary>Text is aligned to the top-right.</summary>
    TopRight,
    /// <summary>Text is vertically centered on the left.</summary>
    CenterLeft,
    /// <summary>Text is centered both horizontally and vertically.</summary>
    Center,
    /// <summary>Text is vertically centered on the right.</summary>
    CenterRight,
    /// <summary>Text is aligned to the bottom-left.</summary>
    BottomLeft,
    /// <summary>Text is centered horizontally at the bottom.</summary>
    BottomCenter,
    /// <summary>Text is aligned to the bottom-right.</summary>
    BottomRight
}

/// <summary>
/// Specifies text wrapping behavior when text exceeds the bounds.
/// </summary>
public enum TextWrapMode
{
    /// <summary>No wrapping. Text may extend beyond the bounds.</summary>
    None,
    /// <summary>Wraps at word boundaries (spaces).</summary>
    Word,
    /// <summary>Wraps at character boundaries (any character).</summary>
    Character
}

/// <summary>
/// Batch rendering for sprites with support for textures, transformations,
/// color modulation, texture effects (flip), depth sorting, and atlas packing.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="SpriteBatcher"/> class provides efficient batch rendering
/// for sprites and text. It supports:
/// <list type="bullet">
///   <item><description>Sprite rendering with textures, source rectangles, and color modulation</description></item>
///   <item><description>Transformations (position, rotation, scale, origin)</description></item>
///   <item><description>Horizontal and vertical flipping (TextureEffects)</description></item>
///   <item><description>Depth sorting (BackToFront, FrontToBack)</description></item>
///   <item><description>Automatic texture atlasing via <see cref="AtlasManager"/></description></item>
///   <item><description>Text rendering with alignment and wrapping</description></item>
///   <item><description>Nine-patch scaling for UI elements</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// var batcher = new SpriteBatcher();
/// batcher.Begin(SortMode.BackToFront);
/// 
/// // Draw a sprite
/// batcher.Draw(texture, new Vect2(100, 100), Color.White);
/// 
/// // Draw a sprite with rotation and scale
/// batcher.Draw(texture, new Rect2(200, 200, 64, 64), Color.White, 0.5f, new Vect2(2, 2), new Vect2(32, 32), TextureEffects.None, 0.5f);
/// 
/// // Draw text
/// batcher.DrawText(font, "Hello World!", new Vect2(300, 300), Color.Black, TextAlignment.Center);
/// 
/// // Draw a nine-patch UI element
/// batcher.DrawNinePatch(uiTexture, new Rect2(400, 400, 200, 100), new Rect2(0, 0, 64, 64), new Rect2(8, 8, 8, 8), Color.White);
/// 
/// batcher.End();
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe and should be accessed from the main thread.
/// </para>
/// </remarks>
public sealed class SpriteBatcher : BaseBatcher
{
    private struct DrawCommand
    {
        public SFTexture Texture;
        public float Depth;
        public Rect2 DstRect;
        public Rect2 SrcRect;
        public Color Color;
        public float Rotation;
        public Vect2 Scale;
        public Vect2 Origin;
        public TextureEffects Effects;
    }

    private const int VerticesPerQuad = 6;
    private const int InitialCapacity = 1024;

    private DrawCommand[] _cmds;
    private readonly DrawCommandComparer _comparer;

    /// <summary>
    /// Gets the name of the batcher.
    /// </summary>
    public override string Name => "SpriteBatcher";

    /// <summary>
    /// Gets the number of vertices per command (6 for a quad).
    /// </summary>
    protected override int VerticesPerCommand => VerticesPerQuad;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpriteBatcher"/> class.
    /// </summary>
    /// <param name="capacity">The initial capacity of the batch.</param>
    public SpriteBatcher(int capacity = 0) : base(capacity)
    {
        _capacity = capacity > 0 ? capacity : GetDefaultCapacity();
        _cmds = new DrawCommand[_capacity];
        _comparer = new DrawCommandComparer(_sortMode);
    }

    /// <summary>
    /// Gets the default capacity for the sprite batch.
    /// </summary>
    protected override int GetDefaultCapacity() => GameSettings.Instance.SpriteBatchCapacity;

    /// <summary>
    /// Called when batching begins.
    /// </summary>
    protected override void OnBegin()
    {
        _currentTexture = null;

        AtlasManager.Instance.ProcessPendingDefragMoves(
            GameSettings.Instance.AtlasDefragMovesPerFrame);

        base.OnBegin();
    }


    /// <summary>
    /// Called when batching ends.
    /// </summary>
    protected override void OnEnd() { }

    /// <summary>
    /// Called when the batch is flushed.
    /// </summary>
    protected override void OnFlush() { }

    /// <summary>
    /// Sorts the commands for optimal rendering.
    /// </summary>
    protected override void SortCommands()
    {
        _comparer.UpdateMode(_sortMode);
        Array.Sort(_cmds, 0, _cmdCount, _comparer);
    }

    /// <summary>
    /// Builds the vertices for all commands.
    /// </summary>
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

    /// <summary>
    /// Determines whether two commands can be batched together.
    /// </summary>
    protected override bool CanBatchTogether(int indexA, int indexB)
        => _cmds[indexA].Texture.NativeHandle == _cmds[indexB].Texture.NativeHandle;

    /// <summary>
    /// Sets the render state for a group of commands.
    /// </summary>
    protected override void SetRenderStateForGroup(int commandIndex)
    {
        base.SetRenderStateForGroup(commandIndex);

        var texture = _cmds[commandIndex].Texture;
        _renderStates.Texture = texture;

        if (_currentShader is Shader shaderAsset)
            shaderAsset.SetUniform("uTexture", texture);
    }

    /// <summary>
    /// Resizes the command and vertex buffers.
    /// </summary>
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

    // Add this method to SpriteBatcher
    /// <summary>
    /// Draws an atlas page for debugging purposes. Bypasses the atlas system.
    /// </summary>
    /// <param name="pageId">The atlas page index.</param>
    /// <param name="dstRect">The destination rectangle on screen.</param>
    /// <param name="depth">The depth for sorting.</param>
    public void DrawAtlasDebugPage(int pageId, Rect2 dstRect, float depth = 0.999f)
    {
        var pageTexture = AtlasManager.Instance.GetPageTexture(pageId);
        if (pageTexture == null)
            return;

        EngineDrawSFMLBypassAtlas(pageTexture, dstRect, new Rect2(Vect2.Zero, pageTexture.Size), Color.White, depth);
    }
    private void EngineDrawSFMLBypassAtlas(SFTexture texture, Rect2 dstRect, Rect2 srcRect, Color color, float depth = 0.999f)
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(SpriteBatcher));
        if (!_isDrawing) throw new InvalidOperationException("Cannot draw outside Begin/End");
        if (!IsVisible(dstRect)) return;
        if (_cmdCount >= _cmds.Length) ResizeBuffers();

        _cmds[_cmdCount] = new DrawCommand
        {
            Texture = texture,
            Depth = depth,
            DstRect = dstRect,
            SrcRect = srcRect,
            Color = color,
            Rotation = 0f,
            Scale = Vect2.One,
            Origin = Vect2.Zero,
            Effects = TextureEffects.None
        };

        _cmdCount++;
    }

    /// <summary>
    /// Draws a sprite with the specified texture, destination, source rectangle, and color.
    /// </summary>
    public void Draw(Texture texture, Rect2 dstRect, Rect2 srcRect, Color color, float depth = 0f)
        => EngineDraw(texture, dstRect, srcRect, color, 0f, Vect2.One, Vect2.Zero, TextureEffects.None, depth, texture.Type == AssetType.Normal);

    /// <summary>
    /// Draws a sprite with the specified texture, rectangle, and color.
    /// </summary>
    public void Draw(Texture texture, Rect2 rect, Color color, float depth = 0f)
        => EngineDraw(texture, rect, texture.Bounds, color, 0f, Vect2.One, Vect2.Zero, TextureEffects.None, depth, texture.Type == AssetType.Normal);

    /// <summary>
    /// Draws a sprite with the specified texture, position, source rectangle, and color.
    /// </summary>
    public void Draw(Texture texture, Vect2 position, Rect2 srcRect, Color color, float depth = 0f)
        => EngineDraw(texture, new(position, srcRect.Size), srcRect, color, 0f, Vect2.One, Vect2.Zero, TextureEffects.None, depth, texture.Type == AssetType.Normal);

    /// <summary>
    /// Draws a sprite with the specified texture, destination, source rectangle, color, and transformations.
    /// </summary>
    public void Draw(Texture texture, Rect2 dstRect, Rect2 srcRect, Color color, float rotation, Vect2 scale, Vect2 origin, TextureEffects effects, float depth)
        => EngineDraw(texture, dstRect, srcRect, color, rotation, scale, origin, effects, depth, texture.Type == AssetType.Normal);

    /// <summary>
    /// Draws a sprite with the specified texture, rectangle, color, and transformations.
    /// </summary>
    public void Draw(Texture texture, Rect2 rect, Color color, float rotation, Vect2 scale, Vect2 origin, TextureEffects effects, float depth)
        => EngineDraw(texture, rect, texture.Bounds, color, rotation, scale, origin, effects, depth, texture.Type == AssetType.Normal);

    /// <summary>
    /// Draws a sprite with the specified texture, position, source rectangle, color, and transformations.
    /// </summary>
    public void Draw(Texture texture, Vect2 position, Rect2 srcRect, Color color, float rotation, Vect2 scale, Vect2 origin, TextureEffects effects, float depth)
        => EngineDraw(texture, new(position, srcRect.Size), srcRect, color, rotation, scale, origin, effects, depth, texture.Type == AssetType.Normal);

    /// <summary>
    /// Draws a sprite with the specified texture, position, and color.
    /// </summary>
    public void Draw(Texture texture, Vect2 position, Color color, float depth = 0f)
        => EngineDraw(texture, new Rect2(position.X, position.Y, texture.Size.X, texture.Size.Y), texture.Bounds, color, 0f, Vect2.One, Vect2.Zero, TextureEffects.None, depth, texture.Type == AssetType.Normal);

    /// <summary>
    /// Draws a sprite with the specified texture, position, source rectangle, color, and rotation.
    /// </summary>
    public void Draw(Texture texture, Vect2 position, Rect2 srcRect, Color color, float rotation, float depth = 0f)
        => EngineDraw(texture, new Rect2(position.X, position.Y, srcRect.Width, srcRect.Height), srcRect, color, rotation, Vect2.One, Vect2.Zero, TextureEffects.None, depth, texture.Type == AssetType.Normal);

    /// <summary>
    /// Draws a sprite with the specified texture, destination rectangle, color, and rotation.
    /// </summary>
    public void Draw(Texture texture, Rect2 dstRect, Color color, float rotation, float depth = 0f)
        => EngineDraw(texture, dstRect, texture.Bounds, color, rotation, Vect2.One, Vect2.Zero, TextureEffects.None, depth, texture.Type == AssetType.Normal);

    /// <summary>
    /// Draws a sprite with the specified texture, position, color, rotation, and scale.
    /// </summary>
    public void Draw(Texture texture, Vect2 position, Color color, float rotation, Vect2 scale, float depth = 0f)
        => EngineDraw(texture, new Rect2(position.X, position.Y, texture.Size.X * scale.X, texture.Size.Y * scale.Y), texture.Bounds, color, rotation, scale, Vect2.Zero, TextureEffects.None, depth, texture.Type == AssetType.Normal);

    #endregion

    #region DrawBypassAtlas Methods

    /// <summary>
    /// Draws a sprite bypassing the atlas system.
    /// </summary>
    public void DrawBypassAtlas(Texture texture, Rect2 dstRect, Rect2 srcRect, Color color, float depth = 0f)
        => EngineDrawBypassAtlas(texture, dstRect, srcRect, color, 0f, Vect2.One, Vect2.Zero, TextureEffects.None, depth);

    /// <summary>
    /// Draws a sprite bypassing the atlas system.
    /// </summary>
    public void DrawBypassAtlas(Texture texture, Rect2 rect, Color color, float depth = 0f)
        => EngineDrawBypassAtlas(texture, rect, texture.Bounds, color, 0f, Vect2.One, Vect2.Zero, TextureEffects.None, depth);

    /// <summary>
    /// Draws a sprite bypassing the atlas system.
    /// </summary>
    public void DrawBypassAtlas(Texture texture, Vect2 position, Rect2 srcRect, Color color, float depth = 0f)
        => EngineDrawBypassAtlas(texture, new(position, srcRect.Size), srcRect, color, 0f, Vect2.One, Vect2.Zero, TextureEffects.None, depth);

    /// <summary>
    /// Draws a sprite bypassing the atlas system with transformations.
    /// </summary>
    public void DrawBypassAtlas(Texture texture, Rect2 dstRect, Rect2 srcRect, Color color, float rotation, Vect2 scale, Vect2 origin, TextureEffects effects, float depth)
        => EngineDrawBypassAtlas(texture, dstRect, srcRect, color, rotation, scale, origin, effects, depth);

    /// <summary>
    /// Draws a sprite bypassing the atlas system with transformations.
    /// </summary>
    public void DrawBypassAtlas(Texture texture, Rect2 rect, Color color, float rotation, Vect2 scale, Vect2 origin, TextureEffects effects, float depth)
        => EngineDrawBypassAtlas(texture, rect, texture.Bounds, color, rotation, scale, origin, effects, depth);

    /// <summary>
    /// Draws a sprite bypassing the atlas system with transformations.
    /// </summary>
    public void DrawBypassAtlas(Texture texture, Vect2 position, Rect2 srcRect, Color color, float rotation, Vect2 scale, Vect2 origin, TextureEffects effects, float depth)
        => EngineDrawBypassAtlas(texture, new(position, srcRect.Size), srcRect, color, rotation, scale, origin, effects, depth);

    /// <summary>
    /// Draws a sprite bypassing the atlas system.
    /// </summary>
    public void DrawBypassAtlas(Texture texture, Vect2 position, Color color, float depth = 0f)
        => EngineDrawBypassAtlas(texture, new Rect2(position.X, position.Y, texture.Size.X, texture.Size.Y), texture.Bounds, color, 0f, Vect2.One, Vect2.Zero, TextureEffects.None, depth);

    /// <summary>
    /// Draws a sprite bypassing the atlas system with rotation.
    /// </summary>
    public void DrawBypassAtlas(Texture texture, Vect2 position, Rect2 srcRect, Color color, float rotation, float depth = 0f)
        => EngineDrawBypassAtlas(texture, new Rect2(position.X, position.Y, srcRect.Width, srcRect.Height), srcRect, color, rotation, Vect2.One, Vect2.Zero, TextureEffects.None, depth);

    /// <summary>
    /// Draws a sprite bypassing the atlas system with rotation.
    /// </summary>
    public void DrawBypassAtlas(Texture texture, Rect2 dstRect, Color color, float rotation, float depth = 0f)
        => EngineDrawBypassAtlas(texture, dstRect, texture.Bounds, color, rotation, Vect2.One, Vect2.Zero, TextureEffects.None, depth);

    /// <summary>
    /// Draws a sprite bypassing the atlas system with rotation and scale.
    /// </summary>
    public void DrawBypassAtlas(Texture texture, Vect2 position, Color color, float rotation, Vect2 scale, float depth = 0f)
        => EngineDrawBypassAtlas(texture, new Rect2(position.X, position.Y, texture.Size.X * scale.X, texture.Size.Y * scale.Y), texture.Bounds, color, rotation, scale, Vect2.Zero, TextureEffects.None, depth);

    #endregion

    #region DrawText Methods

    /// <summary>
    /// Draws text at the specified position.
    /// </summary>
    public void DrawText(Font font, string text, Vect2 position, Color color)
        => DrawTextPosition(font, text, position, color, 0f, Vect2.One, TextAlignment.TopLeft);

    /// <summary>
    /// Draws text at the specified position with scale.
    /// </summary>
    public void DrawText(Font font, string text, Vect2 position, Color color, Vect2 scale)
        => DrawTextPosition(font, text, position, color, 0f, scale, TextAlignment.TopLeft);

    /// <summary>
    /// Draws text at the specified position with depth.
    /// </summary>
    public void DrawText(Font font, string text, Vect2 position, Color color, float depth)
        => DrawTextPosition(font, text, position, color, depth, Vect2.One, TextAlignment.TopLeft);

    /// <summary>
    /// Draws text at the specified position with scale and depth.
    /// </summary>
    public void DrawText(Font font, string text, Vect2 position, Color color, Vect2 scale, float depth)
        => DrawTextPosition(font, text, position, color, depth, scale, TextAlignment.TopLeft);

    /// <summary>
    /// Draws text at the specified position with alignment.
    /// </summary>
    public void DrawText(Font font, string text, Vect2 position, Color color, TextAlignment alignment)
        => DrawTextPosition(font, text, position, color, 0f, Vect2.One, alignment);

    /// <summary>
    /// Draws text at the specified position with alignment and scale.
    /// </summary>
    public void DrawText(Font font, string text, Vect2 position, Color color, TextAlignment alignment, Vect2 scale)
        => DrawTextPosition(font, text, position, color, 0f, scale, alignment);

    /// <summary>
    /// Draws text at the specified position with alignment, scale, and depth.
    /// </summary>
    public void DrawText(Font font, string text, Vect2 position, Color color, TextAlignment alignment, Vect2 scale, float depth)
        => DrawTextPosition(font, text, position, color, depth, scale, alignment);

    /// <summary>
    /// Draws text within the specified bounds.
    /// </summary>
    public void DrawText(Font font, string text, Rect2 bounds, Color color)
        => DrawTextBounds(font, text, bounds, color, 0f, Vect2.One, TextAlignment.TopLeft, TextWrapMode.None);

    /// <summary>
    /// Draws text within the specified bounds with scale.
    /// </summary>
    public void DrawText(Font font, string text, Rect2 bounds, Color color, Vect2 scale)
        => DrawTextBounds(font, text, bounds, color, 0f, scale, TextAlignment.TopLeft, TextWrapMode.None);

    /// <summary>
    /// Draws text within the specified bounds with depth.
    /// </summary>
    public void DrawText(Font font, string text, Rect2 bounds, Color color, float depth)
        => DrawTextBounds(font, text, bounds, color, depth, Vect2.One, TextAlignment.TopLeft, TextWrapMode.None);

    /// <summary>
    /// Draws text within the specified bounds with scale, depth, alignment, and wrapping.
    /// </summary>
    public void DrawText(Font font, string text, Rect2 bounds, Color color, Vect2 scale, float depth,
        TextAlignment alignment = TextAlignment.TopLeft, TextWrapMode wrapMode = TextWrapMode.None)
        => DrawTextBounds(font, text, bounds, color, depth, scale, alignment, wrapMode);

    #endregion

    /// <summary>
    /// Draws a nine-patch sprite (scalable UI element).
    /// </summary>
    /// <param name="texture">The texture containing the nine-patch.</param>
    /// <param name="dstRect">The destination rectangle.</param>
    /// <param name="sourceRect">The source rectangle in the texture.</param>
    /// <param name="corners">The corner sizes (left, top, right, bottom).</param>
    /// <param name="color">The color modulation.</param>
    /// <param name="depth">The depth for sorting.</param>
    /// <remarks>
    /// <para>
    /// A nine-patch divides the source texture into 9 regions:
    /// <list type="bullet">
    ///   <item><description>4 corners (fixed size)</description></item>
    ///   <item><description>4 edges (stretch or repeat)</description></item>
    ///   <item><description>1 center (stretch or repeat)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This is commonly used for UI elements like buttons, panels, and windows
    /// that need to scale to different sizes without distorting the corners.
    /// </para>
    /// </remarks>
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

            float centerX = left + cmd.Origin.X;
            float centerY = top + cmd.Origin.Y;

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

    #endregion

    #region Text Processing

    private void DrawTextPosition(Font font, string text, Vect2 position, Color color, float depth, Vect2 scale, TextAlignment alignment)
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(SpriteBatcher));
        if (!_isDrawing) throw new InvalidOperationException("Cannot draw outside Begin/End");
        if (string.IsNullOrEmpty(text) || font == null) return;

        AssetManager.Instance.Touch(font);

        if(!font.IsValid)
            font.Load();

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
        if (_isDisposed) throw new ObjectDisposedException(nameof(SpriteBatcher));
        if (!_isDrawing) throw new InvalidOperationException("Cannot draw outside Begin/End");
        if (string.IsNullOrEmpty(text) || font == null) return;
        if (!IsVisible(bounds)) return;

        AssetManager.Instance.Touch(font);

        if(!font.IsValid)
            font.Load();

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

    /// <summary>
    /// Disposes the batcher and releases all resources.
    /// </summary>
    protected override void OnDispose()
    {
        if (_isDisposed) return;

        if (_cmds != null)
            Array.Clear(_cmds, 0, _cmds.Length);

        base.OnDispose();
    }

    private sealed class DrawCommandComparer : IComparer<DrawCommand>
    {
        private SortMode _sortMode;

        public DrawCommandComparer(SortMode sortMode) => _sortMode = sortMode;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare(DrawCommand a, DrawCommand b)
        {
            bool aValue = a.Texture != null && !a.Texture.IsInvalid;
            bool bValue = b.Texture != null && !b.Texture.IsInvalid;

            if (!aValue && !bValue) return 0;
            if (!bValue) return -1;
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

            uint texA = a.Texture.NativeHandle;
            uint texB = b.Texture.NativeHandle;
            if (texA < texB) return -1;
            if (texA > texB) return 1;

            return 0;
        }

        public void UpdateMode(SortMode sortMode) => _sortMode = sortMode;
    }
}