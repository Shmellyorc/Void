// ============================================================================
//  FileSink.cs
// ============================================================================
//  Writes log entries to daily files with size rollover and retention cleanup.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using Void.Engine.Logs;

namespace Void.Engine.Logs.Sinks;

/// <summary>
/// Writes log entries to daily text files with size-based rollover and retention cleanup.
/// </summary>
/// <remarks>
/// <para>
/// The first file for a day is named <c>log_dd-MM-yyyy.txt</c>. When that file
/// reaches the configured size limit, additional files use numeric suffixes such as
/// <c>log_dd-MM-yyyy_1.txt</c>, <c>log_dd-MM-yyyy_2.txt</c>, and so on.
/// </para>
/// <para>
/// Entries are written as
/// <c>[dd-MM-yyyy HH:mm:ss.fff] [Level] [Category] Message</c>. The category
/// segment is omitted when no category is present. An associated exception is
/// appended on the following line.
/// </para>
/// <para>
/// The sink keeps at most the configured number of matching log files, removing
/// the oldest files by last-write time when daily or size-based rotation creates
/// a newer file.
/// </para>
/// </remarks>
public sealed class FileSink : ILogSink
{
    private static readonly Encoding Utf8 = new UTF8Encoding(false);

    private readonly string _logFolder;
    private readonly long _maxFileSize;
    private readonly int _maxFiles;
    private readonly Lock _lock = new();

    private string _currentFilePath;
    private DateTime _currentDate;
    private int _currentFileIndex;
    private long _currentSize;

    /// <summary>
    /// Initializes a file sink.
    /// </summary>
    /// <param name="logFolder">
    /// The folder where log files are stored. The folder is created when necessary.
    /// </param>
    /// <param name="maxFileSizeMB">
    /// The maximum size of one log file in megabytes. The default is 10.
    /// </param>
    /// <param name="maxFiles">
    /// The maximum number of matching log files retained in <paramref name="logFolder"/>.
    /// The default is 10.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="logFolder"/> is <see langword="null"/>, empty,
    /// or contains only whitespace.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxFileSizeMB"/> or <paramref name="maxFiles"/>
    /// is less than or equal to zero.
    /// </exception>
    public FileSink(string logFolder, long maxFileSizeMB = 10, int maxFiles = 10)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logFolder);

        if (maxFileSizeMB <= 0 || maxFileSizeMB > long.MaxValue / (1024L * 1024L))
            throw new ArgumentOutOfRangeException(nameof(maxFileSizeMB), "Maximum file size is outside the supported range.");

        if (maxFiles <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxFiles), "Maximum file count must be greater than zero.");

        _logFolder = logFolder;
        _maxFileSize = maxFileSizeMB * 1024L * 1024L;
        _maxFiles = maxFiles;

        Directory.CreateDirectory(_logFolder);
    }

    /// <summary>
    /// Writes a log entry to the current log file.
    /// </summary>
    /// <param name="entry">The entry to format and write.</param>
    public void Write(LogEntry entry)
    {
        lock (_lock)
        {
            bool changedFile = SelectCurrentFile();

            string text = Format(entry) + Environment.NewLine;
            int byteCount = Utf8.GetByteCount(text);

            if (_currentSize > 0 && _currentSize + byteCount > _maxFileSize)
            {
                SelectNextFile();
                changedFile = true;
            }

            File.AppendAllText(_currentFilePath, text, Utf8);
            _currentSize += byteCount;

            if (changedFile)
                CleanupOldFiles();
        }
    }

    private bool SelectCurrentFile()
    {
        DateTime today = DateTime.Now.Date;

        if (_currentFilePath != null && _currentDate == today)
            return false;

        _currentDate = today;
        _currentFileIndex = FindLatestFileIndex(today);

        if (_currentFileIndex < 0)
        {
            _currentFileIndex = 0;
            _currentFilePath = GetFilePath(today, _currentFileIndex);
            _currentSize = 0;
            return true;
        }

        _currentFilePath = GetFilePath(today, _currentFileIndex);
        _currentSize = File.Exists(_currentFilePath)
            ? new FileInfo(_currentFilePath).Length
            : 0;

        if (_currentSize >= _maxFileSize)
        {
            SelectNextFile();
            return true;
        }

        return true;
    }

    private void SelectNextFile()
    {
        _currentFileIndex++;

        string nextPath = GetFilePath(_currentDate, _currentFileIndex);

        while (File.Exists(nextPath))
        {
            _currentFileIndex++;
            nextPath = GetFilePath(_currentDate, _currentFileIndex);
        }

        _currentFilePath = nextPath;
        _currentSize = 0;
    }

    private int FindLatestFileIndex(DateTime date)
    {
        string baseName = $"log_{date:dd-MM-yyyy}";
        int latestIndex = -1;

        foreach (string file in Directory.GetFiles(_logFolder, $"{baseName}*.txt"))
        {
            string name = Path.GetFileNameWithoutExtension(file);

            if (string.Equals(name, baseName, StringComparison.Ordinal))
            {
                latestIndex = Math.Max(latestIndex, 0);
                continue;
            }

            string prefix = baseName + "_";

            if (!name.StartsWith(prefix, StringComparison.Ordinal))
                continue;

            if (int.TryParse(name[prefix.Length..], out int index) && index > latestIndex)
                latestIndex = index;
        }

        return latestIndex;
    }

    private string GetFilePath(DateTime date, int index)
    {
        string suffix = index == 0 ? "" : $"_{index}";
        return Path.Combine(_logFolder, $"log_{date:dd-MM-yyyy}{suffix}.txt");
    }

    private void CleanupOldFiles()
    {
        string currentPath = Path.GetFullPath(_currentFilePath);

        var logFiles = Directory.GetFiles(_logFolder, "log_*.txt")
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => string.Equals(file.FullName, currentPath, StringComparison.Ordinal))
            .ThenByDescending(file => file.LastWriteTimeUtc)
            .ToList();

        if (logFiles.Count <= _maxFiles)
            return;

        foreach (FileInfo file in logFiles.Skip(_maxFiles))
        {
            try
            {
                file.Delete();
            }
            catch
            {
                // Cleanup failure must not stop logging.
            }
        }
    }

    private static string Format(LogEntry entry)
    {
        string category = string.IsNullOrEmpty(entry.Category) ? "" : $"[{entry.Category}] ";
        string exception = entry.Exception != null ? $"\n{entry.Exception}" : "";

        return $"[{entry.Timestamp:dd-MM-yyyy HH:mm:ss.fff}] [{entry.Level}] {category}{entry.Message}{exception}";
    }
}
