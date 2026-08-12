namespace Void.Engine.Inputs.InputActions;

/// <summary>
/// Represents a snapshot of all input action states.
/// Query actions by name or enum.
/// </summary>
public readonly struct InputActionState
{
    private readonly Dictionary<string, bool> _states;
    private readonly Dictionary<string, bool> _previousStates;

    internal InputActionState(Dictionary<string, bool> states, Dictionary<string, bool> previousStates)
    {
        _states = states;
        _previousStates = previousStates;
    }

    /// <summary>
    /// Gets the trigger state of an action by string name.
    /// </summary>
    public ActionState GetState(string name)
    {
        bool current = _states.TryGetValue(name, out var c) && c;
        bool previous = _previousStates.TryGetValue(name, out var p) && p;

        if (current && !previous)
            return ActionState.Pressed;
        if (current && previous)
            return ActionState.Held;
        if (!current && previous)
            return ActionState.Released;
        return ActionState.Up;
    }

    /// <summary>
    /// Gets the trigger state of an action by enum.
    /// </summary>
    public ActionState GetState(Enum name) => GetState(name.ToEnumString());

    /// <summary>
    /// Checks if an action was just pressed this frame.
    /// </summary>
    public bool IsPressed(string name) => GetState(name) == ActionState.Pressed;

    /// <summary>
    /// Checks if an action was just pressed this frame (enum).
    /// </summary>
    public bool IsPressed(Enum name) => GetState(name) == ActionState.Pressed;

    /// <summary>
    /// Checks if an action is currently held.
    /// </summary>
    public bool IsHeld(string name)
    {
        return _states.TryGetValue(name, out var c) && c;
    }

    /// <summary>
    /// Checks if an action is currently held (enum).
    /// </summary>
    public bool IsHeld(Enum name) => IsHeld(name.ToEnumString());

    /// <summary>
    /// Checks if an action was just released this frame.
    /// </summary>
    public bool IsReleased(string name) => GetState(name) == ActionState.Released;

    /// <summary>
    /// Checks if an action was just released this frame (enum).
    /// </summary>
    public bool IsReleased(Enum name) => GetState(name) == ActionState.Released;

    /// <summary>
    /// Checks if an action is not active.
    /// </summary>
    public bool IsUp(string name) => GetState(name) == ActionState.Up;

    /// <summary>
    /// Checks if an action is not active (enum).
    /// </summary>
    public bool IsUp(Enum name) => GetState(name) == ActionState.Up;
}
