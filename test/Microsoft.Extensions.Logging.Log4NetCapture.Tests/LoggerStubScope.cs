namespace Microsoft.Extensions.Logging.Log4NetCapture.Tests;

public class LoggerStubScope<T> : IDisposable
{
    public LoggerStubScope(LoggerStubBase<T> loggerStub, string name)
    {
        LoggerStub = loggerStub;
        if (loggerStub.Scopes.Any()) Parent = loggerStub.Scopes.Peek();
        loggerStub.Scopes.Push(this);
        Name = name;
    }

    public LoggerStubScope<T>? Parent { get; }
    public string Name { get; }
    public LoggerStubBase<T> LoggerStub { get; }

    public void Dispose()
    {
        LoggerStub.Scopes.Pop();
    }

    public string? FullName()
    {
        var last = this;
        var names = new List<string>();
        while (last != null)
        {
            names.Insert(0, last.Name);
            last = last.Parent;
        }

        return string.Join(".", names);
    }
}