// ============================================================================
//  SceneManager.cs - Scavengers Demo Scene Management System
// ============================================================================
//  This file contains the scene management system used throughout the game.
//  It handles adding, removing, updating, and drawing scenes with layer-based
//  ordering.
//
//  The demo shows how to:
//  - Create a simple scene management system
//  - Use layer-based ordering for rendering
//  - Handle scene lifecycle (enter, update, draw, exit)
//  - Manage scene transitions with dirty state tracking
// ============================================================================

namespace Scavengers;

/// <summary>
/// Base class for all scenes in the game.
/// A scene represents a distinct state or screen in the game (gameplay, game over, transition, etc.)
/// </summary>
public class Scene
{
    /// <summary>
    /// Layer determines draw order. Higher layers are drawn on top.
    /// </summary>
    public int Layer { get; set; }

    /// <summary>
    /// Returns true if the scene is marked for removal.
    /// </summary>
    public bool IsExiting { get; private set; }

    /// <summary>
    /// Called when the scene is added to the SceneManager.
    /// Use this for initialization logic.
    /// </summary>
    public virtual void OnEnter() { }

    /// <summary>
    /// Called when the scene is removed from the SceneManager.
    /// Use this for cleanup logic.
    /// </summary>
    public virtual void OnExit()
    {
        if (IsExiting)
            return;

        IsExiting = true;
    }

    /// <summary>
    /// Called every frame while the scene is active.
    /// </summary>
    public virtual void Update(FrameTime frameTime) { }

    /// <summary>
    /// Called every frame while the scene is active.
    /// Use this for rendering.
    /// </summary>
    public virtual void Draw(FrameTime frameTime) { }

    /// <summary>
    /// Marks the scene for removal and removes it from the SceneManager.
    /// </summary>
    public void ExitScene()
    {
        if (IsExiting)
            return;

        SceneManager.Instance.Remove(this);
    }
}

/// <summary>
/// Manages all active scenes in the game.
/// Handles adding, removing, updating, drawing, and sorting scenes by layer.
/// </summary>
public sealed class SceneManager
{
    [Flags]
    private enum DirtyState
    {
        None,
        AddOrRemove,
        Layers
    }

    private static readonly Comparison<Scene> Comparison = (a, b) => a.Layer.CompareTo(b.Layer);
    private readonly List<Scene> _scenes = [];
    private readonly List<Scene> _active = [];
    private DirtyState _state;

    /// <summary>
    /// Singleton instance of the SceneManager.
    /// </summary>
    public static SceneManager Instance { get; private set; }

    /// <summary>
    /// Returns a read-only list of all registered scenes.
    /// </summary>
    public IReadOnlyList<Scene> Scenes => _scenes;

    /// <summary>
    /// Initializes the singleton instance.
    /// </summary>
    public SceneManager() => Instance ??= this;

    /// <summary>
    /// Updates all active scenes.
    /// </summary>
    public void Update(FrameTime frameTime)
    {
        if (_scenes.Count == 0)
            return;

        // Rebuild the active list if scenes were added or removed
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

            // Sort by layer if layer order changed
            if (_state.HasFlag(DirtyState.Layers))
                _active.Sort(Comparison);

            _state = DirtyState.None;
        }

        foreach (var scene in _active)
            scene.Update(frameTime);
    }

    /// <summary>
    /// Draws all active scenes in layer order.
    /// </summary>
    public void Draw(FrameTime frameTime)
    {
        foreach (var scene in _active)
        {
            if (scene == null || scene.IsExiting)
                continue;

            scene.Draw(frameTime);
        }
    }

    /// <summary>
    /// Adds one or more scenes to the manager.
    /// </summary>
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

    /// <summary>
    /// Removes one or more scenes from the manager.
    /// </summary>
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

    /// <summary>
    /// Removes all active scenes.
    /// </summary>
    public void Clear()
    {
        if (_scenes.Count == 0)
            return;

        Remove([.. _scenes]);
    }

    /// <summary>
    /// Gets the first scene of the specified type.
    /// </summary>
    public TScene GetScene<TScene>() where TScene : Scene
        => _scenes.OfType<TScene>().FirstOrDefault();
}