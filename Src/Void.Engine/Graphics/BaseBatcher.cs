using RenderVertex = Void.Engine.Graphics.Rendering.Vertex;

// ============================================================================
//  BaseBatcher.cs
// ============================================================================
//  Abstract base class for batch rendering implementations. Provides core
//  batching functionality including command sorting, vertex buffer management,
//  render state handling, and performance statistics collection.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Graphics;

/// <summary>
/// Abstract base class for batch rendering implementations.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="BaseBatcher"/> class provides core batching functionality
/// for rendering large numbers of primitives with minimal draw calls.
/// </para>
/// <para>
/// Derived batchers should use the protected PascalCase extension surface
/// rather than relying on VOID's internal backing state.
/// </para>
/// <para>
/// This class is not thread-safe and should be accessed from the main thread.
/// </para>
/// </remarks>
public abstract class BaseBatcher : IBatcher
{
    private const int InitialCapacity = 1024;
    private const int MaxCapacity = 65536;

    private readonly IRenderTarget _defaultRenderTarget;

    // ------------------------------------------------------------------------
    // Built-in batcher backing state.
    // ------------------------------------------------------------------------
    // These remain internal so VOID's existing SpriteBatcher and
    // PrimitiveBatcher do not need a mechanical rename-only rewrite.
    // External batcher implementations should use the protected PascalCase
    // surface below instead.

    internal bool _isDisposed;
    internal bool _isDrawing;
    internal int _cmdCount;
    internal int _capacity;
    internal SortMode _sortMode;
    internal IBlendMode _blendMode;
    internal Camera _currentCamera;
    internal BatchRenderState _renderStates;
    internal BatchStats _stats;
    internal RenderVertex[] _vertexData;
    internal IShader _currentShader;
    internal int _vertexBufferSize;
    internal IRenderTarget _renderTarget;
    internal IVertexBuffer _vertexBuffer;

    // Temporary compatibility state used by the current built-in
    // SpriteBatcher. It will be removed when SpriteBatcher moves to the
    // indexed-quad path in the next step.
    internal Texture _currentTexture;

    // ------------------------------------------------------------------------
    // Protected extension surface.
    // ------------------------------------------------------------------------

    /// <summary>Gets whether this batcher has been disposed.</summary>
    protected bool IsDisposed => _isDisposed;

    /// <summary>Gets whether a batch is currently active.</summary>
    protected bool IsBatchActive => _isDrawing;

    /// <summary>Gets or sets the number of queued draw commands.</summary>
    protected int QueuedCommandCount
    {
        get => _cmdCount;
        set => _cmdCount = value;
    }

    /// <summary>Gets or sets the current command capacity.</summary>
    protected int Capacity
    {
        get => _capacity;
        set => _capacity = Math.Clamp(value, 1, MaxCapacity);
    }

    /// <summary>Gets the active sort mode.</summary>
    protected SortMode CurrentSortMode => _sortMode;

    /// <summary>Gets the active blend mode.</summary>
    protected IBlendMode CurrentBlendMode => _blendMode;

    /// <summary>Gets the camera used by the current batch.</summary>
    protected Camera CurrentCamera => _currentCamera;

    /// <summary>Gets the current backend-neutral render state.</summary>
    protected BatchRenderState RenderStates => _renderStates;

    /// <summary>Gets or sets the current batch statistics.</summary>
    protected BatchStats Statistics
    {
        get => _stats;
        set => _stats = value;
    }

    /// <summary>Gets or sets the CPU-side vertex data used by the batcher.</summary>
    protected RenderVertex[] VertexData
    {
        get => _vertexData;
        set => _vertexData = value;
    }

    /// <summary>Gets the shader selected for the current batch.</summary>
    protected IShader CurrentShader => _currentShader;

    /// <summary>Gets or sets the vertex buffer capacity in vertices.</summary>
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

    /// <summary>Gets or sets the vertex buffer used by this batcher.</summary>
    protected IVertexBuffer VertexBuffer
    {
        get => _vertexBuffer;
        set => _vertexBuffer = value;
    }

    /// <summary>Gets the name of the batcher.</summary>
    public abstract string Name { get; }

    /// <summary>Gets whether a batch is currently active.</summary>
    public bool IsDrawing => _isDrawing;

    /// <summary>Gets the performance statistics for the current batch.</summary>
    public BatchStats Stats => _stats;

    /// <summary>Gets the number of draw calls issued in the current batch.</summary>
    public int DrawCallCount => _stats.DrawCalls;

    /// <summary>Gets the number of vertices processed in the current batch.</summary>
    public int VertexCount => _stats.Vertices;

    /// <summary>Gets the number of queued commands.</summary>
    public int CommandCount => _cmdCount;

    /// <summary>Gets the number of vertices produced per command.</summary>
    protected abstract int VerticesPerCommand { get; }

    /// <summary>
    /// Initializes a new batcher.
    /// </summary>
    /// <param name="capacity">Initial command capacity, or zero for the batcher default.</param>
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

    /// <summary>Sets the render target used by subsequent batches.</summary>
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

    /// <summary>Gets whether the current target is the default game target.</summary>
    public bool IsUsingDefaultRenderTarget
        => ReferenceEquals(_renderTarget, _defaultRenderTarget);

    /// <summary>Gets the currently selected render target.</summary>
    public IRenderTarget GetRenderTarget()
        => _renderTarget;

    /// <summary>Copies the selected shader into the current render state.</summary>
    protected virtual void ApplyShader()
    {
        _renderStates.Shader = _currentShader;
    }

    /// <summary>Selects a shader for subsequent submissions.</summary>
    public void SetShader(IShader shader)
    {
        _currentShader = shader;
    }

    /// <summary>Clears the selected custom shader.</summary>
    public void ClearShader()
    {
        _currentShader = null;
    }

    /// <summary>Begins a new batch.</summary>
    public virtual void Begin(
        SortMode? sortMode = null,
        IBlendMode blendMode = null,
        Camera camera = null,
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

        _renderStates.BlendMode = _blendMode;
        _renderStates.ViewProjection =
            camera?.ViewProjectionMatrix
            ?? Camera.CreateDefaultViewProjection();

        if (camera != null)
            _renderTarget.SetView(camera);

        ApplyShader();

        _stats.Reset();
        _isDrawing = true;

        OnBegin();
    }

    /// <summary>Flushes the batch and ends drawing.</summary>
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

    /// <summary>Flushes queued commands without ending the batch.</summary>
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

    /// <summary>Gets the total number of vertices to upload for this flush.</summary>
    protected virtual int GetTotalVertexCount()
        => _cmdCount * VerticesPerCommand;

    /// <summary>Gets the triangle count reported for this flush.</summary>
    protected virtual int GetTriangleCount(int totalVertices)
        => totalVertices / 3;

    /// <summary>Uploads geometry for the current flush.</summary>
    protected virtual void UploadGeometry(int vertexCount)
    {
        _vertexBuffer.Update(
            _vertexData,
            checked((uint)vertexCount),
            0);
    }

    /// <summary>Submits one compatible group of commands.</summary>
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

    /// <summary>Sorts queued commands for the active sort mode.</summary>
    protected abstract void SortCommands();

    /// <summary>Builds vertex data for queued commands.</summary>
    protected abstract void BuildVertices();

    /// <summary>Resizes the batcher's command/geometry storage.</summary>
    protected abstract void ResizeBuffers();

    /// <summary>Gets the default command capacity for the concrete batcher.</summary>
    protected virtual int GetDefaultCapacity()
        => InitialCapacity;

    /// <summary>Called after a batch begins.</summary>
    protected virtual void OnBegin() { }

    /// <summary>Called after a batch ends.</summary>
    protected virtual void OnEnd() { }

    /// <summary>Called after queued commands are flushed.</summary>
    protected virtual void OnFlush() { }

    /// <summary>Called while the batcher is being disposed.</summary>
    protected virtual void OnDispose() { }

    /// <summary>Returns whether two queued commands can share one submission.</summary>
    protected virtual bool CanBatchTogether(int indexA, int indexB)
        => true;

    /// <summary>Applies render state for a compatible command group.</summary>
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
