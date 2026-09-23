// ============================================================================
//  InputAction.cs
// ============================================================================
//  Static manager for named input actions with string and enum support,
//  providing frame-based action state tracking.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Generic;
using Void.Engine.Inputs.Gamepads;
using Void.Engine.Inputs.Keyboards;
using Void.Engine.Inputs.Mouses;

namespace Void.Engine.Inputs.InputActions;

/// <summary>
/// Manages named input actions and their keyboard, mouse, and gamepad bindings.
/// </summary>
/// <remarks>
/// <para>
/// Actions are registered by name and may contain one or more bindings. An action
/// is active when any of its bindings is active during the current game update.
/// Action names are converted to cached 64-bit identifiers for internal lookup.
/// </para>
/// <para>
/// The engine updates the action system once per game update. <see cref="GetState"/>
/// returns the snapshot produced by that update and does not poll devices or advance
/// action state when called. This allows multiple systems to query the same snapshot
/// without changing transition results.
/// </para>
/// <para>
/// <b>State Queries:</b>
/// <list type="bullet">
///   <item><description><see cref="InputActionState.IsJustPressed(string)"/> is true only on the update an action becomes pressed.</description></item>
///   <item><description><see cref="InputActionState.IsPressed(string)"/> is true while an action is currently pressed, including the update it was first pressed.</description></item>
///   <item><description><see cref="InputActionState.IsJustReleased(string)"/> is true only on the update an action becomes released.</description></item>
///   <item><description><see cref="InputActionState.IsReleased(string)"/> is true while an action is currently released, including the update it was first released.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// InputAction.AddAction("Jump")
///     .AddKey(KeyboardKey.Space)
///     .AddGamepad(GamepadButton.A);
///
/// InputAction.AddAction("MoveLeft")
///     .AddKey(KeyboardKey.Left)
///     .AddKey(KeyboardKey.A)
///     .AddGamepad(GamepadButton.LeftStickLeft);
///
/// var state = InputAction.GetState();
///
/// if (state.IsJustPressed("Jump"))
///     HandleJump();
///
/// if (state.IsPressed("MoveLeft"))
///     MoveLeft();
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// Registration, binding changes, and state access are intended for the main game thread.
/// </para>
/// </remarks>
public static class InputAction
{
    private static readonly Dictionary<ulong, ActionBinding> _actions = [];

    private static Dictionary<ulong, bool> _currentStates = [];
    private static Dictionary<ulong, bool> _previousStates = [];

    private static InputActionState _state;

    /// <summary>
    /// Gets the registered input actions.
    /// </summary>
    /// <remarks>
    /// The returned collection reflects the actions currently registered with the manager.
    /// </remarks>
    public static IReadOnlyCollection<ActionBinding> Actions => _actions.Values;

    /// <summary>
    /// Gets an existing action or creates a new action with the specified name.
    /// </summary>
    /// <param name="name">The action name.</param>
    /// <returns>The existing or newly created <see cref="ActionBinding"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> is empty or consists only of whitespace.
    /// </exception>
    public static ActionBinding AddAction(string name)
    {
        if (name is null)
            throw new ArgumentNullException(nameof(name));

        if (name.IsEmpty())
        {
            throw new ArgumentException(
                "Action name cannot be empty or whitespace.",
                nameof(name));
        }

        ulong hash = HashHelper.Cache64(name);

        if (_actions.TryGetValue(hash, out ActionBinding existing))
            return existing;

        var action = new ActionBinding(name);
        _actions[hash] = action;
        return action;
    }

    /// <summary>
    /// Gets an existing action or creates a new action using the specified enum value as its name.
    /// </summary>
    /// <param name="name">The enum value used to identify the action.</param>
    /// <returns>The existing or newly created <see cref="ActionBinding"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name"/> is <see langword="null"/>.
    /// </exception>
    public static ActionBinding AddAction(Enum name)
    {
        if (name is null)
            throw new ArgumentNullException(nameof(name));

        return AddAction(name.ToEnumString());
    }

    /// <summary>
    /// Gets the action registered with the specified name.
    /// </summary>
    /// <param name="name">The action name.</param>
    /// <returns>
    /// The matching <see cref="ActionBinding"/>, or <see langword="null"/> when the name is null,
    /// empty, whitespace, or no action is registered with that name.
    /// </returns>
    public static ActionBinding GetAction(string name)
        => TryGetAction(name, out ActionBinding action)
            ? action
            : null;

    /// <summary>
    /// Gets the action registered for the specified enum value.
    /// </summary>
    /// <param name="name">The enum value used to identify the action.</param>
    /// <returns>
    /// The matching <see cref="ActionBinding"/>, or <see langword="null"/> when the value is
    /// <see langword="null"/> or no action is registered for it.
    /// </returns>
    public static ActionBinding GetAction(Enum name)
        => TryGetAction(name, out ActionBinding action)
            ? action
            : null;

    /// <summary>
    /// Attempts to get the action registered with the specified name.
    /// </summary>
    /// <param name="name">The action name.</param>
    /// <param name="action">
    /// When this method returns, contains the matching <see cref="ActionBinding"/> when found;
    /// otherwise, <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the action exists; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool TryGetAction(string name, out ActionBinding action)
    {
        if (name.IsEmpty())
        {
            action = null;
            return false;
        }

        return _actions.TryGetValue(HashHelper.Cache64(name), out action);
    }

    /// <summary>
    /// Attempts to get the action registered for the specified enum value.
    /// </summary>
    /// <param name="name">The enum value used to identify the action.</param>
    /// <param name="action">
    /// When this method returns, contains the matching <see cref="ActionBinding"/> when found;
    /// otherwise, <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the action exists; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool TryGetAction(Enum name, out ActionBinding action)
    {
        if (name is null)
        {
            action = null;
            return false;
        }

        return _actions.TryGetValue(HashHelper.Cache64(name), out action);
    }

    /// <summary>
    /// Determines whether an action is registered with the specified name.
    /// </summary>
    /// <param name="name">The action name.</param>
    /// <returns>
    /// <see langword="true"/> if the action exists; otherwise, <see langword="false"/>.
    /// Null, empty, or whitespace names return <see langword="false"/>.
    /// </returns>
    public static bool HasAction(string name) => TryGetAction(name, out _);

    /// <summary>
    /// Determines whether an action is registered for the specified enum value.
    /// </summary>
    /// <param name="name">The enum value used to identify the action.</param>
    /// <returns>
    /// <see langword="true"/> if the action exists; otherwise, <see langword="false"/>.
    /// A <see langword="null"/> value returns <see langword="false"/>.
    /// </returns>
    public static bool HasAction(Enum name) => TryGetAction(name, out _);

    /// <summary>
    /// Removes the action registered with the specified name.
    /// </summary>
    /// <param name="name">The action name.</param>
    /// <returns>
    /// <see langword="true"/> if an action was removed; otherwise, <see langword="false"/>.
    /// Null, empty, or whitespace names are ignored and return <see langword="false"/>.
    /// </returns>
    public static bool RemoveAction(string name)
    {
        if (name.IsEmpty())
            return false;

        return RemoveAction(HashHelper.Cache64(name));
    }

    /// <summary>
    /// Removes the action registered for the specified enum value.
    /// </summary>
    /// <param name="name">The enum value used to identify the action.</param>
    /// <returns>
    /// <see langword="true"/> if an action was removed; otherwise, <see langword="false"/>.
    /// A <see langword="null"/> value is ignored and returns <see langword="false"/>.
    /// </returns>
    public static bool RemoveAction(Enum name) => name is not null && RemoveAction(HashHelper.Cache64(name));

    private static bool RemoveAction(ulong hash)
    {
        _previousStates.Remove(hash);
        _currentStates.Remove(hash);
        return _actions.Remove(hash);
    }

    /// <summary>
    /// Removes all registered actions and resets the current action state snapshot.
    /// </summary>
    /// <remarks>
    /// After clearing, <see cref="GetState"/> returns the default snapshot until the engine updates the action system again.
    /// </remarks>
    public static void Clear()
    {
        _actions.Clear();
        _currentStates.Clear();
        _previousStates.Clear();
        _state = default;
    }

    internal static void Update()
    {
        if (_actions.Count == 0)
        {
            if (_currentStates.Count != 0)
                _currentStates.Clear();

            if (_previousStates.Count != 0)
                _previousStates.Clear();

            _state = default;
            return;
        }

        MouseState mouse = Mouse.GetState();
        KeyboardState keyboard = Keyboard.GetState();
        GamepadState gamepad = Gamepad.GetState();

        Dictionary<ulong, bool> temp = _previousStates;

        _previousStates = _currentStates;
        _currentStates = temp;

        _currentStates.Clear();

        foreach ((ulong hash, ActionBinding action) in _actions)
        {
            _currentStates[hash] =
                action.Evaluate(
                    mouse,
                    keyboard,
                    gamepad);
        }

        if (!_state.Matches(
                _currentStates,
                _previousStates))
        {
            _state = new InputActionState(
                _currentStates,
                _previousStates);
        }
    }

    /// <summary>
    /// Gets the input action snapshot produced by the current game update.
    /// </summary>
    /// <remarks>
    /// This method only returns the current snapshot. It does not poll input devices or
    /// advance action transitions. Before the first action-system update, or after
    /// <see cref="Clear"/>, the default snapshot treats every action as released.
    /// </remarks>
    /// <returns>The current input action snapshot.</returns>
    public static InputActionState GetState() => _state;
}
