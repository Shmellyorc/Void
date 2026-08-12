namespace FlappyBirb;

public sealed class FlappyBirbGame(GameSettings settings) : Game(settings)
{
    private const float TimeSpawnTime = 3.5f;

    private readonly float[] _bgParallax = [0, 144], _groundParallax = [0, 154];
    private readonly List<Pipe> _pipes = [], _pipesRemove = [];
    private Rect2 _bgRect, _groundRect;
    private SpriteBatcher _batch;
    private Camera _camera;
    private float _pipeDelay;
    private Birb _birb;


    protected override void OnEnter()
    {
        // var mount = AssetManager.Instance.LoadPack("GameAssets.pack");
        // AssetManager.Instance.AddMountToStart(mount);

        Global.Texture = AssetManager.Instance.Load<Texture>("Spritesheet.png");
        Global.Font = AssetManager.Instance.LoadSpriteFont("Fonts/Font.png", 1, 2);
        Global.Sheet = AssetManager.Instance.Load<Spritesheet>("Spritesheet.sheet");

        _bgRect = Global.Sheet.GetBounds("Background");
        _groundRect = Global.Sheet.GetBounds("Ground");
        _batch = new SpriteBatcher();
        _camera = new Camera();
        _birb = new Birb(new(30, 60));

        base.OnEnter();
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
            _batch.Draw(Global.Texture, new Vect2(_bgParallax[i], 0), _bgRect, Color.White, 0f);

        for (int i = 0; i < _groundParallax.Length; i++)
        {
            var pos = new Vect2(_groundParallax[i], _bgRect.Height - _groundRect.Height);

            _batch.Draw(Global.Texture, pos, _groundRect, Color.White, 1f);
        }

        foreach (var pipe in _pipes)
            pipe.Draw(_batch);

        _birb.Draw(_batch);

        _batch.DrawText(Global.Font, "Hello this is\na test!", new Vect2(0, 0), Color.White, TextAlignment.Center, Vect2.One, 1f);

        _batch.DrawNinePatch(Global.Texture, new(5, 5, 20, 20), Global.Sheet.GetBounds("Patch"), Global.Sheet.GetPatch("Patch"), Color.White, 1f);

        _batch.End();

        base.OnDraw(frameTime);
    }
}
