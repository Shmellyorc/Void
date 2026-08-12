namespace Void.Engine.FSM;

/// <summary>
/// A fluent, coroutine-based finite state machine.
/// States are registered as coroutine factories and can yield strings to transition
/// or yield nested IEnumerators for complex sequencing.
/// </summary>
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
    /// Registers a new state.
    /// </summary>
    /// <param name="name">The unique name of the state.</param>
    /// <param name="stateFactory">A factory that creates a new IEnumerator for the state.</param>
    /// <returns>This <see cref="StateMachine"/> for chaining.</returns>
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
    public StateMachine OnEnter(Action<string> callback)
    {
        ThrowIfDisposed();
        OnStateEnter = callback;
        return this;
    }

    /// <summary>
    /// Sets the state exit callback.
    /// </summary>
    public StateMachine OnExit(Action<string> callback)
    {
        ThrowIfDisposed();
        OnStateExit = callback;
        return this;
    }

    /// <summary>
    /// Sets the state changed callback.
    /// </summary>
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
    /// <returns>This <see cref="StateMachine"/> for chaining.</returns>
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
    /// Returns to the previous state.
    /// </summary>
    /// <returns>This <see cref="StateMachine"/> for chaining.</returns>
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
    /// <returns>This <see cref="StateMachine"/> for chaining.</returns>
    public StateMachine Restart()
    {
        ThrowIfDisposed();

        if (_currentStateName != null)
            ChangeState(_currentStateName);
        return this;
    }

    /// <summary>
    /// Pauses the current state. Update calls will be ignored while paused.
    /// </summary>
    /// <returns>This <see cref="StateMachine"/> for chaining.</returns>
    public StateMachine Pause()
    {
        ThrowIfDisposed();
        _paused = true;
        return this;
    }

    /// <summary>
    /// Resumes a paused state machine.
    /// </summary>
    /// <returns>This <see cref="StateMachine"/> for chaining.</returns>
    public StateMachine Resume()
    {
        ThrowIfDisposed();
        _paused = false;
        return this;
    }

    /// <summary>
    /// Stops the state machine entirely.
    /// </summary>
    /// <returns>This <see cref="StateMachine"/> for chaining.</returns>
    public StateMachine Stop()
    {
        ThrowIfDisposed();
        ExitCurrentState();
        _running = false;
        return this;
    }

    /// <summary>
    /// Advances the state machine by one frame.
    /// Supports nested routines yielded by states.
    /// </summary>
    /// <param name="frameTime">The current frame time information.</param>
    public void Update(FrameTime frameTime)
    {
        ThrowIfDisposed();

        _frameTime = frameTime ?? throw new ArgumentNullException(nameof(frameTime));

        if (!_running || _paused || _currentState == null)
            return;

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
    public bool IsInState(string name)
    {
        ThrowIfDisposed();
        return string.Equals(_currentStateName, name, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Clears the state history.
    /// </summary>
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

        // Clear callbacks to prevent memory leaks
        OnStateEnter = null;
        OnStateExit = null;
        OnStateChanged = null;

        // Clear collections
        _history.Clear();
        _stateFactories.Clear();

        // Clear references
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