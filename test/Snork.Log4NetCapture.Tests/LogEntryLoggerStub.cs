using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Snork.Log4NetCapture.Tests;

public class LogEntryLoggerStub : LoggerStubBase<LogEntry>
{
    public override LogEntry CreateItem<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var json = formatter(state, exception);
        var logEntry = JsonConvert.DeserializeObject<LogEntry>(json);
        logEntry.EventualPassedException = exception;
        
        if (AnyCurrentThreadScope()) logEntry.Scope =  PeekCurrentThreadScope().FullName();
        return logEntry;
    }
}