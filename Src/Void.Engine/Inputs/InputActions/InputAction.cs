// ============================================================================
//  InputAction.cs
// ============================================================================
//  Static manager for named input actions with string and enum support,
//  providing zero-GC snapshot-based state tracking.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Generic;
using Void.Engine.Inputs.Gamepads;
using Void.Engine.Inputs.Keyboards;
using Void.Engine.Inputs.Mouses;

namespace Void.Engine.Inputs.InputActions;

/// <summary>
/// Static manager for named input actions with string and enum support,
/// providing zero-GC snapshot-based state tracking.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="InputAction"/> class manages a collection of named input
/// actions that can be bound to keyboard keys, mouse buttons, and gamepad
/// buttons. It provides a snapshot-based state system that captures the
/// state of all actions in a single frame, eliminating garbage collection
/// and ensuring consistent input handling.
/// </para>
/// <para>
/// <b>Key Features:</b>
/// <list type="bullet">
///   <item><description>Named actions with string or enum identifiers</description></item>
///   <item><description>Multiple bindings per action (keyboard, mouse, gamepad)</description></item>
///   <item><description>Zero-GC snapshot-based state tracking</description></item>
///   <item><description>Pressed, Held, Released, and Up state detection</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Define actions during initialization
/// InputAction.AddAction("Jump")
///     .AddKey(KeyboardKey.Space)
///     .AddGamepad(GamepadButton.A);
/// 
/// InputAction.AddAction("MoveLeft")
///     .AddKey(KeyboardKey.Left)
///     .AddKey(KeyboardKey.A)
///     .AddGamepad(GamepadButton.LeftStickLeft);
/// 
/// // In the game loop, get the state snapshot
/// var state = InputAction.GetState();
/// 
/// // Query action states
/// if (state.IsPressed("Jump"))
///     HandleJump();
/// 
/// if (state.IsHeld("MoveLeft"))
///     MoveLeft();
/// 
/// if (state.IsReleased("Pause"))
///     TogglePause();
/// </code>
/// </para>
/// <para>
/// <b>State Transitions:</b>
/// <list type="bullet">
///   <item><description><see cref="ActionState.Pressed"/> - Action was just pressed this frame</description></item>
///   <item><description><see cref="ActionState.Held"/> - Action is being held down</description></item>
///   <item><description><see cref="ActionState.Released"/> - Action was just released this frame</description></item>
///   <item><description><see cref="ActionState.Up"/> - Action is not active</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe. All operations should be performed on
/// the main thread.
/// </para>
/// </remarks>
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
    /// <param name="name">The name of the action.</param>
    /// <returns>The existing or newly created <see cref="ActionBinding"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null or empty.</exception>
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
    /// <param name="name">The enum representing the action name.</param>
    /// <returns>The existing or newly created <see cref="ActionBinding"/>.</returns>
    public static ActionBinding AddAction(Enum name)
        => AddAction(name.ToEnumString());

    /// <summary>
    /// Gets an action by string name.
    /// </summary>
    /// <param name="name">The name of the action.</param>
    /// <returns>The <see cref="ActionBinding"/>, or <see langword="null"/> if not found.</returns>
    public static ActionBinding GetAction(string name)
        => _actions.TryGetValue(name, out var action) ? action : null;

    /// <summary>
    /// Gets an action by enum.
    /// </summary>
    /// <param name="name">The enum representing the action name.</param>
    /// <returns>The <see cref="ActionBinding"/>, or <see langword="null"/> if not found.</returns>
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
    /// <returns><see langword="true"/> if the action was removed; otherwise, <see langword="false"/>.</returns>
    public static bool RemoveAction(string name)
    {
        _previousStates.Remove(name);
        _currentStates.Remove(name);
        return _actions.Remove(name);
    }

    /// <summary>
    /// Removes an action by enum.
    /// </summary>
    /// <returns><see langword="true"/> if the action was removed; otherwise, <see langword="false"/>.</returns>
    public static bool RemoveAction(Enum name) => RemoveAction(name.ToEnumString());

    /// <summary>
    /// Clears all actions and states. Call on engine shutdown.
    /// </summary>
    public static void Clear()
    {
        _actions.Clear();
        _currentStates.Clear();
        _previousStates.Clear();
    }

    /// <summary>
    /// Gets a snapshot of all action states.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method reads input directly from the input systems and evaluates
    /// all registered actions. It reuses internal dictionaries to achieve
    /// zero allocation and garbage-free operation.
    /// </para>
    /// <para>
    /// The returned <see cref="InputActionState"/> provides a consistent
    /// snapshot of all action states for the current frame.
    /// </para>
    /// </remarks>
    /// <returns>An <see cref="InputActionState"/> containing all action states.</returns>
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