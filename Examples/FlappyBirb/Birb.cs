namespace FlappyBirb;

public sealed class Birb
{
    private const float AnimSpeed = 6f;

    private float _delta;
    private int _frame;
    private readonly Rect2[] _anims = [new(264, 64, 17, 12), new(264, 90, 17, 12), new(223, 124, 17, 12)];
    private readonly Texture _texture;
    private Vect2 _position;

    private KeyboardState _state = new(), _oldState = new();

    public Birb(Vect2 position)
    {
        _position = position;
        _texture = AssetManager.Instance.Load<Texture>("Spritesheet.png");
    }

    public void Update(FrameTime frameTime)
    {
        _delta += frameTime.DeltaTime;
        _oldState = _state;
        _state = Keyboard.GetState();

        r += frameTime.DeltaTime * 1f;

        if (_delta > (1f / AnimSpeed))
        {
            _delta -= (1f / AnimSpeed);
            _frame++;

            if (_frame > _anims.Length - 1)
                _frame = 0;
        }
    }

    float r;

    public void Draw(SpriteBatcher batch, FrameTime frameTime)
    {
        var rect = _anims[Math.Min(_frame, _anims.Length - 1)];

        batch.Draw(_texture, _position, rect, Color.White, r, Vect2.One, rect.Size / 2f, TextureEffects.None, 0.3f);
    }
}
