namespace Snork.Log4NetCapture.Tests;

public class LoggerStubScope<T> : IDisposable
{
    public LoggerStubScope(LoggerStubBase<T> loggerStub, string name)
    {
        LoggerStub = loggerStub;
        if (loggerStub.AnyCurrentThreadScope()) Parent = loggerStub.PeekCurrentThreadScope();
        loggerStub.PushCurrentThreadScope(this);
        Name = name;
    }

    public LoggerStubScope<T>? Parent { get; }
    public string Name { get; }
    public LoggerStubBase<T> LoggerStub { get; }

    public void Dispose()
    {
        LoggerStub.PopCurrentThreadScope();
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