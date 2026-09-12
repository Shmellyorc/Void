// ============================================================================
//  StateMachine.cs
// ============================================================================
//  Coroutine-based finite state machine with fluent state registration,
//  transitions, history, and lifecycle callbacks.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;
using System.Collections.Generic;

namespace Void.Engine.FSM;

/// <summary>
/// Provides a coroutine-based finite state machine with named transitions,
/// history navigation, pause control, and lifecycle callbacks.
/// </summary>
/// <remarks>
/// <para>
/// States are registered as <see cref="IEnumerator"/> factories. State names are
/// matched case-insensitively. A state can request a transition by yielding the
/// name of another registered state.
/// </para>
/// <para>
/// A state may also yield an <see cref="IEnumerator"/>. The state machine advances
/// that direct child until it completes before resuming the state. The child is
/// advanced directly rather than through <c>CoroutineManager</c>, so values yielded
/// by that child are not recursively interpreted by this state machine.
/// </para>
/// <para>
/// When a state finishes naturally, <see cref="IsRunning"/> becomes
/// <see langword="false"/>. The state exit callback is invoked only when the
/// current state is explicitly exited by a state change, <see cref="Stop"/>, or
/// <see cref="Dispose"/>.
/// </para>
/// <code>
/// var machine = new StateMachine()
///     .AddState("Idle", Idle)
///     .AddState("Active", Active)
///     .OnChanged((from, to) =&gt; Console.WriteLine($"{from} -&gt; {to}"));
///
/// machine.ChangeState("Idle");
/// machine.Update(frameTime);
///
/// IEnumerator Idle()
/// {
///     yield return new WaitForSeconds(0.5f);
///     yield return "Active";
/// }
///
/// IEnumerator Active()
/// {
///     while (true)
///         yield return null;
/// }
/// </code>
/// </remarks>
public sealed class StateMachine : IDisposable
{
    private readonly Dictionary<string, Func<IEnumerator>> _stateFactories = new(StringComparer.OrdinalIgnoreCase);
    private readonly Stack<string> _history = new();
    private IEnumerator _currentState;
    private string _currentStateName;
    private string _previousStateName;
    private FrameTime _frameTime;
    private bool _running;
    private bool _paused;
    private bool _disposed;

    /// <summary>
    /// Gets the most recent frame timing data supplied to <see cref="Update(FrameTime)"/>.
    /// </summary>
    /// <remarks>
    /// This value is not initialized until <see cref="Update(FrameTime)"/> has been called.
    /// </remarks>
    public FrameTime FrameTime => _frameTime;

    /// <summary>
    /// Gets the name of the currently selected state.
    /// </summary>
    /// <remarks>
    /// The name remains available after the state stops or completes naturally.
    /// It is not initialized until the first successful state change.
    /// </remarks>
    public string CurrentState => _currentStateName;

    /// <summary>
    /// Gets the state name recorded immediately before the most recent state change.
    /// </summary>
    /// <remarks>
    /// This value is not initialized until a state change records a previous state.
    /// </remarks>
    public string PreviousState => _previousStateName;

    /// <summary>
    /// Gets a value indicating whether the current state is still running.
    /// </summary>
    public bool IsRunning => _running;

    /// <summary>
    /// Gets a value indicating whether updates to the current state are paused.
    /// </summary>
    public bool IsPaused => _paused;

    /// <summary>
    /// Gets a value indicating whether this state machine has been disposed.
    /// </summary>
    public bool IsDisposed => _disposed;

    /// <summary>
    /// Gets the registered state names.
    /// </summary>
    /// <remarks>
    /// The returned collection reflects subsequent registrations made by this state machine.
    /// </remarks>
    public IReadOnlyCollection<string> States => _stateFactories.Keys;

    /// <summary>
    /// Gets or sets the callback invoked after a state becomes current.
    /// </summary>
    public Action<string> OnStateEnter { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the current state is explicitly exited.
    /// </summary>
    /// <remarks>
    /// Natural completion of a state does not invoke this callback by itself.
    /// </remarks>
    public Action<string> OnStateExit { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked after <see cref="ChangeState(string)"/>
    /// completes a transition.
    /// </summary>
    /// <remarks>
    /// Restarting the already-current state through <see cref="ForceChangeState(string)"/>
    /// does not invoke this callback.
    /// </remarks>
    public Action<string, string> OnStateChanged { get; set; }

    /// <summary>
    /// Registers or replaces a named state factory.
    /// </summary>
    /// <param name="name">The state name. State names are matched case-insensitively.</param>
    /// <param name="stateFactory">A factory that creates a new state enumerator when the state is entered.</param>
    /// <returns>This state machine for chaining.</returns>
    /// <exception cref="ObjectDisposedException">The state machine has been disposed.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null or empty, or <paramref name="stateFactory"/> is null.</exception>
    public StateMachine AddState(string name, Func<IEnumerator> stateFactory)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));
        if (stateFactory == null)
            throw new ArgumentNullException(nameof(stateFactory));

        _stateFactories[name] = stateFactory;
        return this;
    }

    /// <summary>
    /// Sets the state-enter callback.
    /// </summary>
    /// <param name="callback">The callback to assign.</param>
    /// <returns>This state machine for chaining.</returns>
    /// <exception cref="ObjectDisposedException">The state machine has been disposed.</exception>
    public StateMachine OnEnter(Action<string> callback)
    {
        ThrowIfDisposed();
        OnStateEnter = callback;
        return this;
    }

    /// <summary>
    /// Sets the state-exit callback.
    /// </summary>
    /// <param name="callback">The callback to assign.</param>
    /// <returns>This state machine for chaining.</returns>
    /// <exception cref="ObjectDisposedException">The state machine has been disposed.</exception>
    public StateMachine OnExit(Action<string> callback)
    {
        ThrowIfDisposed();
        OnStateExit = callback;
        return this;
    }

    /// <summary>
    /// Sets the state-changed callback.
    /// </summary>
    /// <param name="callback">The callback to assign. Its arguments are the previous and new state names.</param>
    /// <returns>This state machine for chaining.</returns>
    /// <exception cref="ObjectDisposedException">The state machine has been disposed.</exception>
    public StateMachine OnChanged(Action<string, string> callback)
    {
        ThrowIfDisposed();
        OnStateChanged = callback;
        return this;
    }

    /// <summary>
    /// Exits the current state and enters the specified registered state.
    /// </summary>
    /// <param name="name">The state name to enter.</param>
    /// <returns>This state machine for chaining.</returns>
    /// <remarks>
    /// The destination is pushed onto the history stack. Calling this method with
    /// the already-current state still performs a full transition and records that
    /// state again in history.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The state machine has been disposed.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="KeyNotFoundException">No state is registered with the specified name.</exception>
    public StateMachine ChangeState(string name)
    {
        ThrowIfDisposed();

        if (!_stateFactories.ContainsKey(name))
            throw new KeyNotFoundException($"State '{name}' is not registered.");

        ExitCurrentState();

        _previousStateName = _currentStateName;
        _currentStateName = name;
        _history.Push(name);

        _currentState = _stateFactories[name]();
        _running = true;
        _paused = false;

        OnStateEnter?.Invoke(name);
        OnStateChanged?.Invoke(_previousStateName, name);

        return this;
    }

    /// <summary>
    /// Re-enters the specified state, with special handling when it is already current.
    /// </summary>
    /// <param name="name">The registered state name to enter.</param>
    /// <returns>This state machine for chaining.</returns>
    /// <remarks>
    /// When <paramref name="name"/> is already the current state, the current
    /// enumerator is exited and recreated without changing history, updating
    /// <see cref="PreviousState"/>, or invoking <see cref="OnStateChanged"/>.
    /// For a different state, this method delegates to <see cref="ChangeState(string)"/>.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The state machine has been disposed.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="KeyNotFoundException">No state is registered with the specified name.</exception>
    public StateMachine ForceChangeState(string name)
    {
        ThrowIfDisposed();

        if (_currentStateName == name)
        {
            ExitCurrentState();
            _currentState = _stateFactories[name]();
            _running = true;
            _paused = false;
            OnStateEnter?.Invoke(name);
            return this;
        }

        return ChangeState(name);
    }

    /// <summary>
    /// Returns to the previous state stored in history, if one is available.
    /// </summary>
    /// <returns>This state machine for chaining.</returns>
    /// <remarks>
    /// If history contains fewer than two entries, this method has no effect.
    /// The restored state becomes the newest history entry again.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The state machine has been disposed.</exception>
    public StateMachine GoBack()
    {
        ThrowIfDisposed();

        if (_history.Count > 1)
        {
            _history.Pop(); // Remove current
            string previous = _history.Pop();
            ChangeState(previous);
        }
        return this;
    }

    /// <summary>
    /// Restarts the current state through a normal state change.
    /// </summary>
    /// <returns>This state machine for chaining.</returns>
    /// <remarks>
    /// Restarting records the current state again in history and invokes the normal
    /// exit, enter, and changed callbacks. If no state has been selected, this method
    /// has no effect.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The state machine has been disposed.</exception>
    public StateMachine Restart()
    {
        ThrowIfDisposed();

        if (_currentStateName != null)
            ChangeState(_currentStateName);
        return this;
    }

    /// <summary>
    /// Pauses advancement of the current state.
    /// </summary>
    /// <returns>This state machine for chaining.</returns>
    /// <remarks>
    /// <see cref="Update(FrameTime)"/> still records its supplied frame timing data while paused.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The state machine has been disposed.</exception>
    public StateMachine Pause()
    {
        ThrowIfDisposed();
        _paused = true;
        return this;
    }

    /// <summary>
    /// Resumes advancement after a pause.
    /// </summary>
    /// <returns>This state machine for chaining.</returns>
    /// <exception cref="ObjectDisposedException">The state machine has been disposed.</exception>
    public StateMachine Resume()
    {
        ThrowIfDisposed();
        _paused = false;
        return this;
    }

    /// <summary>
    /// Exits the current state and marks the state machine as not running.
    /// </summary>
    /// <returns>This state machine for chaining.</returns>
    /// <remarks>
    /// The selected state name and history are retained.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The state machine has been disposed.</exception>
    public StateMachine Stop()
    {
        ThrowIfDisposed();
        ExitCurrentState();
        _running = false;
        return this;
    }

    /// <summary>
    /// Updates frame timing data and advances the active state when running and not paused.
    /// </summary>
    /// <param name="frameTime">The frame timing data for this update.</param>
    /// <remarks>
    /// A yielded string transitions only when it matches a registered state name.
    /// Other yielded values are not interpreted. A directly yielded
    /// <see cref="IEnumerator"/> is advanced until it completes before the parent state resumes.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The state machine has been disposed.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="frameTime"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The current state yields another <see cref="StateMachine"/>.</exception>
    public void Update(FrameTime frameTime)
    {
        ThrowIfDisposed();

        _frameTime = frameTime ?? throw new ArgumentNullException(nameof(frameTime));

        if (!_running || _paused || _currentState == null)
            return;

        if (_currentState.Current is StateMachine)
            throw new InvalidOperationException(
                $"State '{_currentStateName}' yielded a StateMachine. " +
                "Nested StateMachines are not supported. " +
                "Use a single StateMachine with well-defined states instead.");

        if (_currentState.Current is IEnumerator nested && nested != _currentState)
        {
            if (nested.MoveNext())
                return;

            (nested as IDisposable)?.Dispose();
        }
        else if (_currentState.Current is string transition && _stateFactories.ContainsKey(transition))
        {
            ChangeState(transition);
            return;
        }

        if (!_currentState.MoveNext())
        {
            _running = false;
        }
    }

    /// <summary>
    /// Determines whether the selected state has the specified name.
    /// </summary>
    /// <param name="name">The state name to compare, using case-insensitive ordinal comparison.</param>
    /// <returns><see langword="true"/> when the selected state name matches; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// This checks the selected state name, which remains set even when the state machine is not running.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The state machine has been disposed.</exception>
    public bool IsInState(string name)
    {
        ThrowIfDisposed();
        return string.Equals(_currentStateName, name, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Removes all entries from the state history.
    /// </summary>
    /// <returns>This state machine for chaining.</returns>
    /// <remarks>
    /// The current state is not changed. After clearing, <see cref="GoBack"/> has no
    /// effect until later transitions rebuild history.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The state machine has been disposed.</exception>
    public StateMachine ClearHistory()
    {
        ThrowIfDisposed();
        _history.Clear();
        return this;
    }

    /// <summary>
    /// Stops the current state, clears callbacks and registrations, and marks this instance as disposed.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        Stop();

        OnStateEnter = null;
        OnStateExit = null;
        OnStateChanged = null;

        _history.Clear();
        _stateFactories.Clear();

        _frameTime = null;
        _currentStateName = null;
        _previousStateName = null;

        _disposed = true;
    }

    private void ExitCurrentState()
    {
        if (_currentState != null)
        {
            (_currentState as IDisposable)?.Dispose();
            OnStateExit?.Invoke(_currentStateName);
        }
        _currentState = null;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(StateMachine));
    }
}
