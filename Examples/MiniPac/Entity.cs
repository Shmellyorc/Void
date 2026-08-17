namespace MiniPac;

public class Entity
{
    public Vect2 Position;
    public Vect2 Location => MapHelper.WorldToMap(Position, (int)Globals.TileSize);
    public Vect2 Size => new(Globals.TileSize);
    public Rect2 Bounds => new(Location, Size);
    public MiniPackGame App => (MiniPackGame)Game.Instance;
    public bool IsExiting { get; private set; }

    public Entity(Vect2 position)
    {
        Position = position;
    }

    public virtual void Update(FrameTime frameTime) { }
    public virtual void Draw(PrimitiveBatcher batch) { }

    public virtual void OnExit()
    {
        if (IsExiting)
            return;

        IsExiting = true;
    }

    public void Destroy()
    {
        if (IsExiting)
            return;

        OnExit();
    }
}
