using Void.Engine.Logs.Sinks;

namespace Void.Engine.Logs;

public enum LogLevel
{
    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3,
    Fatal = 4,
    None = 5
}

public sealed class Logger : IDisposable
{
    private static readonly Lazy<Logger> _instance = new(() => new Logger());
    public static Logger Instance => _instance.Value;

    private readonly ConcurrentQueue<LogEntry> _queue = [];
    private readonly List<ILogSink> _sinks = [];
    private readonly Lock _sinkLock = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Thread _worker;
    private LogLevel _minimumLevel = LogLevel.Debug;
    private const int MaxQueueSize = 10000;
    private const int BatchSize = 100;

    private Logger()
    {
        _worker = new Thread(ProcessQueue)
        {
            IsBackground = true,
            Name = "LogWriter"
        };
        _worker.Start();
    }

    public void SetLevel(LogLevel level)
    {
        _minimumLevel = level;
    }

    public LogLevel GetLevel() => _minimumLevel;

    public void AddSink(ILogSink sink)
    {
        if (sink == null)
            throw new ArgumentNullException(nameof(sink));

        lock (_sinkLock)
        {
            _sinks.Add(sink);
        }
    }

    public void RemoveSink(ILogSink sink)
    {
        if (sink == null)
            return;

        lock (_sinkLock)
        {
            _sinks.Remove(sink);
        }
    }

    public void Debug() => Write(LogLevel.Debug, null, "", null);

    public void Debug(string message)
        => Write(LogLevel.Debug, null, message, null);

    public void Debug(string message, params object[] args)
        => Write(LogLevel.Debug, null, string.Format(message, args), null);

    public void DebugWithCategory(string category, string message)
        => Write(LogLevel.Debug, ToCategoryString(category), message, null);

    public void DebugWithCategory(string category, string message, params object[] args)
        => Write(LogLevel.Debug, ToCategoryString(category), string.Format(message, args), null);

    public void Info() => Write(LogLevel.Info, null, "", null);

    public void Info(string message)
        => Write(LogLevel.Info, null, message, null);

    public void Info(string message, params object[] args)
        => Write(LogLevel.Info, null, string.Format(message, args), null);

    public void InfoWithCategory(string category, string message)
        => Write(LogLevel.Info, ToCategoryString(category), message, null);

    public void InfoWithCategory(string category, string message, params object[] args)
        => Write(LogLevel.Info, ToCategoryString(category), string.Format(message, args), null);

    public void Warning() => Write(LogLevel.Warning, null, "", null);

    public void Warning(string message)
        => Write(LogLevel.Warning, null, message, null);

    public void Warning(string message, params object[] args)
        => Write(LogLevel.Warning, null, string.Format(message, args), null);

    public void WarningWithCategory(string category, string message)
        => Write(LogLevel.Warning, ToCategoryString(category), message, null);

    public void WarningWithCategory(string category, string message, params object[] args)
        => Write(LogLevel.Warning, ToCategoryString(category), string.Format(message, args), null);

    public void Error() => Write(LogLevel.Error, null, "", null);

    public void Error(string message)
        => Write(LogLevel.Error, null, message, null);

    public void Error(string message, params object[] args)
        => Write(LogLevel.Error, null, string.Format(message, args), null);

    public void Error(Exception exception)
        => Write(LogLevel.Error, null, exception.ToString(), exception);

    public void Error(Exception exception, string message)
        => Write(LogLevel.Error, null, message, exception);

    public void ErrorWithCategory(string category, string message)
        => Write(LogLevel.Error, ToCategoryString(category), message, null);

    public void ErrorWithCategory(string category, string message, params object[] args)
        => Write(LogLevel.Error, ToCategoryString(category), string.Format(message, args), null);

    public void ErrorWithCategory(string category, Exception exception)
        => Write(LogLevel.Error, ToCategoryString(category), exception.ToString(), exception);

    public void ErrorWithCategory(string category, Exception exception, string message)
        => Write(LogLevel.Error, ToCategoryString(category), message, exception);

    public void Fatal() => Write(LogLevel.Fatal, null, "", null);

    public void Fatal(string message)
        => Write(LogLevel.Fatal, null, message, null);

    public void Fatal(string message, params object[] args)
    {
        Write(LogLevel.Fatal, null, string.Format(message, args), null);
        Flush(); // Ensure fatal errors are written before process exits
    }

    public void Fatal(Exception exception)
    {
        Write(LogLevel.Fatal, null, exception.ToString(), exception);
        Flush(); // Ensure fatal errors are written before process exits
    }

    public void Fatal(Exception exception, string message)
    {
        Write(LogLevel.Fatal, null, message, exception);
        Flush(); // Ensure fatal errors are written before process exits
    }

    public void FatalWithCategory(string category, string message)
    {
        Write(LogLevel.Fatal, ToCategoryString(category), message, null);
        Flush(); // Ensure fatal errors are written before process exits
    }

    public void FatalWithCategory(string category, string message, params object[] args)
    {
        Write(LogLevel.Fatal, ToCategoryString(category), string.Format(message, args), null);
        Flush(); // Ensure fatal errors are written before process exits
    }

    public void FatalWithCategory(string category, Exception exception)
    {
        Write(LogLevel.Fatal, ToCategoryString(category), exception.ToString(), exception);
        Flush(); // Ensure fatal errors are written before process exits
    }

    public void FatalWithCategory(string category, Exception exception, string message)
    {
        Write(LogLevel.Fatal, ToCategoryString(category), message, exception);
        Flush(); // Ensure fatal errors are written before process exits
    }

    private void Write(LogLevel level, string category, string message, Exception exception)
    {
        if (level < _minimumLevel)
            return;

        // Fix 2: Queue size limit to prevent unbounded memory growth
        if (_queue.Count >= MaxQueueSize)
            return;

        _queue.Enqueue(new LogEntry
        {
            Level = level,
            Timestamp = DateTime.Now,
            Category = category,
            Message = message,
            Exception = exception
        });
    }

    private void ProcessQueue()
    {
        while (!_cts.IsCancellationRequested)
        {
            // Fix 3: Batch processing for better throughput
            var batch = new List<LogEntry>(BatchSize);

            while (batch.Count < BatchSize && _queue.TryDequeue(out var entry))
            {
                batch.Add(entry);
            }

            if (batch.Count > 0)
            {
                lock (_sinkLock)
                {
                    foreach (var sink in _sinks)
                    {
                        foreach (var entry in batch)
                        {
                            try
                            {
                                sink.Write(entry);
                            }
                            catch
                            {
                                // Sink failure shouldn't crash the game
                            }
                        }
                    }
                }
            }
            else
            {
                Thread.Sleep(1);
            }
        }
    }

    // Fix 1: Flush method for fatal errors
    private void Flush()
    {
        // Process remaining queue synchronously
        var batch = new List<LogEntry>();

        while (_queue.TryDequeue(out var entry))
        {
            batch.Add(entry);
        }

        if (batch.Count > 0)
        {
            lock (_sinkLock)
            {
                foreach (var sink in _sinks)
                {
                    foreach (var entry in batch)
                    {
                        try
                        {
                            sink.Write(entry);
                        }
                        catch
                        {
                            // Sink failure shouldn't crash the game
                        }
                    }
                }
            }
        }
    }

    private static string ToCategoryString(object category)
    {
        return category is Enum e
            ? $"{e.GetType().FullName}.{e}"
            : category?.ToString() ?? "";
    }

    public void Dispose()
    {
        Flush(); // Final flush on dispose
        _cts.Cancel();
        _worker.Join(1000);
    }
}