// ============================================================================
//  InputActionState.cs
// ============================================================================
//  Represents a snapshot of all input action states with query methods
//  for Pressed, Held, Released, and Up states.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Inputs.InputActions;

/// <summary>
/// Represents a snapshot of all input action states with query methods
/// for Pressed, Held, Released, and Up states.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="InputActionState"/> structure provides a read-only snapshot
/// of all registered input actions at a specific moment in time. It is
/// returned by <see cref="InputAction.GetState"/> and should be used for
/// all action queries within a frame.
/// </para>
/// <para>
/// This structure supports querying actions by either string name or enum,
/// and provides methods for checking the four possible states:
/// <list type="bullet">
///   <item><description><see cref="ActionState.Pressed"/> - Action was just pressed this frame</description></item>
///   <item><description><see cref="ActionState.Held"/> - Action is being held down</description></item>
///   <item><description><see cref="ActionState.Released"/> - Action was just released this frame</description></item>
///   <item><description><see cref="ActionState.Up"/> - Action is not active</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Get the action state snapshot
/// var state = InputAction.GetState();
/// 
/// // Query by string name
/// if (state.IsPressed("Jump"))
///     Jump();
/// 
/// if (state.IsHeld("MoveLeft"))
///     MoveLeft();
/// 
/// // Query by enum
/// if (state.IsPressed(Actions.Jump))
///     Jump();
/// 
/// // Get detailed state
/// var actionState = state.GetState("Sprint");
/// switch (actionState)
/// {
///     case ActionState.Pressed: StartSprint(); break;
///     case ActionState.Held: ContinueSprint(); break;
///     case ActionState.Released: StopSprint(); break;
/// }
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This structure is immutable and thread-safe. All fields are read-only.
/// </para>
/// </remarks>
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
    /// <param name="name">The name of the action.</param>
    /// <returns>The current <see cref="ActionState"/> of the action.</returns>
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
    /// <param name="name">The enum representing the action name.</param>
    /// <returns>The current <see cref="ActionState"/> of the action.</returns>
    public ActionState GetState(Enum name) => GetState(name.ToEnumString());

    /// <summary>
    /// Checks if an action was just pressed this frame.
    /// </summary>
    /// <param name="name">The name of the action.</param>
    /// <returns><see langword="true"/> if the action was just pressed; otherwise, <see langword="false"/>.</returns>
    public bool IsPressed(string name) => GetState(name) == ActionState.Pressed;

    /// <summary>
    /// Checks if an action was just pressed this frame (enum).
    /// </summary>
    /// <param name="name">The enum representing the action name.</param>
    /// <returns><see langword="true"/> if the action was just pressed; otherwise, <see langword="false"/>.</returns>
    public bool IsPressed(Enum name) => GetState(name) == ActionState.Pressed;

    /// <summary>
    /// Checks if an action is currently held.
    /// </summary>
    /// <param name="name">The name of the action.</param>
    /// <returns><see langword="true"/> if the action is being held; otherwise, <see langword="false"/>.</returns>
    public bool IsHeld(string name)
    {
        return _states.TryGetValue(name, out var c) && c;
    }

    /// <summary>
    /// Checks if an action is currently held (enum).
    /// </summary>
    /// <param name="name">The enum representing the action name.</param>
    /// <returns><see langword="true"/> if the action is being held; otherwise, <see langword="false"/>.</returns>
    public bool IsHeld(Enum name) => IsHeld(name.ToEnumString());

    /// <summary>
    /// Checks if an action was just released this frame.
    /// </summary>
    /// <param name="name">The name of the action.</param>
    /// <returns><see langword="true"/> if the action was just released; otherwise, <see langword="false"/>.</returns>
    public bool IsReleased(string name) => GetState(name) == ActionState.Released;

    /// <summary>
    /// Checks if an action was just released this frame (enum).
    /// </summary>
    /// <param name="name">The enum representing the action name.</param>
    /// <returns><see langword="true"/> if the action was just released; otherwise, <see langword="false"/>.</returns>
    public bool IsReleased(Enum name) => GetState(name) == ActionState.Released;

    /// <summary>
    /// Checks if an action is not active.
    /// </summary>
    /// <param name="name">The name of the action.</param>
    /// <returns><see langword="true"/> if the action is not active; otherwise, <see langword="false"/>.</returns>
    public bool IsUp(string name) => GetState(name) == ActionState.Up;

    /// <summary>
    /// Checks if an action is not active (enum).
    /// </summary>
    /// <param name="name">The enum representing the action name.</param>
    /// <returns><see langword="true"/> if the action is not active; otherwise, <see langword="false"/>.</returns>
    public bool IsUp(Enum name) => GetState(name) == ActionState.Up;
}