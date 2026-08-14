// using Void.Engine.Graphics.RenderTargets;

// internal sealed class WindowRenderTarget : IRenderTarget
// {
//     private readonly SFRenderTexture _renderTexture;
//     private SFView _view = new();
//     private bool _disposed;

//     internal SFRenderTexture RenderTexture => _renderTexture;

//     public int Width { get; }
//     public int Height { get; }
//     public bool Srgb => false;
//     public Vect2 Size => new(Width, Height);

//     internal WindowRenderTarget(Window window)
//     {
//         if (window == null) throw new ArgumentNullException(nameof(window));
//         _renderTexture = window._renderTexture;
//         Width = (int)_renderTexture.Size.X;
//         Height = (int)_renderTexture.Size.Y;
//         _view = _renderTexture.GetView();
//     }

//     public void SetView(Camera camera)
//     {
//         if (_disposed) throw new ObjectDisposedException(nameof(WindowRenderTarget));
//         if (_renderTexture == null || _renderTexture.IsInvalid)
//             return;
//         _renderTexture?.SetView(camera);
//         _view = _renderTexture?.GetView();
//     }

//     public void Clear(Color color)
//     {
//         if (_disposed) throw new ObjectDisposedException(nameof(WindowRenderTarget));
//         _renderTexture.Clear(color);
//     }

//     public void Display()
//     {
//         if (_disposed) throw new ObjectDisposedException(nameof(WindowRenderTarget));
//         _renderTexture.Display();
//     }

//     public void Draw(IVertexBuffer buffer, uint vertexStart, uint vertexCount, SFRenderStates states)
//     {
//         if (_disposed) throw new ObjectDisposedException(nameof(WindowRenderTarget));
//         buffer.Draw(this, vertexStart, vertexCount, states);
//     }

//     public void Dispose()
//     {
//         if (_disposed) return;

//         // NOTE: Don't dispose _renderTexture — it's owned by the Window class

//         _disposed = true;
//         GC.SuppressFinalize(this);
//     }
// }