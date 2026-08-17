namespace Scavengers;

public class Scene
{
    public int Layer { get; set; }
    public bool IsExiting { get; private set; }

    public virtual void OnEnter() { }
    public virtual void OnExit()
    {
        if (IsExiting)
            return;

        IsExiting = true;
    }
    public virtual void Update(FrameTime frameTime) { }
    public virtual void Draw(FrameTime frameTime) { }

    public void ExitScene()
    {
        if (IsExiting)
            return;

        SceneManager.Instance.Remove(this);
    }
}


public sealed class SceneManager
{
    [Flags] private enum DirtyState { None, AddOrRemove, Layers }

    private static readonly Comparison<Scene> Comparison = (a, b) => a.Layer.CompareTo(b.Layer);
    private readonly List<Scene> _scenes = [], _active = [];
    private DirtyState _state;

    public static SceneManager Instance { get; private set; }
    public IReadOnlyList<Scene> Scenes => _scenes;

    public SceneManager() => Instance ??= this;

    public void Update(FrameTime frameTime)
    {
        if (_scenes.Count == 0)
            return;

        if (_state != DirtyState.None)
        {
            if (_state.HasFlag(DirtyState.AddOrRemove))
            {
                _active.EnsureCapacity(_scenes.Count);
                _active.Clear();

                foreach (var scene in _scenes)
                {
                    if (scene == null || scene.IsExiting)
                        continue;

                    _active.Add(scene);
                }
            }

            if (_state.HasFlag(DirtyState.Layers))
                _active.Sort(Comparison);

            _state = DirtyState.None;
        }

        foreach (var scene in _active)
            scene.Update(frameTime);
    }

    public void Draw(FrameTime frameTime)
    {
        foreach (var scene in _active)
        {
            if (scene == null || scene.IsExiting)
                continue;

            scene.Draw(frameTime);
        }
    }

    public void Add(params Scene[] scenes)
    {
        if (scenes.IsEmpty())
            return;

        var anyAdded = false;
        for (int i = 0; i < scenes.Length; i++)
        {
            var scene = scenes[i];

            if (scene == null || scene.IsExiting)
                continue;

            _scenes.Add(scene);

            scene.OnEnter();
            anyAdded = true;
        }

        if (anyAdded)
            _state |= DirtyState.AddOrRemove | DirtyState.Layers;
    }

    public void Remove(params Scene[] scenes)
    {
        if (scenes.IsEmpty())
            return;

        var anyRemoved = false;
        for (int i = 0; i < scenes.Length; i++)
        {
            var scene = scenes[i];

            if (scene == null || scene.IsExiting)
                continue;
            if (!_scenes.Remove(scene))
                continue;

            scene.OnExit();
            anyRemoved = true;
        }

        if (anyRemoved)
            _state |= DirtyState.AddOrRemove | DirtyState.Layers;
    }


    public void Clear()
    {
        if (_scenes.Count == 0)
            return;

        Remove([.. _scenes]);
    }

    public TScene GetScene<TScene>() where TScene : Scene
        => _scenes.OfType<TScene>().FirstOrDefault();
}
