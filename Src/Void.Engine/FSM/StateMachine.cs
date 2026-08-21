// ============================================================================
//  StateMachine.cs
// ============================================================================
//  A fluent, coroutine-based finite state machine with support for state
//  transitions, history, and nested coroutines.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections;
using System.Collections.Generic;

namespace Void.Engine.FSM;

/// <summary>
/// A fluent, coroutine-based finite state machine with support for state
/// transitions, history, and nested coroutines.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="StateMachine"/> class provides a flexible state machine
/// implementation where states are registered as coroutine factories.
/// States can yield strings to transition to other states, or yield nested
/// <see cref="IEnumerator"/> objects for complex sequencing.
/// </para>
/// <para>
/// <b>Key Features:</b>
/// <list type="bullet">
///   <item><description>Fluent API for state registration and configuration</description></item>
///   <item><description>Coroutine-based state execution</description></item>
///   <item><description>String-based state transitions</description></item>
///   <item><description>State history with back navigation</description></item>
///   <item><description>Pause and resume support</description></item>
///   <item><description>Enter, exit, and changed callbacks</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// var fsm = new StateMachine();
/// 
/// // Register states
/// fsm.AddState("Idle", () => IdleState());
/// fsm.AddState("Walk", () => WalkState());
/// fsm.AddState("Jump", () => JumpState());
/// 
/// // Set callbacks
/// fsm.OnEnter(state => Console.WriteLine($"Entering {state}"));
/// fsm.OnExit(state => Console.WriteLine($"Exiting {state}"));
/// fsm.OnChanged((from, to) => Console.WriteLine($"{from} -> {to}"));
/// 
/// // Start the state machine
/// fsm.ChangeState("Idle");
/// 
/// // In the update loop
/// fsm.Update(frameTime);
/// 
/// // Pause and resume
/// fsm.Pause();
/// fsm.Resume();
/// 
/// // Go back to previous state
/// fsm.GoBack();
/// 
/// // Clean up
/// fsm.Dispose();
/// 
/// // State coroutine examples
/// IEnumerator IdleState()
/// {
///     while (true)
///     {
///         // Do idle behavior
///         yield return null;
///         
///         if (Input.IsPressed("Jump"))
///             yield return "Jump"; // Transition to Jump state
///     }
/// }
/// 
/// IEnumerator WalkState()
/// {
///     // Walk for 2 seconds then transition
///     yield return new WaitForSeconds(2f);
///     yield return "Idle";
/// }
/// </code>
/// </para>
/// <para>
/// <b>State Transitions:</b>
/// States can transition by yielding a string that matches a registered
/// state name. The state machine handles the transition cleanly by exiting
/// the current state, entering the new state, and firing the appropriate
/// callbacks.
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is not thread-safe. All operations should be performed from
/// the main thread.
/// </para>
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
    /// Gets the current frame time for this state machine.
    /// </summary>
    public FrameTime FrameTime => _frameTime;

    /// <summary>
    /// Gets the name of the current state.
    /// </summary>
    public string CurrentState => _currentStateName;

    /// <summary>
    /// Gets the name of the previous state.
    /// </summary>
    public string PreviousState => _previousStateName;

    /// <summary>
    /// Gets whether the state machine is currently running.
    /// </summary>
    public bool IsRunning => _running;

    /// <summary>
    /// Gets whether the state machine is paused.
    /// </summary>
    public bool IsPaused => _paused;

    /// <summary>
    /// Gets whether the state machine has been disposed.
    /// </summary>
    public bool IsDisposed => _disposed;

    /// <summary>
    /// Gets all registered state names.
    /// </summary>
    public IReadOnlyCollection<string> States => _stateFactories.Keys;

    /// <summary>
    /// Called when a state is entered.
    /// </summary>
    public Action<string> OnStateEnter { get; set; }

    /// <summary>
    /// Called when a state is exited.
    /// </summary>
    public Action<string> OnStateExit { get; set; }

    /// <summary>
    /// Called after a state transition completes.
    /// </summary>
    public Action<string, string> OnStateChanged { get; set; }

    /// <summary>
    /// Registers a new state with the state machine.
    /// </summary>
    /// <param name="name">The unique name of the state.</param>
    /// <param name="stateFactory">A factory that creates a new <see cref="IEnumerator"/> for the state.</param>
    /// <returns>This <see cref="StateMachine"/> instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> or <paramref name="stateFactory"/> is null.</exception>
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
    /// Sets the state enter callback.
    /// </summary>
    /// <param name="callback">The callback to invoke when a state is entered.</param>
    /// <returns>This <see cref="StateMachine"/> instance for method chaining.</returns>
    public StateMachine OnEnter(Action<string> callback)
    {
        ThrowIfDisposed();
        OnStateEnter = callback;
        return this;
    }

    /// <summary>
    /// Sets the state exit callback.
    /// </summary>
    /// <param name="callback">The callback to invoke when a state is exited.</param>
    /// <returns>This <see cref="StateMachine"/> instance for method chaining.</returns>
    public StateMachine OnExit(Action<string> callback)
    {
        ThrowIfDisposed();
        OnStateExit = callback;
        return this;
    }

    /// <summary>
    /// Sets the state changed callback.
    /// </summary>
    /// <param name="callback">The callback to invoke when a state transition occurs.</param>
    /// <returns>This <see cref="StateMachine"/> instance for method chaining.</returns>
    public StateMachine OnChanged(Action<string, string> callback)
    {
        ThrowIfDisposed();
        OnStateChanged = callback;
        return this;
    }

    /// <summary>
    /// Transitions to a new state.
    /// </summary>
    /// <param name="name">The name of the state to transition to.</param>
    /// <returns>This <see cref="StateMachine"/> instance for method chaining.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the state is not registered.</exception>
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
    /// Forces a state change even if already in that state.
    /// </summary>
    /// <param name="name">The name of the state to transition to.</param>
    /// <returns>This <see cref="StateMachine"/> instance for method chaining.</returns>
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
    /// Returns to the previous state in the history.
    /// </summary>
    /// <returns>This <see cref="StateMachine"/> instance for method chaining.</returns>
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
    /// Restarts the current state.
    /// </summary>
    /// <returns>This <see cref="StateMachine"/> instance for method chaining.</returns>
    public StateMachine Restart()
    {
        ThrowIfDisposed();

        if (_currentStateName != null)
            ChangeState(_currentStateName);
        return this;
    }

    /// <summary>
    /// Pauses the state machine. Update calls will be ignored while paused.
    /// </summary>
    /// <returns>This <see cref="StateMachine"/> instance for method chaining.</returns>
    public StateMachine Pause()
    {
        ThrowIfDisposed();
        _paused = true;
        return this;
    }

    /// <summary>
    /// Resumes a paused state machine.
    /// </summary>
    /// <returns>This <see cref="StateMachine"/> instance for method chaining.</returns>
    public StateMachine Resume()
    {
        ThrowIfDisposed();
        _paused = false;
        return this;
    }

    /// <summary>
    /// Stops the state machine entirely.
    /// </summary>
    /// <returns>This <see cref="StateMachine"/> instance for method chaining.</returns>
    public StateMachine Stop()
    {
        ThrowIfDisposed();
        ExitCurrentState();
        _running = false;
        return this;
    }

    /// <summary>
    /// Advances the state machine by one frame.
    /// </summary>
    /// <param name="frameTime">The current frame time information.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="frameTime"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a state yields a nested <see cref="StateMachine"/>.</exception>
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
    /// Checks if the state machine is currently in the specified state.
    /// </summary>
    /// <param name="name">The name of the state to check.</param>
    /// <returns><see langword="true"/> if the state machine is in the specified state; otherwise, <see langword="false"/>.</returns>
    public bool IsInState(string name)
    {
        ThrowIfDisposed();
        return string.Equals(_currentStateName, name, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Clears the state history.
    /// </summary>
    /// <returns>This <see cref="StateMachine"/> instance for method chaining.</returns>
    public StateMachine ClearHistory()
    {
        ThrowIfDisposed();
        _history.Clear();
        return this;
    }

    /// <summary>
    /// Stops the state machine and releases all resources.
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