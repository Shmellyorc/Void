using RenderVertex = Void.Engine.Graphics.Rendering.Vertex;

// ============================================================================
//  BaseBatcher.cs
// ============================================================================
//  Shared batch lifecycle, render state, geometry upload, and statistics for
//  VOID batcher implementations.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Graphics;

/// <summary>
/// Provides the shared lifecycle and submission pipeline for batched rendering.
/// </summary>
/// <remarks>
/// <para>
/// Derive from <see cref="BaseBatcher"/> when a custom batcher can use VOID's
/// renderer-neutral vertex buffer and grouped submission model. Derived classes
/// provide command storage, sorting, vertex generation, and grouping rules.
/// </para>
/// <para>
/// A batch begins with <see cref="Begin"/>, accepts commands through the derived
/// type, and is submitted by <see cref="Flush"/> or <see cref="End"/>. Shader and
/// render-target selection are batcher-wide state at flush time, not per-command state.
/// </para>
/// </remarks>
public abstract class BaseBatcher : IBatcher
{
    private const int InitialCapacity = 1024;
    private const int MaxCapacity = 65536;

    private readonly IRenderTarget _defaultRenderTarget;

    // Built-in batchers retain this backing state for compatibility. External
    // derived batchers should use the protected PascalCase surface below.
    internal bool _isDisposed;
    internal bool _isDrawing;
    internal int _cmdCount;
    internal int _capacity;
    internal SortMode _sortMode;
    internal IBlendMode _blendMode;
    internal BaseCamera _currentCamera;
    internal BatchRenderState _renderStates;
    internal BatchStats _stats;
    internal RenderVertex[] _vertexData;
    internal IShader _currentShader;
    internal int _vertexBufferSize;
    internal IRenderTarget _renderTarget;
    internal IVertexBuffer _vertexBuffer;
    // internal Texture _currentTexture;

    /// <summary>Gets whether this batcher has been disposed.</summary>
    protected bool IsDisposed => _isDisposed;

    /// <summary>Gets whether a batch is currently active.</summary>
    protected bool IsBatchActive => _isDrawing;

    /// <summary>Gets or sets the number of queued commands.</summary>
    protected int QueuedCommandCount
    {
        get => _cmdCount;
        set => _cmdCount = value;
    }

    /// <summary>Gets or sets the command capacity, clamped to the supported range.</summary>
    protected int Capacity
    {
        get => _capacity;
        set => _capacity = Math.Clamp(value, 1, MaxCapacity);
    }

    /// <summary>Gets the sort mode selected for the active batch.</summary>
    protected SortMode CurrentSortMode => _sortMode;

    /// <summary>Gets the blend mode selected for the active batch.</summary>
    protected IBlendMode CurrentBlendMode => _blendMode;

    /// <summary>Gets the camera selected for the active batch, if any.</summary>
    protected BaseCamera CurrentCamera => _currentCamera;

    /// <summary>Gets the renderer-neutral state used for draw submission.</summary>
    protected BatchRenderState RenderStates => _renderStates;

    /// <summary>Gets or sets the statistics reported by the batcher.</summary>
    protected BatchStats Statistics
    {
        get => _stats;
        set => _stats = value;
    }

    /// <summary>Gets or sets the CPU-side vertex array used for uploads.</summary>
    protected RenderVertex[] VertexData
    {
        get => _vertexData;
        set => _vertexData = value;
    }

    /// <summary>Gets the shader currently selected for the batcher.</summary>
    protected IShader CurrentShader => _currentShader;

    /// <summary>Gets or sets the vertex-buffer capacity measured in vertices.</summary>
    protected int VertexBufferSize
    {
        get => _vertexBufferSize;
        set => _vertexBufferSize = value;
    }

    /// <summary>Gets or sets the active render target.</summary>
    protected IRenderTarget RenderTarget
    {
        get => _renderTarget;
        set => _renderTarget = value ?? _defaultRenderTarget;
    }

    /// <summary>Gets or sets the vertex buffer used for submissions.</summary>
    protected IVertexBuffer VertexBuffer
    {
        get => _vertexBuffer;
        set => _vertexBuffer = value;
    }

    /// <summary>Gets the display name of the concrete batcher.</summary>
    public abstract string Name { get; }

    /// <summary>Gets whether <see cref="Begin"/> has been called without a matching <see cref="End"/>.</summary>
    public bool IsDrawing => _isDrawing;

    /// <summary>
    /// Gets statistics for the most recent flush since the current <see cref="Begin"/>.
    /// </summary>
    public BatchStats Stats => _stats;

    /// <summary>Gets the draw-call count reported by <see cref="Stats"/>.</summary>
    public int DrawCallCount => _stats.DrawCalls;

    /// <summary>Gets the vertex count reported by <see cref="Stats"/>.</summary>
    public int VertexCount => _stats.Vertices;

    /// <summary>Gets the number of commands currently queued and not yet flushed.</summary>
    public int CommandCount => _cmdCount;

    /// <summary>Gets the number of vertices reserved for each queued command.</summary>
    protected abstract int VerticesPerCommand { get; }

    /// <summary>
    /// Initializes a batcher with the requested command capacity.
    /// </summary>
    /// <param name="capacity">
    /// Initial command capacity. A value less than or equal to zero uses
    /// <see cref="GetDefaultCapacity"/>.
    /// </param>
    protected BaseBatcher(int capacity = 0)
    {
        _capacity = capacity > 0
            ? Math.Clamp(capacity, 1, MaxCapacity)
            : GetDefaultCapacity();

        _cmdCount = 0;
        _sortMode = SortMode.BackToFront;
        _blendMode = BlendMode.Alpha;
        _stats = new BatchStats();

        int vertexCapacity = checked(_capacity * VerticesPerCommand);

        _vertexBuffer = new VertexBuffer(vertexCapacity);
        _vertexData = new RenderVertex[vertexCapacity];
        _vertexBufferSize = vertexCapacity;

        _defaultRenderTarget = Game.Instance.Window.MainRenderTarget;
        _renderTarget = _defaultRenderTarget;

        _renderStates = new BatchRenderState
        {
            BlendMode = BlendMode.Alpha
        };
    }

    /// <summary>
    /// Selects the render target used by subsequent submissions.
    /// </summary>
    /// <param name="target">The target to select. A null value is ignored.</param>
    public void SetRenderTarget(IRenderTarget target)
    {
        if (target != null)
            _renderTarget = target;
    }

    /// <summary>Restores the game's main render target.</summary>
    public void ResetRenderTarget()
    {
        _renderTarget = _defaultRenderTarget;
    }

    /// <summary>Gets whether the selected target is the game's main render target.</summary>
    public bool IsUsingDefaultRenderTarget
        => ReferenceEquals(_renderTarget, _defaultRenderTarget);

    /// <summary>Gets the currently selected render target.</summary>
    /// <returns>The render target that will receive the next submission.</returns>
    public IRenderTarget GetRenderTarget()
        => _renderTarget;

    private static Matrix CreateDefaultViewProjection()
    {
        Vect2 viewport = GameSettings.Instance.Viewport;

        if (viewport.X <= 0f || viewport.Y <= 0f)
            return Matrix.Identity;

        return Matrix.CreateOrthographicOffCenter(
            0f,
            viewport.X,
            0f,
            viewport.Y);
    }

    /// <summary>
    /// Copies the selected shader into <see cref="RenderStates"/>.
    /// </summary>
    /// <remarks>Derived batchers may override this to prepare additional shader state.</remarks>
    protected virtual void ApplyShader()
    {
        _renderStates.Shader = _currentShader;
    }

    /// <summary>
    /// Selects the shader used when queued geometry is next flushed.
    /// </summary>
    /// <param name="shader">The shader to select.</param>
    /// <remarks>
    /// Shader selection is not stored per command. Changing it before a flush affects
    /// the queued groups submitted by that flush.
    /// </remarks>
    public void SetShader(IShader shader)
    {
        _currentShader = shader;
    }

    /// <summary>
    /// Clears the selected custom shader for the next flush.
    /// </summary>
    public void ClearShader()
    {
        _currentShader = null;
    }

    /// <summary>
    /// Begins a batch and initializes its render state.
    /// </summary>
    /// <param name="sortMode">Sort mode, or null to use <see cref="GameSettings.DefaultSortMode"/>.</param>
    /// <param name="blendMode">Blend mode, or null to use the configured default.</param>
    /// <param name="camera">Optional camera used for view state and the view-projection matrix. Attached cameras update automatically once per rendered frame.</param>
    /// <param name="renderTarget">Optional target to select for this and later submissions.</param>
    /// <exception cref="ObjectDisposedException">Thrown when the batcher has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a batch is already active.</exception>
    public virtual void Begin(
        SortMode? sortMode = null,
        IBlendMode blendMode = null,
        BaseCamera camera = null,
        IRenderTarget renderTarget = null)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(Name);

        if (_isDrawing)
        {
            throw new InvalidOperationException(
                $"{Name}.Begin called while already drawing. Call End() first.");
        }

        if (renderTarget != null)
            _renderTarget = renderTarget;

        _cmdCount = 0;
        _sortMode = sortMode ?? GameSettings.Instance.DefaultSortMode;
        _blendMode = blendMode
            ?? GameSettings.Instance.DefaultBlendMode
            ?? BlendMode.Alpha;
        _currentCamera = camera;

        camera?.Update();

        _renderStates.BlendMode = _blendMode;
        _renderStates.ViewProjection =
            camera?.ViewProjection
            ?? CreateDefaultViewProjection();

        if (camera != null)
            _renderTarget.SetView(camera);

        ApplyShader();

        _stats.Reset();
        _isDrawing = true;

        OnBegin();
    }

    /// <summary>
    /// Flushes queued commands and ends the active batch.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the batcher has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no batch is active.</exception>
    public virtual void End()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(Name);

        if (!_isDrawing)
        {
            throw new InvalidOperationException(
                $"{Name}.End called without a batching Begin.");
        }

        Flush();

        _isDrawing = false;
        _renderStates.Shader = null;

        OnEnd();
    }

    /// <summary>
    /// Sorts when required, builds geometry, submits compatible command groups,
    /// records statistics, and clears the queued command count.
    /// </summary>
    public virtual void Flush()
    {
        if (_cmdCount == 0)
            return;

        long startTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();

        if (_sortMode is not SortMode.Immediate and not SortMode.Deferred)
            SortCommands();

        BuildVertices();

        int totalVertices = GetTotalVertexCount();
        UploadGeometry(totalVertices);

        int drawCalls = 0;
        int index = 0;

        while (index < _cmdCount)
        {
            int groupStart = index++;

            while (index < _cmdCount && CanBatchTogether(groupStart, index))
                index++;

            int commandCount = index - groupStart;

            SetRenderStateForGroup(groupStart);

            // SetShader/ClearShader may be called during an active batch.
            _renderStates.Shader = _currentShader;

            SubmitGroup(groupStart, commandCount);
            drawCalls++;
        }

        _stats.CPUTime = (float)System.Diagnostics.Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
        _stats.DrawCalls = drawCalls;
        _stats.Vertices = totalVertices;
        _stats.Triangles = GetTriangleCount(totalVertices);
        _stats.Commands = _cmdCount;

        _cmdCount = 0;

        OnFlush();
    }

    /// <summary>Gets the number of vertices uploaded by the next flush.</summary>
    /// <returns>The number of vertices to upload.</returns>
    protected virtual int GetTotalVertexCount()
        => _cmdCount * VerticesPerCommand;

    /// <summary>Gets the triangle count reported for the supplied vertex count.</summary>
    /// <param name="totalVertices">The number of uploaded vertices.</param>
    /// <returns>The triangle count to place in <see cref="BatchStats"/>.</returns>
    protected virtual int GetTriangleCount(int totalVertices)
        => totalVertices / 3;

    /// <summary>Uploads the generated vertex data.</summary>
    /// <param name="vertexCount">The number of vertices to upload.</param>
    protected virtual void UploadGeometry(int vertexCount)
    {
        _vertexBuffer.Update(
            _vertexData,
            checked((uint)vertexCount),
            0);
    }

    /// <summary>Submits one contiguous group of compatible commands.</summary>
    /// <param name="commandStart">Index of the first command in the group.</param>
    /// <param name="commandCount">Number of commands in the group.</param>
    protected virtual void SubmitGroup(int commandStart, int commandCount)
    {
        int vertexStart = commandStart * VerticesPerCommand;
        int vertexCount = commandCount * VerticesPerCommand;

        _vertexBuffer.Draw(
            _renderTarget,
            checked((uint)vertexStart),
            checked((uint)vertexCount),
            _renderStates);
    }

    /// <summary>Sorts queued commands according to the active sort mode.</summary>
    protected abstract void SortCommands();

    /// <summary>Builds CPU-side vertex data for queued commands.</summary>
    protected abstract void BuildVertices();

    /// <summary>Expands command and geometry storage when capacity is exhausted.</summary>
    protected abstract void ResizeBuffers();

    /// <summary>Gets the concrete batcher's default command capacity.</summary>
    /// <returns>The default command capacity.</returns>
    protected virtual int GetDefaultCapacity()
        => InitialCapacity;

    /// <summary>Called after <see cref="Begin"/> has initialized the batch.</summary>
    protected virtual void OnBegin() { }

    /// <summary>Called after <see cref="End"/> has ended the batch.</summary>
    protected virtual void OnEnd() { }

    /// <summary>Called after a non-empty <see cref="Flush"/> completes.</summary>
    protected virtual void OnFlush() { }

    /// <summary>Called while resources owned by the batcher are being disposed.</summary>
    protected virtual void OnDispose() { }

    /// <summary>Determines whether two queued commands can share one submission.</summary>
    /// <param name="indexA">Index of the first command in the candidate group.</param>
    /// <param name="indexB">Index of the command being tested.</param>
    /// <returns>True when both commands can be submitted together.</returns>
    protected virtual bool CanBatchTogether(int indexA, int indexB)
        => true;

    /// <summary>Applies renderer-neutral state for the command group being submitted.</summary>
    /// <param name="commandIndex">Index of the first command in the group.</param>
    protected virtual void SetRenderStateForGroup(int commandIndex) { }

    /// <summary>Releases resources owned by the batcher.</summary>
    public virtual void Dispose()
    {
        if (_isDisposed)
            return;

        _vertexBuffer?.Dispose();
        _vertexBuffer = null;

        _cmdCount = 0;
        _vertexData = null;

        OnDispose();

        _isDisposed = true;
        GC.SuppressFinalize(this);
    }
}
