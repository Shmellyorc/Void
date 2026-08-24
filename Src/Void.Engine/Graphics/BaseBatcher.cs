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
/// for rendering large numbers of primitives with minimal draw calls. It handles:
/// <list type="bullet">
///   <item><description>Command queuing and sorting</description></item>
///   <item><description>Vertex buffer management and resizing</description></item>
///   <item><description>Render state management (blend modes, shaders, textures)</description></item>
///   <item><description>Performance statistics collection</description></item>
///   <item><description>Render target switching</description></item>
/// </list>
/// </para>
/// <para>
/// This class is abstract and must be inherited by concrete batcher
/// implementations such as <see cref="SpriteBatcher"/> and <see cref="PrimitiveBatcher"/>.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Use a concrete batcher
/// var batcher = new SpriteBatcher();
/// 
/// // Begin a batch with back-to-front sorting
/// batcher.Begin(SortMode.BackToFront, BlendMode.Alpha, camera);
/// 
/// // Draw sprites
/// batcher.Draw(texture, position, Color.White);
/// 
/// // End the batch and flush to GPU
/// batcher.End();
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe and should be accessed from the main thread.
/// </para>
/// </remarks>
public abstract class BaseBatcher : IBatcher
{
    private const int InitialCapacity = 1024;
    private const int MaxCapacity = 65536;

    private readonly IRenderTarget _defaultRenderTarget;

    /// <summary>
    /// Whether the batcher has been disposed.
    /// </summary>
    protected bool _isDisposed;

    /// <summary>
    /// Whether a batch is currently active.
    /// </summary>
    protected bool _isDrawing;

    /// <summary>
    /// The current number of commands in the batch.
    /// </summary>
    protected int _cmdCount;

    /// <summary>
    /// The current batch capacity.
    /// </summary>
    protected int _capacity;

    /// <summary>
    /// The current sort mode.
    /// </summary>
    protected SortMode _sortMode;

    /// <summary>
    /// The current blend mode.
    /// </summary>
    protected IBlendMode _blendMode;

    /// <summary>
    /// The current camera.
    /// </summary>
    protected Camera _currentCamera;

    /// <summary>
    /// The current SFML render states.
    /// </summary>
    protected SFRenderStates _renderStates;

    /// <summary>
    /// The current texture.
    /// </summary>
    protected Texture _currentTexture;

    /// <summary>
    /// Performance statistics for the current batch.
    /// </summary>
    protected BatchStats _stats;

    /// <summary>
    /// The name of this batcher.
    /// </summary>
    protected string _name;

    /// <summary>
    /// The vertex data array.
    /// </summary>
    protected SFVertex[] _vertexData;

    /// <summary>
    /// The current shader.
    /// </summary>
    protected IShader _currentShader;

    /// <summary>
    /// The size of the vertex buffer.
    /// </summary>
    protected int _vertexBufferSize;

    /// <summary>
    /// The current render target.
    /// </summary>
    protected IRenderTarget _renderTarget;

    /// <summary>
    /// The vertex buffer used for rendering.
    /// </summary>
    protected IVertexBuffer _vertexBuffer;

    /// <summary>
    /// Gets the name of the batcher.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Gets a value indicating whether a batch is currently active.
    /// </summary>
    public bool IsDrawing => _isDrawing;

    /// <summary>
    /// Gets the performance statistics for the current batch.
    /// </summary>
    public BatchStats Stats => _stats;

    /// <summary>
    /// Gets the number of draw calls issued in the current batch.
    /// </summary>
    public int DrawCallCount => _stats.DrawCalls;

    /// <summary>
    /// Gets the number of vertices processed in the current batch.
    /// </summary>
    public int VertexCount => _stats.Vertices;

    /// <summary>
    /// Gets the number of commands in the current batch.
    /// </summary>
    public int CommandCount => _cmdCount;

    /// <summary>
    /// Gets the number of vertices per command for this batcher.
    /// </summary>
    protected abstract int VerticesPerCommand { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseBatcher"/> class.
    /// </summary>
    /// <param name="capacity">The initial capacity of the batch.</param>
    protected BaseBatcher(int capacity = 0)
    {
        _capacity = capacity > 0 ? Math.Clamp(capacity, 1, MaxCapacity) : GetDefaultCapacity();
        _cmdCount = 0;
        _sortMode = SortMode.BackToFront;
        _blendMode = BlendMode.Alpha;
        _stats = new BatchStats();

        int vertexCap = _capacity * VerticesPerCommand;
        _vertexBuffer = new VertexBuffer(vertexCap);
        _vertexData = new SFVertex[vertexCap];
        _vertexBufferSize = vertexCap;
        _defaultRenderTarget = new TextureRenderTarget(Game.Instance.Window);
        _renderTarget = _defaultRenderTarget;

        _renderStates = new SFRenderStates
        {
            BlendMode = SFBlendMode.Alpha,
            Transform = SFTransform.Identity,
            CoordinateType = SFCoordinateType.Pixels,
        };

        _name = GetType().Name;
    }

    /// <summary>
    /// Sets the render target for the batcher.
    /// </summary>
    /// <param name="target">The render target to draw to. If <see langword="null"/>, this method does nothing.</param>
    /// <remarks>
    /// <para>
    /// This method changes the render target that subsequent draw calls will
    /// render to. The batcher will use this target until it is changed again
    /// or <see cref="ResetRenderTarget"/> is called.
    /// </para>
    /// </remarks>
    public void SetRenderTarget(IRenderTarget target)
    {
        if (target != null)
            _renderTarget = target;
    }

    /// <summary>
    /// Resets the render target to the default (the game window).
    /// </summary>
    public void ResetRenderTarget()
    {
        _renderTarget = _defaultRenderTarget;
    }

    /// <summary>
    /// Gets a value indicating whether the batcher is using the default render target.
    /// </summary>
    public bool IsUsingDefaultRenderTarget => _renderTarget == _defaultRenderTarget;

    /// <summary>
    /// Gets the current render target used by the batcher.
    /// </summary>
    /// <returns>The current render target.</returns>
    public IRenderTarget GetRenderTarget() => _renderTarget;

    /// <summary>
    /// Applies the current shader to the render states.
    /// </summary>
    protected virtual void ApplyShader()
    {
        _renderStates.Shader = (_currentShader as Shader)?.SFShader;
    }

    /// <summary>
    /// Sets the shader to use for rendering.
    /// </summary>
    /// <param name="shader">The shader to use. Pass <see langword="null"/> to clear the shader.</param>
    /// <remarks>
    /// <para>
    /// The shader will be applied to all subsequent draw calls in the current batch.
    /// </para>
    /// <para>
    /// To clear the shader and use the default, call <see cref="ClearShader"/>.
    /// </para>
    /// </remarks>
    public void SetShader(IShader shader)
    {
        _currentShader = shader;
    }

    /// <summary>
    /// Clears the current shader, reverting to the default shader.
    /// </summary>
    public void ClearShader()
    {
        _currentShader = null;
    }

    /// <summary>
    /// Begins a new batch with the specified settings.
    /// </summary>
    /// <param name="sortMode">The sort mode to use. If <see langword="null"/>, the default is used.</param>
    /// <param name="blendMode">The blend mode to use. If <see langword="null"/>, the default is used.</param>
    /// <param name="camera">The camera to use for rendering. If <see langword="null"/>, no camera is applied.</param>
    /// <param name="renderTarget">The render target to draw to. If <see langword="null"/>, the current target is used.</param>
    /// <exception cref="ObjectDisposedException">Thrown when the batcher has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="Begin"/> is called while a batch is already active.</exception>
    public virtual void Begin(SortMode? sortMode = null, IBlendMode blendMode = null, Camera camera = null, IRenderTarget renderTarget = null)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(Name);
        if (_isDrawing)
            throw new InvalidOperationException($"{Name}.Begin called while already drawing. Call End() first.");

        _renderTarget = renderTarget ?? _renderTarget ?? _defaultRenderTarget;
        _cmdCount = 0;
        _sortMode = sortMode ?? GameSettings.Instance.DefaultSortMode;
        _blendMode = blendMode ?? GameSettings.Instance.DefaultBlendMode ?? BlendMode.Alpha;
        _currentCamera = camera;

        _renderStates.BlendMode = ConvertToSFML(_blendMode);

        if (camera != null)
            _renderTarget.SetView(camera);

        ApplyShader();
        _isDrawing = true;
        _stats.Reset();

        OnBegin();
    }

    /// <summary>
    /// Ends the current batch and flushes all commands to the GPU.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the batcher has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="End"/> is called without a matching <see cref="Begin"/>.</exception>
    public virtual void End()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(Name);
        if (!_isDrawing)
            throw new InvalidOperationException($"{Name}.End called without a batching Begin.");

        Flush();
        _isDrawing = false;
        _renderStates.Shader = null;

        OnEnd();
    }

    /// <summary>
    /// Flushes all pending commands to the GPU without ending the batch.
    /// </summary>
    public virtual void Flush()
    {
        if (_cmdCount == 0) return;

        var sw = System.Diagnostics.Stopwatch.StartNew();

        if (_sortMode != SortMode.Immediate && _sortMode != SortMode.Deferred)
            SortCommands();

        BuildVertices();

        int totalVertices = _cmdCount * VerticesPerCommand;
        _vertexBuffer.Update(_vertexData, (uint)totalVertices, 0);

        int drawCalls = 0;
        int index = 0;

        while (index < _cmdCount)
        {
            int groupStart = index;
            index++;

            while (index < _cmdCount && CanBatchTogether(groupStart, index))
                index++;

            int quadCount = index - groupStart;
            int vertexStart = groupStart * VerticesPerCommand;
            int vertexCount = quadCount * VerticesPerCommand;

            SetRenderStateForGroup(groupStart);

            if (_currentShader is Shader shaderAsset)
                _renderStates.Shader = shaderAsset.SFShader;

            _vertexBuffer.Draw(_renderTarget, (uint)vertexStart, (uint)vertexCount, _renderStates);
            drawCalls++;
        }

        sw.Stop();
        _stats.GPUTime = (float)sw.Elapsed.TotalMilliseconds;
        _stats.DrawCalls = drawCalls;
        _stats.Vertices = totalVertices;
        _stats.Triangles = totalVertices / 3;
        _stats.Commands = _cmdCount;

        _cmdCount = 0;
        OnFlush();
    }

    /// <summary>
    /// Sorts the commands for optimal rendering.
    /// </summary>
    protected abstract void SortCommands();

    /// <summary>
    /// Builds the vertices for all commands.
    /// </summary>
    protected abstract void BuildVertices();

    /// <summary>
    /// Resizes the vertex and command buffers when capacity is exceeded.
    /// </summary>
    protected abstract void ResizeBuffers();

    /// <summary>
    /// Gets the default capacity for the batcher.
    /// </summary>
    /// <returns>The default capacity.</returns>
    protected virtual int GetDefaultCapacity() => InitialCapacity;

    /// <summary>
    /// Called when a batch begins.
    /// </summary>
    protected virtual void OnBegin() { }

    /// <summary>
    /// Called when a batch ends.
    /// </summary>
    protected virtual void OnEnd() { }

    /// <summary>
    /// Called when a batch is flushed.
    /// </summary>
    protected virtual void OnFlush() { }

    /// <summary>
    /// Called when the batcher is disposed.
    /// </summary>
    protected virtual void OnDispose() { }

    /// <summary>
    /// Determines whether two commands can be batched together.
    /// </summary>
    /// <param name="indexA">The index of the first command.</param>
    /// <param name="indexB">The index of the second command.</param>
    /// <returns><see langword="true"/> if the commands can be batched; otherwise, <see langword="false"/>.</returns>
    protected virtual bool CanBatchTogether(int indexA, int indexB) => true;

    /// <summary>
    /// Sets the render state for a group of commands.
    /// </summary>
    /// <param name="commandIndex">The index of the first command in the group.</param>
    protected virtual void SetRenderStateForGroup(int commandIndex) { }

    /// <summary>
    /// Converts a <see cref="BlendFactor"/> to an SFML blend factor.
    /// </summary>
    /// <param name="factor">The blend factor to convert.</param>
    /// <returns>The corresponding SFML blend factor.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected SFBlendMode.Factor ConvertFactor(BlendFactor factor) => factor switch
    {
        BlendFactor.Zero => SFBlendMode.Factor.Zero,
        BlendFactor.One => SFBlendMode.Factor.One,
        BlendFactor.SrcColor => SFBlendMode.Factor.SrcColor,
        BlendFactor.OneMinusSrcColor => SFBlendMode.Factor.OneMinusSrcColor,
        BlendFactor.DstColor => SFBlendMode.Factor.DstColor,
        BlendFactor.OneMinusDstColor => SFBlendMode.Factor.OneMinusDstColor,
        BlendFactor.SrcAlpha => SFBlendMode.Factor.SrcAlpha,
        BlendFactor.OneMinusSrcAlpha => SFBlendMode.Factor.OneMinusSrcAlpha,
        BlendFactor.DstAlpha => SFBlendMode.Factor.DstAlpha,
        BlendFactor.OneMinusDstAlpha => SFBlendMode.Factor.OneMinusDstAlpha,
        _ => SFBlendMode.Factor.One
    };

    /// <summary>
    /// Converts a <see cref="BlendEquation"/> to an SFML blend equation.
    /// </summary>
    /// <param name="equation">The blend equation to convert.</param>
    /// <returns>The corresponding SFML blend equation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected SFBlendMode.Equation ConvertEquation(BlendEquation equation) => equation switch
    {
        BlendEquation.Add => SFBlendMode.Equation.Add,
        BlendEquation.Subtract => SFBlendMode.Equation.Subtract,
        BlendEquation.ReverseSubtract => SFBlendMode.Equation.ReverseSubtract,
        BlendEquation.Min => SFBlendMode.Equation.Min,
        BlendEquation.Max => SFBlendMode.Equation.Max,
        _ => SFBlendMode.Equation.Add
    };

    /// <summary>
    /// Converts an <see cref="IBlendMode"/> to an SFML blend mode.
    /// </summary>
    /// <param name="blendMode">The blend mode to convert.</param>
    /// <returns>The corresponding SFML blend mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected SFBlendMode ConvertToSFML(IBlendMode blendMode) => new SFBlendMode(
        ConvertFactor(blendMode.ColorSrcFactor),
        ConvertFactor(blendMode.ColorDstFactor),
        ConvertEquation(blendMode.ColorEquation),
        ConvertFactor(blendMode.AlphaSrcFactor),
        ConvertFactor(blendMode.AlphaDstFactor),
        ConvertEquation(blendMode.AlphaEquation)
    );

    /// <summary>
    /// Disposes the batcher and releases all resources.
    /// </summary>
    public virtual void Dispose()
    {
        if (_isDisposed) return;

        _vertexBuffer?.Dispose();
        _vertexBuffer = null;
        _cmdCount = 0;

        if (_vertexData != null)
            Array.Clear(_vertexData, 0, _vertexData.Length);

        OnDispose();

        _isDisposed = true;
    }
}