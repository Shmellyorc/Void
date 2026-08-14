using Void.Engine.Graphics.RenderTargets;

namespace Void.Engine.Graphics;

public abstract class BaseBatcher : IBatcher
{
    private const int InitialCapacity = 1024;
    private const int MaxCapacity = 65536;

    protected bool _isDisposed;
    protected bool _isDrawing;
    protected int _cmdCount;
    protected int _capacity;
    protected SortMode _sortMode;
    protected IBlendMode _blendMode;
    protected Camera _currentCamera;
    protected SFRenderStates _renderStates;
    protected Texture _currentTexture;
    protected BatchStats _stats;
    protected string _name;
    protected SFVertex[] _vertexData;
    private readonly IRenderTarget _defaultRenderTarget;

    // protected SFVertex[] _vertexBuffer;
    // protected SFVertexBuffer _gpuBuffer;
    protected int _vertexBufferSize;
    protected IRenderTarget _renderTarget;
    protected IVertexBuffer _vertexBuffer;

    public abstract string Name { get; }
    public bool IsDrawing => _isDrawing;
    public BatchStats Stats => _stats;
    public int DrawCallCount => _stats.DrawCalls;
    public int VertexCount => _stats.Vertices;
    public int CommandCount => _cmdCount;

    // protected SFRenderTexture RenderTexture => Game.Instance.Window._renderTexture;
    protected abstract int VerticesPerCommand { get; }



    public void SetRenderTarget(IRenderTarget target)
    {
        if (target == null)
            return;
        _renderTarget = target;
    }

    public void ResetRenderTarget()
    {
        if (_renderTarget == _defaultRenderTarget)
            return;
        _renderTarget = _defaultRenderTarget;
    }

    public bool IsUsingDefaultRenderTarget => _renderTarget == _defaultRenderTarget;

    public IRenderTarget GetRenderTarget() => _renderTarget;


    protected BaseBatcher(int capacity = 0)
    {
        if (capacity <= 0)
            capacity = GetDefaultCapacity();

        _capacity = Math.Clamp(capacity, 1, MaxCapacity);
        _cmdCount = 0;
        _sortMode = SortMode.BackToFront;
        _blendMode = BlendMode.Alpha;
        _stats = new BatchStats();

        int vertexCap = _capacity * VerticesPerCommand;
        // _vertexBuffer = new SFVertex[vertexCap];
        _vertexBuffer = new VertexBuffer(vertexCap);
        _vertexData = new SFVertex[vertexCap];
        _vertexBufferSize = vertexCap;
        _defaultRenderTarget = new TextureRenderTarget(Game.Instance.Window);
        _renderTarget = _defaultRenderTarget;

        // _gpuBuffer = new SFVertexBuffer(
        //     (uint)vertexCap,
        //     SFPrimitiveType.Triangles,
        //     SFUsageSpecifier.Stream
        // );

        _renderStates = new SFRenderStates
        {
            BlendMode = SFBlendMode.Alpha,
            Transform = SFTransform.Identity
        };

        _name = GetType().Name;
    }

    public virtual void Begin(SortMode? sortMode = null, IBlendMode blendMode = null, Camera camera = null, IRenderTarget renderTarget = null)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(Name);
        if (_isDrawing)
            throw new InvalidOperationException($"{Name}.Begin called while already drawing. Call End() first.");

        if (renderTarget != null)
            _renderTarget = renderTarget;
        else if (_renderTarget == null)
            _renderTarget = _defaultRenderTarget;

        _cmdCount = 0;
        _sortMode = sortMode ?? GameSettings.Instance.DefaultSortMode;
        _blendMode = blendMode ?? GameSettings.Instance.DefaultBlendMode ?? BlendMode.Alpha;
        _currentCamera = camera;

        _renderStates.BlendMode = ConvertToSFML(_blendMode);

        if (camera != null)
            _renderTarget.SetView(camera);

        _isDrawing = true;
        _stats.Reset();

        OnBegin();
    }

    public virtual void End()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(Name);
        if (!_isDrawing)
            throw new InvalidOperationException($"{Name}.End called without a batching Begin.");

        Flush();
        _isDrawing = false;

        OnEnd();
    }

    public virtual void Flush()
    {
        if (_cmdCount == 0) return;

        var sw = System.Diagnostics.Stopwatch.StartNew();

        if (_sortMode != SortMode.Immediate && _sortMode != SortMode.Deferred)
        {
            SortCommands();
        }

        BuildVertices();

        int totalVertices = _cmdCount * VerticesPerCommand;
        // _gpuBuffer.Update(_vertexBuffer, (uint)totalVertices, 0);
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

            _vertexBuffer.Draw(_renderTarget, (uint)vertexStart, (uint)vertexCount, _renderStates);
            // _gpuBuffer.Draw(
            //     _vertexBuffer,
            //     (uint)vertexStart,
            //     (uint)vertexCount,
            //     _renderStates
            // );

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


    protected abstract void SortCommands();
    protected abstract void BuildVertices();
    protected abstract void ResizeBuffers();
    protected virtual int GetDefaultCapacity() => InitialCapacity;
    protected virtual void OnBegin() { }
    protected virtual void OnEnd() { }
    protected virtual void OnFlush() { }
    protected virtual void OnDispose() { }
    protected virtual bool CanBatchTogether(int indexA, int indexB) => true;
    protected virtual void SetRenderStateForGroup(int commandIndex) { }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected SFBlendMode.Factor ConvertFactor(BlendFactor factor)
    {
        return factor switch
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
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected SFBlendMode.Equation ConvertEquation(BlendEquation equation)
    {
        return equation switch
        {
            BlendEquation.Add => SFBlendMode.Equation.Add,
            BlendEquation.Subtract => SFBlendMode.Equation.Subtract,
            BlendEquation.ReverseSubtract => SFBlendMode.Equation.ReverseSubtract,
            BlendEquation.Min => SFBlendMode.Equation.Min,
            BlendEquation.Max => SFBlendMode.Equation.Max,
            _ => SFBlendMode.Equation.Add
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected SFBlendMode ConvertToSFML(IBlendMode blendMode)
    {
        return new SFBlendMode(
            ConvertFactor(blendMode.ColorSrcFactor),
            ConvertFactor(blendMode.ColorDstFactor),
            ConvertEquation(blendMode.ColorEquation),
            ConvertFactor(blendMode.AlphaSrcFactor),
            ConvertFactor(blendMode.AlphaDstFactor),
            ConvertEquation(blendMode.AlphaEquation)
        );
    }

    public virtual void Dispose()
    {
        if (_isDisposed) return;

        _vertexBuffer?.Dispose();
        _vertexBuffer = null;
        _cmdCount = 0;
        _isDisposed = true;

        OnDispose();
    }
}