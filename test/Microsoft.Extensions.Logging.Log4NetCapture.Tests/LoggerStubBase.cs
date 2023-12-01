namespace Microsoft.Extensions.Logging.Log4NetCapture.Tests;

public abstract class LoggerStubBase<T> : ILogger
{
    public List<T>? Items { get; set; }

    public bool Enabled { get; set; }

    public Stack<LoggerStubScope<T>> Scopes { get; } = new();
    public int CheckEnabledCount { get; private set; }
    public int LogCount { get; private set; }

    public IDisposable BeginScope<TState>(TState state)
    {
        return new LoggerStubScope<T>(this, state.ToString());
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        CheckEnabledCount++;
        return Enabled;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        LogCount++;
        Items.Add(CreateItem(logLevel, eventId, state, exception, formatter));
    }

    public abstract T CreateItem<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter);
}