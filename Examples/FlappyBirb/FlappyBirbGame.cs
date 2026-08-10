namespace FlappyBirb;

public sealed class FlappyBirbGame(GameSettings settings) : Game(settings)
{
    private const float TimeSpawnTime = 3.5f;

    private readonly Rect2 _bgRect = new(0, 0, 144, 256);
    private readonly Rect2 _groundRect = new(146, 0, 154, 56);

    private Birb _birb;
    private readonly List<Pipe> _pipes = [], _pipesRemove = [];
    private SpriteBatcher _batch;
    private Texture _texture;
    private Camera _camera;
    private float _pipeDelay;

    private readonly float[] _bgParallax = [0, 144], _groundParallax = [0, 154];

    protected override void OnEnter()
    {
        _batch = new SpriteBatcher();
        _texture = AssetManager.Instance.Load<Texture>("Spritesheet.png");
        _camera = new Camera();
        _birb = new Birb(new(30, 60));

        base.OnEnter();
    }

    protected override void OnExit()
    {
        base.OnExit();
    }

    protected override void OnUpdate(FrameTime frameTime)
    {
        for (int i = 0; i < _bgParallax.Length; i++)
        {
            _bgParallax[i] -= frameTime.DeltaTime * 5f;

            if ((_bgParallax[i] + _bgRect.Width) < 0)
                _bgParallax[i] += _bgRect.Width * 2f;
        }

        for (int i = 0; i < _groundParallax.Length; i++)
        {
            _groundParallax[i] -= frameTime.DeltaTime * 30f;

            if ((_groundParallax[i] + _groundRect.Width) < 0)
                _groundParallax[i] += _groundRect.Width * 2f;
        }

        foreach (var pipe in _pipes)
        {
            pipe.Position += Vect2.Left * 30f * frameTime.DeltaTime;

            if ((pipe.Position.X + 30) < 0)
                _pipesRemove.Add(pipe);
        }

        foreach (var pipe in _pipesRemove)
            _pipes.Remove(pipe);
        _pipesRemove.Clear();

        _birb.Update(frameTime);

        if (_pipeDelay > TimeSpawnTime)
        {
            var range = FastRandom.Shared.RangeFloat(50, 150);

            _pipes.Add(new Pipe(new(_bgRect.Width + 60, range)));
            _pipeDelay -= TimeSpawnTime;
        }
        else
            _pipeDelay += frameTime.DeltaTime;

        base.OnUpdate(frameTime);
    }

    protected override void OnDraw(FrameTime frameTime)
    {
        _batch.Begin(SortMode.BackToFront, BlendMode.Alpha, _camera);

        for (int i = 0; i < _bgParallax.Length; i++)
            _batch.Draw(_texture, new Vect2(_bgParallax[i], 0), _bgRect, Color.White, 0f);

        for (int i = 0; i < _groundParallax.Length; i++)
        {
            var pos = new Vect2(_groundParallax[i], _bgRect.Height - _groundRect.Height);

            _batch.Draw(_texture, pos, _groundRect, Color.White, 1f);
        }

        foreach (var pipe in _pipes)
            pipe.Draw(_batch);

        _birb.Draw(_batch, frameTime);

        _batch.End();

        base.OnDraw(frameTime);
    }
}
