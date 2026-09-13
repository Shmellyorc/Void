// ============================================================================
//  Logger.cs
// ============================================================================
//  Asynchronous logging with levels, categories, formatted messages, and sinks.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

using Void.Engine.Logs.Sinks;

namespace Void.Engine.Logs;

/// <summary>
/// Defines the severity of a log entry.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Detailed diagnostic information intended for development and troubleshooting.
    /// </summary>
    Debug = 0,

    /// <summary>
    /// General information about normal application operation.
    /// </summary>
    Info = 1,

    /// <summary>
    /// A potentially problematic condition that does not prevent continued operation.
    /// </summary>
    Warning = 2,

    /// <summary>
    /// A failure or error condition that may still allow the application to continue.
    /// </summary>
    Error = 3,

    /// <summary>
    /// A severe failure that should be written immediately.
    /// </summary>
    Fatal = 4,

    /// <summary>
    /// Disables normal log output when used as the logger's minimum level.
    /// </summary>
    None = 5
}

internal sealed class CriticalLogException : InvalidOperationException
{
    internal CriticalLogException(string message)
        : base(message)
    {
    }

    internal CriticalLogException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Provides the process-wide VOID logging service.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Logger"/> queues log entries and writes them asynchronously to each
/// registered <see cref="ILogSink"/>. Messages can be filtered by
/// <see cref="LogLevel"/>, grouped by category, and formatted with the same
/// composite-format strings accepted by <see cref="string.Format(string, object[])"/>.
/// </para>
/// <code>
/// var logger = Logger.Instance;
///
/// logger.SetLevel(LogLevel.Info);
/// logger.AddSink(new ConsoleSink());
/// logger.AddSink(new FileSink("Logs", 10, 10));
///
/// logger.Info("Game started");
/// logger.Info("Player position: {0}, {1}", x, y);
/// logger.WarningWithCategory("Network", "Retry {0} of {1}", retry, maxRetries);
/// logger.ErrorWithCategory("Assets", exception, "Failed to load player texture");
///
/// // Critical writes a crash-style block, flushes pending logs, then throws.
/// logger.CriticalWithCategory("Renderer", "Failed to create framebuffer {0}x{1}", width, height);
/// </code>
/// <para>
/// Formatted messages below the configured minimum level are discarded before
/// <see cref="string.Format(string, object[])"/> is called. The queue is bounded
/// to prevent unbounded memory growth, and sink failures are isolated so one sink
/// cannot stop the others.
/// </para>
/// <para>
/// <see cref="Fatal()"/> and all other fatal overloads flush queued entries after
/// writing. Critical overloads additionally write a crash-style message and stack
/// trace, flush, and throw an <see cref="InvalidOperationException"/>.
/// </para>
/// <para>
/// Logging methods are safe to call from multiple threads. Sink writes are serialized
/// by the logger.
/// </para>
/// </remarks>
public sealed class Logger : IDisposable
{
    private const int MaxQueueSize = 10000;
    private const int BatchSize = 100;

    private static readonly Lazy<Logger> _instance = new(() => new Logger());

    private readonly ConcurrentQueue<LogEntry> _queue = [];
    private readonly List<ILogSink> _sinks = [];
    private readonly Lock _sinkLock = new();
    private readonly Lock _processLock = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Thread _worker;

    private volatile LogLevel _minimumLevel = LogLevel.Debug;
    private int _disposed;

    /// <summary>
    /// Gets the singleton logger instance.
    /// </summary>
    public static Logger Instance => _instance.Value;

    private bool IsDisposed => Volatile.Read(ref _disposed) != 0;

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
    /// Sets the minimum severity that will be queued.
    /// </summary>
    /// <param name="level">
    /// The minimum level to write. Use <see cref="LogLevel.None"/> to suppress
    /// normal log output.
    /// </param>
    public void SetLevel(LogLevel level)
    {
        _minimumLevel = level;
    }

    /// <summary>
    /// Gets the current minimum log level.
    /// </summary>
    /// <returns>The minimum severity currently accepted by the logger.</returns>
    public LogLevel GetLevel() => _minimumLevel;

    /// <summary>
    /// Adds a sink that will receive future log entries.
    /// </summary>
    /// <param name="sink">The sink to register.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="sink"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the logger has already been disposed.
    /// </exception>
    public void AddSink(ILogSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);

        if (IsDisposed)
            throw new ObjectDisposedException(nameof(Logger));

        lock (_sinkLock)
        {
            _sinks.Add(sink);
        }
    }

    /// <summary>
    /// Removes a previously registered sink.
    /// </summary>
    /// <param name="sink">The sink to remove. A <see langword="null"/> value is ignored.</param>
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
    /// Logs an empty debug entry.
    /// </summary>
    public void Debug() => Write(LogLevel.Debug, null, "", null);

    /// <summary>
    /// Logs a debug message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public void Debug(string message)
        => Write(LogLevel.Debug, null, message, null);

    /// <summary>
    /// Logs a formatted debug message.
    /// </summary>
    /// <param name="message">The composite format string.</param>
    /// <param name="args">Values inserted into <paramref name="message"/>.</param>
    public void Debug(string message, params object[] args)
        => WriteFormatted(LogLevel.Debug, null, message, args);

    /// <summary>
    /// Logs a debug message with a category.
    /// </summary>
    /// <param name="category">The category associated with the entry.</param>
    /// <param name="message">The message to log.</param>
    public void DebugWithCategory(string category, string message)
        => Write(LogLevel.Debug, category, message, null);

    /// <summary>
    /// Logs a formatted debug message with a category.
    /// </summary>
    /// <param name="category">The category associated with the entry.</param>
    /// <param name="message">The composite format string.</param>
    /// <param name="args">Values inserted into <paramref name="message"/>.</param>
    public void DebugWithCategory(string category, string message, params object[] args)
        => WriteFormatted(LogLevel.Debug, category, message, args);

    /// <summary>
    /// Logs an empty informational entry.
    /// </summary>
    public void Info() => Write(LogLevel.Info, null, "", null);

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public void Info(string message)
        => Write(LogLevel.Info, null, message, null);

    /// <summary>
    /// Logs a formatted informational message.
    /// </summary>
    /// <param name="message">The composite format string.</param>
    /// <param name="args">Values inserted into <paramref name="message"/>.</param>
    public void Info(string message, params object[] args)
        => WriteFormatted(LogLevel.Info, null, message, args);

    /// <summary>
    /// Logs an informational message with a category.
    /// </summary>
    /// <param name="category">The category associated with the entry.</param>
    /// <param name="message">The message to log.</param>
    public void InfoWithCategory(string category, string message)
        => Write(LogLevel.Info, category, message, null);

    /// <summary>
    /// Logs a formatted informational message with a category.
    /// </summary>
    /// <param name="category">The category associated with the entry.</param>
    /// <param name="message">The composite format string.</param>
    /// <param name="args">Values inserted into <paramref name="message"/>.</param>
    public void InfoWithCategory(string category, string message, params object[] args)
        => WriteFormatted(LogLevel.Info, category, message, args);

    /// <summary>
    /// Logs an empty warning entry.
    /// </summary>
    public void Warning() => Write(LogLevel.Warning, null, "", null);

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public void Warning(string message)
        => Write(LogLevel.Warning, null, message, null);

    /// <summary>
    /// Logs a formatted warning message.
    /// </summary>
    /// <param name="message">The composite format string.</param>
    /// <param name="args">Values inserted into <paramref name="message"/>.</param>
    public void Warning(string message, params object[] args)
        => WriteFormatted(LogLevel.Warning, null, message, args);

    /// <summary>
    /// Logs a warning message with a category.
    /// </summary>
    /// <param name="category">The category associated with the entry.</param>
    /// <param name="message">The message to log.</param>
    public void WarningWithCategory(string category, string message)
        => Write(LogLevel.Warning, category, message, null);

    /// <summary>
    /// Logs a formatted warning message with a category.
    /// </summary>
    /// <param name="category">The category associated with the entry.</param>
    /// <param name="message">The composite format string.</param>
    /// <param name="args">Values inserted into <paramref name="message"/>.</param>
    public void WarningWithCategory(string category, string message, params object[] args)
        => WriteFormatted(LogLevel.Warning, category, message, args);

    /// <summary>
    /// Logs an empty error entry.
    /// </summary>
    public void Error() => Write(LogLevel.Error, null, "", null);

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public void Error(string message)
        => Write(LogLevel.Error, null, message, null);

    /// <summary>
    /// Logs a formatted error message.
    /// </summary>
    /// <param name="message">The composite format string.</param>
    /// <param name="args">Values inserted into <paramref name="message"/>.</param>
    public void Error(string message, params object[] args)
        => WriteFormatted(LogLevel.Error, null, message, args);

    /// <summary>
    /// Logs an exception as an error.
    /// </summary>
    /// <param name="exception">The exception associated with the entry.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="exception"/> is <see langword="null"/>.
    /// </exception>
    public void Error(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        Write(LogLevel.Error, null, exception.Message, exception);
    }

    /// <summary>
    /// Logs an error message with an associated exception.
    /// </summary>
    /// <param name="exception">The exception associated with the entry.</param>
    /// <param name="message">The message to log.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="exception"/> is <see langword="null"/>.
    /// </exception>
    public void Error(Exception exception, string message)
    {
        ArgumentNullException.ThrowIfNull(exception);
        Write(LogLevel.Error, null, message, exception);
    }

    /// <summary>
    /// Logs an error message with a category.
    /// </summary>
    /// <param name="category">The category associated with the entry.</param>
    /// <param name="message">The message to log.</param>
    public void ErrorWithCategory(string category, string message)
        => Write(LogLevel.Error, category, message, null);

    /// <summary>
    /// Logs a formatted error message with a category.
    /// </summary>
    /// <param name="category">The category associated with the entry.</param>
    /// <param name="message">The composite format string.</param>
    /// <param name="args">Values inserted into <paramref name="message"/>.</param>
    public void ErrorWithCategory(string category, string message, params object[] args)
        => WriteFormatted(LogLevel.Error, category, message, args);

    /// <summary>
    /// Logs an exception as a categorized error.
    /// </summary>
    /// <param name="category">The category associated with the entry.</param>
    /// <param name="exception">The exception associated with the entry.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="exception"/> is <see langword="null"/>.
    /// </exception>
    public void ErrorWithCategory(string category, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        Write(LogLevel.Error, category, exception.Message, exception);
    }

    /// <summary>
    /// Logs a categorized error message with an associated exception.
    /// </summary>
    /// <param name="category">The category associated with the entry.</param>
    /// <param name="exception">The exception associated with the entry.</param>
    /// <param name="message">The message to log.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="exception"/> is <see langword="null"/>.
    /// </exception>
    public void ErrorWithCategory(string category, Exception exception, string message)
    {
        ArgumentNullException.ThrowIfNull(exception);
        Write(LogLevel.Error, category, message, exception);
    }

    /// <summary>
    /// Logs an empty fatal entry and flushes queued log messages.
    /// </summary>
    public void Fatal()
    {
        Write(LogLevel.Fatal, null, "", null);
        Flush();
    }

    /// <summary>
    /// Logs a fatal message and flushes queued log messages.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public void Fatal(string message)
    {
        Write(LogLevel.Fatal, null, message, null);
        Flush();
    }

    /// <summary>
    /// Logs a formatted fatal message and flushes queued log messages.
    /// </summary>
    /// <param name="message">The composite format string.</param>
    /// <param name="args">Values inserted into <paramref name="message"/>.</param>
    public void Fatal(string message, params object[] args)
        => WriteFormattedAndFlush(LogLevel.Fatal, null, message, args);

    /// <summary>
    /// Logs an exception as fatal and flushes queued log messages.
    /// </summary>
    /// <param name="exception">The exception associated with the entry.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="exception"/> is <see langword="null"/>.
    /// </exception>
    public void Fatal(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        Write(LogLevel.Fatal, null, exception.Message, exception);
        Flush();
    }

    /// <summary>
    /// Logs a fatal message with an associated exception and flushes queued log messages.
    /// </summary>
    /// <param name="exception">The exception associated with the entry.</param>
    /// <param name="message">The message to log.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="exception"/> is <see langword="null"/>.
    /// </exception>
    public void Fatal(Exception exception, string message)
    {
        ArgumentNullException.ThrowIfNull(exception);
        Write(LogLevel.Fatal, null, message, exception);
        Flush();
    }

    /// <summary>
    /// Logs a categorized fatal message and flushes queued log messages.
    /// </summary>
    /// <param name="category">The category associated with the entry.</param>
    /// <param name="message">The message to log.</param>
    public void FatalWithCategory(string category, string message)
    {
        Write(LogLevel.Fatal, category, message, null);
        Flush();
    }

    /// <summary>
    /// Logs a formatted categorized fatal message and flushes queued log messages.
    /// </summary>
    /// <param name="category">The category associated with the entry.</param>
    /// <param name="message">The composite format string.</param>
    /// <param name="args">Values inserted into <paramref name="message"/>.</param>
    public void FatalWithCategory(string category, string message, params object[] args)
        => WriteFormattedAndFlush(LogLevel.Fatal, category, message, args);

    /// <summary>
    /// Logs an exception as categorized fatal and flushes queued log messages.
    /// </summary>
    /// <param name="category">The category associated with the entry.</param>
    /// <param name="exception">The exception associated with the entry.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="exception"/> is <see langword="null"/>.
    /// </exception>
    public void FatalWithCategory(string category, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        Write(LogLevel.Fatal, category, exception.Message, exception);
        Flush();
    }

    /// <summary>
    /// Logs a categorized fatal message with an associated exception and flushes queued log messages.
    /// </summary>
    /// <param name="category">The category associated with the entry.</param>
    /// <param name="exception">The exception associated with the entry.</param>
    /// <param name="message">The message to log.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="exception"/> is <see langword="null"/>.
    /// </exception>
    public void FatalWithCategory(string category, Exception exception, string message)
    {
        ArgumentNullException.ThrowIfNull(exception);
        Write(LogLevel.Fatal, category, message, exception);
        Flush();
    }

    /// <summary>
    /// Logs a critical crash with no category, flushes the logger, and throws.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Always thrown after the critical crash details have been flushed.
    /// </exception>
    [DoesNotReturn]
    public void Critical()
        => ThrowCritical(null, "Critical error", null);

    /// <summary>
    /// Logs a critical crash with no category, flushes the logger, and throws.
    /// </summary>
    /// <param name="message">The crash message.</param>
    /// <exception cref="InvalidOperationException">
    /// Always thrown after the critical crash details have been flushed.
    /// </exception>
    [DoesNotReturn]
    public void Critical(string message)
        => ThrowCritical(null, message, null);

    /// <summary>
    /// Logs a formatted critical crash with no category, flushes the logger, and throws.
    /// </summary>
    /// <param name="message">The composite format string.</param>
    /// <param name="args">Values inserted into <paramref name="message"/>.</param>
    /// <exception cref="InvalidOperationException">
    /// Always thrown after the critical crash details have been flushed.
    /// </exception>
    [DoesNotReturn]
    public void Critical(string message, params object[] args)
        => ThrowCritical(null, string.Format(message, args), null);

    /// <summary>
    /// Logs an exception as a critical crash, flushes the logger, and throws.
    /// </summary>
    /// <param name="exception">The exception that caused the critical failure.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="exception"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Always thrown after the critical crash details have been flushed.
    /// </exception>
    [DoesNotReturn]
    public void Critical(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ThrowCritical(null, exception.Message, exception);
    }

    /// <summary>
    /// Logs a critical crash message with an associated exception, flushes the logger, and throws.
    /// </summary>
    /// <param name="exception">The exception that caused the critical failure.</param>
    /// <param name="message">The crash message.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="exception"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Always thrown after the critical crash details have been flushed.
    /// </exception>
    [DoesNotReturn]
    public void Critical(Exception exception, string message)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ThrowCritical(null, message, exception);
    }

    /// <summary>
    /// Logs a categorized critical crash, flushes the logger, and throws.
    /// </summary>
    /// <param name="category">The category associated with the crash.</param>
    /// <param name="message">The crash message.</param>
    /// <exception cref="InvalidOperationException">
    /// Always thrown after the critical crash details have been flushed.
    /// </exception>
    [DoesNotReturn]
    public void CriticalWithCategory(string category, string message)
        => ThrowCritical(category, message, null);

    /// <summary>
    /// Logs a formatted categorized critical crash, flushes the logger, and throws.
    /// </summary>
    /// <param name="category">The category associated with the crash.</param>
    /// <param name="message">The composite format string.</param>
    /// <param name="args">Values inserted into <paramref name="message"/>.</param>
    /// <exception cref="InvalidOperationException">
    /// Always thrown after the critical crash details have been flushed.
    /// </exception>
    [DoesNotReturn]
    public void CriticalWithCategory(string category, string message, params object[] args)
        => ThrowCritical(category, string.Format(message, args), null);

    /// <summary>
    /// Logs an exception as a categorized critical crash, flushes the logger, and throws.
    /// </summary>
    /// <param name="category">The category associated with the crash.</param>
    /// <param name="exception">The exception that caused the critical failure.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="exception"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Always thrown after the critical crash details have been flushed.
    /// </exception>
    [DoesNotReturn]
    public void CriticalWithCategory(string category, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ThrowCritical(category, exception.Message, exception);
    }

    /// <summary>
    /// Logs a categorized critical crash message with an associated exception,
    /// flushes the logger, and throws.
    /// </summary>
    /// <param name="category">The category associated with the crash.</param>
    /// <param name="exception">The exception that caused the critical failure.</param>
    /// <param name="message">The crash message.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="exception"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Always thrown after the critical crash details have been flushed.
    /// </exception>
    [DoesNotReturn]
    public void CriticalWithCategory(string category, Exception exception, string message)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ThrowCritical(category, message, exception);
    }

    private bool IsEnabled(LogLevel level)
        => !IsDisposed && level >= _minimumLevel;

    private void WriteFormatted(LogLevel level, string category, string message, object[] args)
    {
        if (!IsEnabled(level))
            return;

        Write(level, category, string.Format(message, args), null);
    }

    private void WriteFormattedAndFlush(LogLevel level, string category, string message, object[] args)
    {
        if (IsEnabled(level))
            Write(level, category, string.Format(message, args), null);

        Flush();
    }

    private void Write(LogLevel level, string category, string message, Exception exception)
    {
        if (!IsEnabled(level))
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
            bool wroteEntries;

            lock (_processLock)
            {
                var batch = new List<LogEntry>(BatchSize);

                while (batch.Count < BatchSize && _queue.TryDequeue(out var entry))
                    batch.Add(entry);

                wroteEntries = batch.Count > 0;
                WriteBatch(batch);
            }

            if (!wroteEntries)
                Thread.Sleep(1);
        }
    }

    private void Flush()
    {
        lock (_processLock)
        {
            var batch = new List<LogEntry>();

            while (_queue.TryDequeue(out var entry))
                batch.Add(entry);

            WriteBatch(batch);
        }
    }

    private void WriteBatch(List<LogEntry> batch)
    {
        if (batch.Count == 0)
            return;

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
                        // A sink failure must not stop the logger or the game.
                    }
                }
            }
        }
    }

    private void WriteCriticalCrash(string category, string message, string stackTrace)
    {
        if (!string.IsNullOrEmpty(message))
            Write(LogLevel.Fatal, category, $"Message: {message}", null);

        if (!string.IsNullOrEmpty(stackTrace))
        {
            Write(LogLevel.Fatal, category, "Stack Trace:", null);
            Write(LogLevel.Fatal, category, stackTrace, null);
        }

        if (string.IsNullOrEmpty(message) && string.IsNullOrEmpty(stackTrace))
            Write(LogLevel.Fatal, category, "Unknown crash - no exception details available", null);
    }

    [StackTraceHidden]
    [DoesNotReturn]
    private void ThrowCritical(string category, string message, Exception innerException)
    {
        var criticalException = innerException == null
            ? new CriticalLogException(message)
            : new CriticalLogException(message, innerException);

        try
        {
            throw criticalException;
        }
        catch (CriticalLogException exception)
        {
            Flush();

            string stackTrace = !string.IsNullOrEmpty(innerException?.StackTrace)
                ? innerException.StackTrace
                : exception.StackTrace;

            WriteCriticalCrash(category, message, stackTrace);
            Flush();
            throw;
        }
    }

    /// <summary>
    /// Stops background logging and flushes all entries that remain queued.
    /// </summary>
    /// <remarks>
    /// Disposal is idempotent. Messages submitted after disposal are ignored, and
    /// new sinks can no longer be registered.
    /// </remarks>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        _cts.Cancel();
        _worker.Join(1000);
        Flush();
    }
}
