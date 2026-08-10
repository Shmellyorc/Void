namespace Void.Engine.Systems;

public sealed class Camera
{
    internal readonly SFView _view;
    
    private Vect2 _position;
    private float _zoom = 1f;

    public Vect2 Position
    {
        get => _position;
        set
        {
            _position = value;
            _view.Center = _position;
        }
    }

    public float Zoom
    {
        get => _zoom;
        set
        {
            _zoom = Math.Max(0.1f, value);
            _view.Size = GameSettings.Instance.Viewport / _zoom;
        }
    }


    public Camera()
    {
        var settings = GameSettings.Instance;

        _view = new SFView(new SFFloatRect(Vect2.Zero, settings.Viewport));
        _position = settings.Viewport / 2f;
        _view.Center = _position;
    }

    public void ResetZoom() => Zoom = 1f;

    public Vect2 ScreenToWorld(Vect2 screenPos)
        => screenPos / _zoom + _position - GameSettings.Instance.Viewport / (2f * _zoom);

    public Vect2 WorldToScreen(Vect2 worldPos)
        => (worldPos - _position) * _zoom + GameSettings.Instance.Viewport / 2f;


    public static implicit operator SFView(Camera v) => v._view;
}
