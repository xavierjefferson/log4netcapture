using Newtonsoft.Json;

namespace Microsoft.Extensions.Logging.Log4NetCapture.Tests;

public class LogEntryLoggerStub : LoggerStubBase<LogEntry>
{
    public override LogEntry CreateItem<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var logEntry = JsonConvert.DeserializeObject<LogEntry>(formatter(state, exception));
        logEntry.Date = DateTime.Now;
        if (Scopes.Any()) logEntry.Scope = Scopes.Peek().FullName();
        return logEntry;
    }
}