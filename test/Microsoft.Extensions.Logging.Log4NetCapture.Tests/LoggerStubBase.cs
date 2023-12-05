namespace Microsoft.Extensions.Logging.Log4NetCapture.Tests;

public abstract class LoggerStubBase<T> : ILogger
{
    private readonly Dictionary<int, Stack<LoggerStubScope<T>>> _scopeDict = new();
    public List<T>? Items { get; set; }

    public bool Enabled { get; set; }

    public int IsEnabledInvocationCount { get; private set; }
    public int LogInvocationCount { get; private set; }

    public IDisposable BeginScope<TState>(TState state)
    {
        return new LoggerStubScope<T>(this, state.ToString());
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        IsEnabledInvocationCount++;
        return Enabled;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        LogInvocationCount++;
        Items.Add(CreateItem(logLevel, eventId, state, exception, formatter));
    }

    public bool AnyCurrentThreadScope()
    {
        if (_scopeDict.ContainsKey(Thread.CurrentThread.ManagedThreadId))
            return _scopeDict[Thread.CurrentThread.ManagedThreadId].Any();
        return false;
    }

    public LoggerStubScope<T> PeekCurrentThreadScope()
    {
        return GetScopesForCurrentThread().Peek();
    }

    public void PushCurrentThreadScope(LoggerStubScope<T> scope)
    {
        GetScopesForCurrentThread().Push(scope);
    }

    public LoggerStubScope<T> PopCurrentThreadScope()
    {
        return GetScopesForCurrentThread().Pop();
    }

    private Stack<LoggerStubScope<T>> GetScopesForCurrentThread()
    {
        lock (_scopeDict)
        {
            var threadId = Thread.CurrentThread.ManagedThreadId;
            if (_scopeDict.ContainsKey(threadId)) return _scopeDict[threadId];

            _scopeDict[threadId] = new Stack<LoggerStubScope<T>>();
            return _scopeDict[threadId];
        }
    }

    public abstract T CreateItem<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter);
}