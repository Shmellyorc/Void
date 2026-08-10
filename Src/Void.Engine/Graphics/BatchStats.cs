namespace Void.Engine.Graphics;

public struct BatchStats
{
    public int DrawCalls;
    public int Vertices;
    public int Triangles;
    public int Commands;
    public int TextureSwitches;
    public int BlendModeSwitches;
    public float CPUTime;
    public float GPUTime;

    public void Reset()
    {
        DrawCalls = 0;
        Vertices = 0;
        Triangles = 0;
        Commands = 0;
        TextureSwitches = 0;
        BlendModeSwitches = 0;
        CPUTime = 0;
        GPUTime = 0;
    }

    public override string ToString()
        => $"DrawCalls: {DrawCalls}, Vertices: {Vertices}, Triangles: {Triangles}, Commands: {Commands}";
}
