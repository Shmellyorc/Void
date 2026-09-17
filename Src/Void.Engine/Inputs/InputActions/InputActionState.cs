// ============================================================================
//  InputActionState.cs
// ============================================================================
//  Represents a snapshot of all input action states with query methods
//  for JustPressed, Pressed, JustReleased, and Released states.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Inputs.InputActions;

/// <summary>
/// Represents a read-only snapshot of the registered input actions for a game update.
/// </summary>
/// <remarks>
/// <para>
/// A snapshot stores both the current and previous state of each action so transition
/// queries remain stable for the lifetime of the value. Snapshots returned by
/// <see cref="InputAction.GetState"/> may therefore be retained and queried later without
/// being affected by subsequent input updates.
/// </para>
/// <para>
/// The boolean query methods describe two related kinds of state. <see cref="IsJustPressed(string)"/>
/// and <see cref="IsJustReleased(string)"/> report transitions that occurred on this update,
/// while <see cref="IsPressed(string)"/> and <see cref="IsReleased(string)"/> report whether
/// the action is currently pressed or released. Because of this, an action is both
/// just pressed and pressed on its first pressed update, and both just released and
/// released on its first released update.
/// </para>
/// <para>
/// <see cref="GetState(string)"/> returns one <see cref="ActionState"/> value describing the
/// transition between the previous and current update. This differs from the boolean
/// helpers, whose current-state queries intentionally overlap with transition queries.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// var state = InputAction.GetState();
///
/// if (state.IsJustPressed("Jump"))
///     StartJump();
///
/// if (state.IsPressed("MoveLeft"))
///     MoveLeft();
///
/// if (state.IsJustReleased("Pause"))
///     ClosePauseMenu();
/// </code>
/// </para>
/// <para>
/// <b>Missing and Default Actions:</b>
/// A missing action, an empty action name, or a default <see cref="InputActionState"/> is
/// treated as released. Transition queries return <see langword="false"/>,
/// <see cref="IsPressed(string)"/> returns <see langword="false"/>,
/// <see cref="IsReleased(string)"/> returns <see langword="true"/>, and
/// <see cref="GetState(string)"/> returns <see cref="ActionState.Up"/>.
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// A constructed snapshot is immutable and may be read concurrently.
/// </para>
/// </remarks>
public readonly struct InputActionState
{
    private readonly Dictionary<string, bool> _states;
    private readonly Dictionary<string, bool> _previousStates;

    internal InputActionState(
        Dictionary<string, bool> states,
        Dictionary<string, bool> previousStates)
    {
        ArgumentNullException.ThrowIfNull(states);
        ArgumentNullException.ThrowIfNull(previousStates);

        if (states.Count == 0 && previousStates.Count == 0)
        {
            _states = null;
            _previousStates = null;
            return;
        }

        _states = new Dictionary<string, bool>(
            states,
            StringComparer.OrdinalIgnoreCase);

        _previousStates = new Dictionary<string, bool>(
            previousStates,
            StringComparer.OrdinalIgnoreCase);
    }

    internal bool Matches(
        Dictionary<string, bool> states,
        Dictionary<string, bool> previousStates)
    {
        ArgumentNullException.ThrowIfNull(states);
        ArgumentNullException.ThrowIfNull(previousStates);

        return MatchesDictionary(_states, states) &&
               MatchesDictionary(_previousStates, previousStates);
    }

    private static bool MatchesDictionary(
        Dictionary<string, bool> snapshot,
        Dictionary<string, bool> source)
    {
        if (snapshot == null)
            return source.Count == 0;

        if (snapshot.Count != source.Count)
            return false;

        foreach (KeyValuePair<string, bool> pair in source)
        {
            if (!snapshot.TryGetValue(pair.Key, out bool value) ||
                value != pair.Value)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Gets the transition state of the specified action.
    /// </summary>
    /// <param name="name">The action name.</param>
    /// <returns>
    /// <see cref="ActionState.JustPressed"/> if the action changed from released to pressed;
    /// <see cref="ActionState.Pressed"/> if it was pressed on both updates;
    /// <see cref="ActionState.JustReleased"/> if it changed from pressed to released;
    /// otherwise, <see cref="ActionState.Up"/>.
    /// </returns>
    public ActionState GetState(string name)
    {
        if (string.IsNullOrEmpty(name))
            return ActionState.Up;

        bool current =
            _states != null &&
            _states.TryGetValue(name, out bool currentValue) &&
            currentValue;

        bool previous =
            _previousStates != null &&
            _previousStates.TryGetValue(name, out bool previousValue) &&
            previousValue;

        if (current && !previous)
            return ActionState.JustPressed;

        if (current)
            return ActionState.Pressed;

        if (previous)
            return ActionState.JustReleased;

        return ActionState.Up;
    }

    /// <summary>
    /// Gets the transition state of the action identified by the specified enum value.
    /// </summary>
    /// <param name="name">The enum value used to identify the action.</param>
    /// <returns>
    /// The action's <see cref="ActionState"/>, or <see cref="ActionState.Up"/> when
    /// <paramref name="name"/> is <see langword="null"/> or the action does not exist.
    /// </returns>
    public ActionState GetState(Enum name)
        => name == null
            ? ActionState.Up
            : GetState(name.ToEnumString());

    /// <summary>
    /// Determines whether the specified action became pressed during this update.
    /// </summary>
    /// <param name="name">The action name.</param>
    /// <returns>
    /// <see langword="true"/> only when the action changed from released to pressed;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool IsJustPressed(string name)
        => GetState(name) == ActionState.JustPressed;

    /// <summary>
    /// Determines whether the action identified by the specified enum value became pressed during this update.
    /// </summary>
    /// <param name="name">The enum value used to identify the action.</param>
    /// <returns>
    /// <see langword="true"/> only when the action changed from released to pressed;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool IsJustPressed(Enum name)
        => GetState(name) == ActionState.JustPressed;

    /// <summary>
    /// Determines whether the specified action is currently pressed.
    /// </summary>
    /// <param name="name">The action name.</param>
    /// <returns>
    /// <see langword="true"/> while the action is pressed, including the update on which
    /// it first became pressed; otherwise, <see langword="false"/>.
    /// </returns>
    public bool IsPressed(string name)
    {
        if (string.IsNullOrEmpty(name) || _states == null)
            return false;

        return _states.TryGetValue(name, out bool current) &&
               current;
    }

    /// <summary>
    /// Determines whether the action identified by the specified enum value is currently pressed.
    /// </summary>
    /// <param name="name">The enum value used to identify the action.</param>
    /// <returns>
    /// <see langword="true"/> while the action is pressed, including the update on which
    /// it first became pressed; otherwise, <see langword="false"/>.
    /// </returns>
    public bool IsPressed(Enum name)
        => name != null &&
           IsPressed(name.ToEnumString());

    /// <summary>
    /// Determines whether the specified action became released during this update.
    /// </summary>
    /// <param name="name">The action name.</param>
    /// <returns>
    /// <see langword="true"/> only when the action changed from pressed to released;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool IsJustReleased(string name)
        => GetState(name) == ActionState.JustReleased;

    /// <summary>
    /// Determines whether the action identified by the specified enum value became released during this update.
    /// </summary>
    /// <param name="name">The enum value used to identify the action.</param>
    /// <returns>
    /// <see langword="true"/> only when the action changed from pressed to released;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool IsJustReleased(Enum name)
        => GetState(name) == ActionState.JustReleased;

    /// <summary>
    /// Determines whether the specified action is currently released.
    /// </summary>
    /// <param name="name">The action name.</param>
    /// <returns>
    /// <see langword="true"/> while the action is released, including the update on which
    /// it first became released. Missing actions are also treated as released.
    /// </returns>
    public bool IsReleased(string name)
        => !IsPressed(name);

    /// <summary>
    /// Determines whether the action identified by the specified enum value is currently released.
    /// </summary>
    /// <param name="name">The enum value used to identify the action.</param>
    /// <returns>
    /// <see langword="true"/> while the action is released, including the update on which
    /// it first became released. Missing or <see langword="null"/> actions are also treated as released.
    /// </returns>
    public bool IsReleased(Enum name)
        => !IsPressed(name);
}
