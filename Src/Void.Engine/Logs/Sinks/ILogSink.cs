namespace Void.Engine.Logs.Sinks;

public interface ILogSink
{
    void Write(LogEntry entry);
}
