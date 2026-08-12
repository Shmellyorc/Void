using Void.Engine.Inputs.Keyboards;
using Void.Engine.Inputs.Mouses;

namespace FlappyBirb;

public sealed class Birb
{
    private const float AnimSpeed = 6f;

    private readonly Rect2[] _anims = [.. Global.Sheet.GetBounds("Bird0", "Bird1", "Bird2")];
    private float _rotate, _delta, _velocity;
    private Vect2 _position;
    private int _frame;

    private KeyboardState _keyState, _oldKeyState;
    private MouseState _mouseState, _oldMoustState;

    public Birb(Vect2 position) => _position = position;

    public void Update(FrameTime frameTime)
    {
        _oldKeyState = _keyState;
        _oldMoustState = _mouseState;
        _keyState = Keyboard.GetState();
        _mouseState = Mouse.GetState();

        if ((_oldKeyState.IsKeyUp(KeyboardKey.Space) && _keyState.IsKeyDown(KeyboardKey.Space)) ||
            (_oldMoustState.IsButtonReleased(MouseButton.Left) && _mouseState.IsButtonPressed(MouseButton.Left)))
        {
            // flap jump
        }

        UpdateAnimate(frameTime);
    }

    private void UpdateAnimate(FrameTime frameTime)
    {
        _delta += frameTime.DeltaTime;

        if (_delta > (1f / AnimSpeed))
        {
            _delta -= 1f / AnimSpeed;
            _frame++;

            if (_frame > _anims.Length - 1)
                _frame = 0;
        }
    }

    public void Draw(SpriteBatcher batch)
    {
        var rect = _anims[Math.Min(_frame, _anims.Length - 1)];

        batch.Draw(Global.Texture, _position, rect, Color.White, _rotate, Vect2.One, rect.Size / 2f, TextureEffects.None, 0.3f);
    }
}
