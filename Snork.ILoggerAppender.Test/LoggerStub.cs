using Newtonsoft.Json;

namespace Microsoft.Extensions.Logging.Log4NetCapture.Tests;

public class LoggerStub : ILogger
{
    private readonly bool _isEnabled;
    private readonly List<LogEntry> _messages;

    public LoggerStub(bool isEnabled, List<LogEntry> messages)
    {
        _isEnabled = isEnabled;
        _messages = messages;
    }

    public Stack<LoggerStubScope> Scopes { get; } = new();
    public int CheckEnabledCount { get; private set; }
    public int LogCount { get; private set; }

    public IDisposable BeginScope<TState>(TState state)
    {
        return new LoggerStubScope(this, state.ToString());
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        CheckEnabledCount++;
        return _isEnabled;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        LogCount++;
        var logEntry = JsonConvert.DeserializeObject<LogEntry>(formatter(state, exception));
        if (Scopes.Any()) logEntry.Scope = Scopes.Peek().FullName();
        _messages.Add(logEntry);
    }
}