namespace Void.Engine.Logs;

public struct LogEntry
{
    public LogLevel Level;
    public DateTime Timestamp;
    public string Message;
    public string Category;
    public Exception Exception;
}
