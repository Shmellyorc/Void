using Void.Engine.Inputs.Gamepads;
using Void.Engine.Inputs.Keyboards;
using Void.Engine.Inputs.Mouses;

namespace Void.Engine.Inputs.InputActions;

/// <summary>
/// Static manager for named input actions with string and enum support.
/// Zero-GC snapshot-based state tracking.
/// </summary>
public static class InputAction
{
    private static readonly Dictionary<string, ActionBinding> _actions = new(StringComparer.OrdinalIgnoreCase);
    private static Dictionary<string, bool> _currentStates = new(StringComparer.OrdinalIgnoreCase);
    private static Dictionary<string, bool> _previousStates = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets all registered actions.
    /// </summary>
    public static IReadOnlyCollection<ActionBinding> Actions => _actions.Values;

    /// <summary>
    /// Creates or gets an action by string name.
    /// </summary>
    public static ActionBinding AddAction(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        if (_actions.TryGetValue(name, out var existing))
            return existing;

        var action = new ActionBinding(name);
        _actions[name] = action;
        return action;
    }

    /// <summary>
    /// Creates or gets an action by enum.
    /// </summary>
    public static ActionBinding AddAction(Enum name)
        => AddAction(name.ToEnumString());

    /// <summary>
    /// Gets an action by string name.
    /// </summary>
    public static ActionBinding GetAction(string name)
        => _actions.TryGetValue(name, out var action) ? action : null;

    /// <summary>
    /// Gets an action by enum.
    /// </summary>
    public static ActionBinding GetAction(Enum name)
        => _actions.TryGetValue(name.ToEnumString(), out var action) ? action : null;

    /// <summary>
    /// Checks if an action with the given name exists.
    /// </summary>
    public static bool HasAction(string name) => _actions.ContainsKey(name);

    /// <summary>
    /// Checks if an action with the given enum exists.
    /// </summary>
    public static bool HasAction(Enum name) => _actions.ContainsKey(name.ToEnumString());

    /// <summary>
    /// Removes an action by string name.
    /// </summary>
    public static bool RemoveAction(string name)
    {
        _previousStates.Remove(name);
        _currentStates.Remove(name);
        return _actions.Remove(name);
    }

    /// <summary>
    /// Removes an action by enum.
    /// </summary>
    public static bool RemoveAction(Enum name) => RemoveAction(name.ToEnumString());

    /// <summary>
    /// Clears all actions and states.
    /// Call on engine shutdown.
    /// </summary>
    public static void Clear()
    {
        _actions.Clear();
        _currentStates.Clear();
        _previousStates.Clear();
    }

    /// <summary>
    /// Gets a snapshot of all action states.
    /// Reads input directly from input systems, like Keyboard.GetState().
    /// Zero allocation - reuses internal dictionaries.
    /// </summary>
    /// <returns>An <see cref="InputActionState"/> with all action states.</returns>
    public static InputActionState GetState()
    {
        var mouse = Mouse.GetState();
        var keyboard = Keyboard.GetState();
        var gamepad = Gamepad.GetState();

        var temp = _previousStates;
        _previousStates = _currentStates;
        _currentStates = temp;

        _currentStates.Clear();

        foreach (var (name, action) in _actions)
            _currentStates[name] = action.Evaluate(mouse, keyboard, gamepad);

        return new InputActionState(_currentStates, _previousStates);
    }
}