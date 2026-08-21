// ============================================================================
//  FileSink.cs
// ============================================================================
//  Log sink that writes formatted log messages to daily rotating files
//  with size-based rollover and automatic cleanup of old files.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Void.Engine.Logs.Sinks;

/// <summary>
/// A log sink that writes formatted log messages to daily rotating files
/// with size-based rollover and automatic cleanup of old files.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="FileSink"/> implements <see cref="ILogSink"/> and writes
/// log entries to text files in the specified folder. It provides:
/// <list type="bullet">
///   <item><description>Daily file rotation with date-based naming</description></item>
///   <item><description>Size-based rollover to prevent individual files from growing too large</description></item>
///   <item><description>Automatic cleanup of old files to manage disk usage</description></item>
/// </list>
/// </para>
/// <para>
/// Each log entry is formatted as:
/// <c>[dd-MM-yyyy HH:mm:ss.fff] [Level] [Category] Message</c>
/// If an exception is present, it is included on a new line after the message.
/// </para>
/// <para>
/// <b>File Naming:</b>
/// Files are named <c>log_dd-MM-yyyy.txt</c> and stored in the specified log folder.
/// If a file exceeds the maximum size, a new file is created for the same day.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Add a file sink with 10MB max file size and keep 10 files
/// var fileSink = new FileSink("Logs/", 10, 10);
/// Logger.Instance.AddSink(fileSink);
/// 
/// // Now all log messages will be written to files
/// Logger.Instance.Info("Game started");
/// Logger.Instance.Error("Failed to load texture", exception);
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is thread-safe. A lock is used to ensure that file writes
/// from multiple threads are properly synchronized.
/// </para>
/// </remarks>
public sealed class FileSink : ILogSink
{
    private readonly string _logFolder;
    private readonly long _maxFileSize;
    private readonly int _maxFiles;
    private string _currentFilePath;
    private DateTime _currentDate;
    private long _currentSize;
    private readonly Lock _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSink"/> class.
    /// </summary>
    /// <param name="logFolder">The folder where log files will be stored. The folder is created if it does not exist.</param>
    /// <param name="maxFileSizeMB">The maximum size of each log file in megabytes. Default is 10.</param>
    /// <param name="maxFiles">The maximum number of log files to keep. Default is 10.</param>
    public FileSink(string logFolder, long maxFileSizeMB = 10, int maxFiles = 10)
    {
        _logFolder = logFolder;
        _maxFileSize = maxFileSizeMB * 1024 * 1024;
        _maxFiles = maxFiles;
        Directory.CreateDirectory(logFolder);
        CreateNewFileIfNeeded();
    }

    /// <summary>
    /// Writes a log entry to the current log file.
    /// </summary>
    /// <param name="entry">The log entry to write.</param>
    public void Write(LogEntry entry)
    {
        lock (_lock)
        {
            CreateNewFileIfNeeded();

            var line = Format(entry);
            File.AppendAllText(_currentFilePath, line + Environment.NewLine);
            _currentSize += line.Length;

            if (_currentSize >= _maxFileSize)
            {
                CleanupOldFiles();
                CreateNewFileIfNeeded(forceNew: true);
            }
        }
    }

    private void CreateNewFileIfNeeded(bool forceNew = false)
    {
        var today = DateTime.Now.Date;

        if (!forceNew && _currentDate == today && File.Exists(_currentFilePath))
            return;

        _currentDate = today;
        _currentFilePath = Path.Combine(_logFolder, $"log_{today:dd-MM-yyyy}.txt");

        _currentSize = File.Exists(_currentFilePath) ? new FileInfo(_currentFilePath).Length : 0;
    }

    private void CleanupOldFiles()
    {
        var logFiles = Directory.GetFiles(_logFolder, "log_*.txt")
            .OrderByDescending(f => f)
            .ToList();

        if (logFiles.Count > _maxFiles)
        {
            foreach (var file in logFiles.Skip(_maxFiles))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // Ignore deletion errors to prevent logging failures
                }
            }
        }
    }

    private string Format(LogEntry entry)
    {
        var category = string.IsNullOrEmpty(entry.Category) ? "" : $"[{entry.Category}] ";
        var exception = entry.Exception != null ? $"\n{entry.Exception}" : "";
        return $"[{entry.Timestamp:dd-MM-yyyy HH:mm:ss.fff}] [{entry.Level}] {category}{entry.Message}{exception}";
    }
}