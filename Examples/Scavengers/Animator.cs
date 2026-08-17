namespace Scavengers;

public readonly struct Animation
{
    public Enum Type { get; }
    public Rect2[] Sources { get; }
    public float Speed { get; }
    public bool Looped { get; }

    public Animation(Enum type, Rect2[] sources, float speed, bool looped)
    {
        Type = type;
        Sources = sources;
        Speed = speed;
        Looped = looped;
    }
}

public sealed class Animator
{
    private readonly Dictionary<Enum, Animation> _anims = [];
    private readonly Texture _texture;
    private Enum _current = null;
    private bool _playing;
    public float _delta;
    private int _frame;

    public Action<Animation> AnimFinished { get; set; }

    public Animator(Texture texture)
    {
        _texture = texture;
    }

    public Animator Add(Enum name, Rect2[] rects, float speed, bool looped)
    {
        if (_anims.ContainsKey(name))
            return this;
        if (rects.IsEmpty())
            return this;

        var sources = new List<Rect2>(rects.Length);
        for (int i = 0; i < rects.Length; i++)
            sources.Add(rects[i]);

        _anims[name] = new Animation(name, sources.ToArray(), speed, looped);

        return this;
    }

    public Animator Play(Enum name, bool repeat)
    {
        if (!_anims.TryGetValue(name, out var result))
            return this;
        if (repeat && _current == name)
            return this;

        _frame = 0;
        _delta = 0;

        _current = name;
        _playing = true;

        return this;
    }

    public void Update(FrameTime frameTime)
    {
        if (!_playing || !_anims.TryGetValue(_current, out var anim))
            return;

        _delta += frameTime.DeltaTime;

        if (_delta > (1f / anim.Speed))
        {
            _delta -= 1f / anim.Speed;
            _frame++;

            if (_frame > anim.Sources.Length - 1)
            {
                if (anim.Looped)
                    _frame = 0;
                else
                {
                    _frame = anim.Sources.Length - 1;
                    _playing = false;

                    AnimFinished?.Invoke(anim);
                }
            }
        }
    }

    public void Draw(SpriteBatcher batch, Vect2 position, TextureEffects effects, float depth)
    {
        if (!_anims.TryGetValue(_current, out var anim))
            return;

        var frame = Math.Clamp(_frame, 0, anim.Sources.Length - 1);
        var rect = anim.Sources[frame];

        batch.Draw(_texture, position, rect, Color.White, 0f, Vect2.One, Vect2.Zero, effects, depth);
    }
}
