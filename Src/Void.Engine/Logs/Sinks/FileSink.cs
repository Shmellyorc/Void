namespace Void.Engine.Logs.Sinks;

public sealed class FileSink : ILogSink
{
    private readonly string _logFolder;
    private readonly long _maxFileSize;
    private readonly int _maxFiles;
    private string _currentFilePath;
    private DateTime _currentDate;
    private long _currentSize;
    private readonly Lock _lock = new();

    public FileSink(string logFolder, long maxFileSizeMB = 10, int maxFiles = 10)
    {
        _logFolder = logFolder;
        _maxFileSize = maxFileSizeMB * 1024 * 1024;
        _maxFiles = maxFiles;
        Directory.CreateDirectory(logFolder);
        CreateNewFileIfNeeded();
    }

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
                    // Ignore
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