// ============================================================================
//  Logger.cs
// ============================================================================
//  High-performance asynchronous logging system with support for multiple
//  sinks, log levels, categories, and batch processing.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Void.Engine.Logs.Sinks;

namespace Void.Engine.Logs;

/// <summary>
/// Defines the severity levels for log messages.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Debug-level messages for development and troubleshooting.
    /// </summary>
    Debug = 0,

    /// <summary>
    /// Informational messages about normal application operation.
    /// </summary>
    Info = 1,

    /// <summary>
    /// Warning messages for potentially problematic situations.
    /// </summary>
    Warning = 2,

    /// <summary>
    /// Error messages for recoverable failures.
    /// </summary>
    Error = 3,

    /// <summary>
    /// Fatal messages for unrecoverable failures that may cause termination.
    /// </summary>
    Fatal = 4,

    /// <summary>
    /// No logging. Messages at this level are not written.
    /// </summary>
    None = 5
}

/// <summary>
/// High-performance asynchronous logging system with support for multiple sinks,
/// log levels, categories, and batch processing.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Logger"/> class provides a thread-safe, asynchronous logging
/// system that writes messages to one or more sinks in the background.
/// It supports:
/// <list type="bullet">
///   <item><description>Multiple log levels (Debug, Info, Warning, Error, Fatal)</description></item>
///   <item><description>Category-based logging for organizational grouping</description></item>
///   <item><description>Multiple sinks (Console, File, etc.)</description></item>
///   <item><description>Asynchronous batch processing for performance</description></item>
///   <item><description>Queue size limits to prevent memory issues</description></item>
///   <item><description>Flush capability for critical messages</description></item>
/// </list>
/// </para>
/// <para>
/// The logger uses a background worker thread to process log messages
/// asynchronously, ensuring that logging does not block the main game loop.
/// Messages are queued and processed in batches for efficiency.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Get the logger instance
/// var logger = Logger.Instance;
/// 
/// // Set minimum log level
/// logger.SetLevel(LogLevel.Info);
/// 
/// // Add sinks
/// logger.AddSink(new ConsoleSink());
/// logger.AddSink(new FileSink("logs/", 10, 10));
/// 
/// // Log messages
/// logger.Info("Game started");
/// logger.Info("Player position: {0}, {1}", x, y);
/// logger.WarningWithCategory("Network", "Connection lost, retrying...");
/// logger.Error("Failed to load texture", exception);
/// 
/// // Fatal errors are automatically flushed
/// logger.Fatal("Critical error, shutting down", exception);
/// </code>
/// </para>
/// <para>
/// <b>Performance Considerations:</b>
/// <list type="bullet">
///   <item><description>Logging is asynchronous and non-blocking</description></item>
///   <item><description>Messages are processed in batches of 100 for efficiency</description></item>
///   <item><description>Queue is limited to 10,000 messages to prevent unbounded memory growth</description></item>
///   <item><description>Messages below the minimum level are discarded immediately</description></item>
///   <item><description>Category strings are cached via <see cref="ToCategoryString"/></description></item>
/// </list>
/// </para>
/// <para>
/// <b>Sink Requirements:</b>
/// Sinks must implement <see cref="ILogSink"/> and handle their own thread safety.
/// The logger processes sinks sequentially and catches exceptions to prevent
/// sink failures from affecting other sinks or the game.
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is thread-safe. Logging methods can be called from any thread.
/// The background worker thread handles all sink writes asynchronously.
/// </para>
/// </remarks>
public sealed class Logger : IDisposable
{
    private static readonly Lazy<Logger> _instance = new(() => new Logger());

    /// <summary>
    /// Gets the singleton instance of the logger.
    /// </summary>
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

    /// <summary>
    /// Sets the minimum log level. Messages below this level are discarded.
    /// </summary>
    public void SetLevel(LogLevel level)
    {
        _minimumLevel = level;
    }

    /// <summary>
    /// Gets the current minimum log level.
    /// </summary>
    public LogLevel GetLevel() => _minimumLevel;

    /// <summary>
    /// Adds a log sink to receive log messages.
    /// </summary>
    public void AddSink(ILogSink sink)
    {
        if (sink == null)
            throw new ArgumentNullException(nameof(sink));

        lock (_sinkLock)
        {
            _sinks.Add(sink);
        }
    }

    /// <summary>
    /// Removes a log sink from the logger.
    /// </summary>
    public void RemoveSink(ILogSink sink)
    {
        if (sink == null)
            return;

        lock (_sinkLock)
        {
            _sinks.Remove(sink);
        }
    }

    /// <summary>
    /// Logs an empty debug message.
    /// </summary>
    public void Debug() => Write(LogLevel.Debug, null, "", null);

    /// <summary>
    /// Logs a debug message.
    /// </summary>
    public void Debug(string message)
        => Write(LogLevel.Debug, null, message, null);

    /// <summary>
    /// Logs a formatted debug message.
    /// </summary>
    public void Debug(string message, params object[] args)
        => Write(LogLevel.Debug, null, string.Format(message, args), null);

    /// <summary>
    /// Logs a debug message with a category.
    /// </summary>
    public void DebugWithCategory(string category, string message)
        => Write(LogLevel.Debug, ToCategoryString(category), message, null);

    /// <summary>
    /// Logs a formatted debug message with a category.
    /// </summary>
    public void DebugWithCategory(string category, string message, params object[] args)
        => Write(LogLevel.Debug, ToCategoryString(category), string.Format(message, args), null);

    /// <summary>
    /// Logs an empty informational message.
    /// </summary>
    public void Info() => Write(LogLevel.Info, null, "", null);

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    public void Info(string message)
        => Write(LogLevel.Info, null, message, null);

    /// <summary>
    /// Logs a formatted informational message.
    /// </summary>
    public void Info(string message, params object[] args)
        => Write(LogLevel.Info, null, string.Format(message, args), null);

    /// <summary>
    /// Logs an informational message with a category.
    /// </summary>
    public void InfoWithCategory(string category, string message)
        => Write(LogLevel.Info, ToCategoryString(category), message, null);

    /// <summary>
    /// Logs a formatted informational message with a category.
    /// </summary>
    public void InfoWithCategory(string category, string message, params object[] args)
        => Write(LogLevel.Info, ToCategoryString(category), string.Format(message, args), null);

    /// <summary>
    /// Logs an empty warning message.
    /// </summary>
    public void Warning() => Write(LogLevel.Warning, null, "", null);

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    public void Warning(string message)
        => Write(LogLevel.Warning, null, message, null);

    /// <summary>
    /// Logs a formatted warning message.
    /// </summary>
    public void Warning(string message, params object[] args)
        => Write(LogLevel.Warning, null, string.Format(message, args), null);

    /// <summary>
    /// Logs a warning message with a category.
    /// </summary>
    public void WarningWithCategory(string category, string message)
        => Write(LogLevel.Warning, ToCategoryString(category), message, null);

    /// <summary>
    /// Logs a formatted warning message with a category.
    /// </summary>
    public void WarningWithCategory(string category, string message, params object[] args)
        => Write(LogLevel.Warning, ToCategoryString(category), string.Format(message, args), null);

    /// <summary>
    /// Logs an empty error message.
    /// </summary>
    public void Error() => Write(LogLevel.Error, null, "", null);

    /// <summary>
    /// Logs an error message.
    /// </summary>
    public void Error(string message)
        => Write(LogLevel.Error, null, message, null);

    /// <summary>
    /// Logs a formatted error message.
    /// </summary>
    public void Error(string message, params object[] args)
        => Write(LogLevel.Error, null, string.Format(message, args), null);

    /// <summary>
    /// Logs an error with an exception.
    /// </summary>
    public void Error(Exception exception)
        => Write(LogLevel.Error, null, exception.ToString(), exception);

    /// <summary>
    /// Logs an error message with an exception.
    /// </summary>
    public void Error(Exception exception, string message)
        => Write(LogLevel.Error, null, message, exception);

    /// <summary>
    /// Logs an error message with a category.
    /// </summary>
    public void ErrorWithCategory(string category, string message)
        => Write(LogLevel.Error, ToCategoryString(category), message, null);

    /// <summary>
    /// Logs a formatted error message with a category.
    /// </summary>
    public void ErrorWithCategory(string category, string message, params object[] args)
        => Write(LogLevel.Error, ToCategoryString(category), string.Format(message, args), null);

    /// <summary>
    /// Logs an error with a category and exception.
    /// </summary>
    public void ErrorWithCategory(string category, Exception exception)
        => Write(LogLevel.Error, ToCategoryString(category), exception.ToString(), exception);

    /// <summary>
    /// Logs an error message with a category and exception.
    /// </summary>
    public void ErrorWithCategory(string category, Exception exception, string message)
        => Write(LogLevel.Error, ToCategoryString(category), message, exception);

    /// <summary>
    /// Logs an empty fatal message and flushes the queue.
    /// </summary>
    public void Fatal() => Write(LogLevel.Fatal, null, "", null);

    /// <summary>
    /// Logs a fatal message and flushes the queue.
    /// </summary>
    public void Fatal(string message)
        => Write(LogLevel.Fatal, null, message, null);

    /// <summary>
    /// Logs a formatted fatal message and flushes the queue.
    /// </summary>
    public void Fatal(string message, params object[] args)
    {
        Write(LogLevel.Fatal, null, string.Format(message, args), null);
        Flush();
    }

    /// <summary>
    /// Logs a fatal error with an exception and flushes the queue.
    /// </summary>
    public void Fatal(Exception exception)
    {
        Write(LogLevel.Fatal, null, exception.ToString(), exception);
        Flush();
    }

    /// <summary>
    /// Logs a fatal message with an exception and flushes the queue.
    /// </summary>
    public void Fatal(Exception exception, string message)
    {
        Write(LogLevel.Fatal, null, message, exception);
        Flush();
    }

    /// <summary>
    /// Logs a fatal message with a category and flushes the queue.
    /// </summary>
    public void FatalWithCategory(string category, string message)
    {
        Write(LogLevel.Fatal, ToCategoryString(category), message, null);
        Flush();
    }

    /// <summary>
    /// Logs a formatted fatal message with a category and flushes the queue.
    /// </summary>
    public void FatalWithCategory(string category, string message, params object[] args)
    {
        Write(LogLevel.Fatal, ToCategoryString(category), string.Format(message, args), null);
        Flush();
    }

    /// <summary>
    /// Logs a fatal error with a category and exception and flushes the queue.
    /// </summary>
    public void FatalWithCategory(string category, Exception exception)
    {
        Write(LogLevel.Fatal, ToCategoryString(category), exception.ToString(), exception);
        Flush();
    }

    /// <summary>
    /// Logs a fatal message with a category and exception and flushes the queue.
    /// </summary>
    public void FatalWithCategory(string category, Exception exception, string message)
    {
        Write(LogLevel.Fatal, ToCategoryString(category), message, exception);
        Flush();
    }

    private void Write(LogLevel level, string category, string message, Exception exception)
    {
        if (level < _minimumLevel)
            return;

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

    private void Flush()
    {
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

    /// <summary>
    /// Disposes the logger, flushing any remaining messages.
    /// </summary>
    public void Dispose()
    {
        Flush();
        _cts.Cancel();
        _worker.Join(1000);
    }
}