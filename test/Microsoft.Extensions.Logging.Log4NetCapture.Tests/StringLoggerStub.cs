namespace Microsoft.Extensions.Logging.Log4NetCapture.Tests;

public class StringLoggerStub : LoggerStubBase<string>
{
    public override string CreateItem<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        return formatter(state, exception);
    }
}