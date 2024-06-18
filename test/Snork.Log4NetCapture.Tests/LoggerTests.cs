using System.Linq.Expressions;
using System.Text;
using log4net;
using log4net.Core;
using log4net.Filter;
using log4net.Layout;
using log4net.ObjectRenderer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Moq;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Snork.Log4NetCapture.Tests;

public class LoggerTests
{
    private const string message =
        "Integer est tortor, lobortis ac leo nec, sollicitudin porttitor metus. Nunc molestie ornare mi, vitae ultrices mi bibendum sed. Donec venenatis volutpat nibh eu blandit. Aenean laoreet nulla turpis, a sollicitudin lorem mollis ac. Curabitur et tempor lectus. Curabitur placerat vel felis sit amet luctus. Donec eu mauris egestas, semper sapien a, lacinia leo. Donec faucibus egestas nisl vitae tincidunt. Ut lorem lorem, posuere ac mollis at, sagittis at lacus. Mauris sollicitudin sit amet nibh et luctus.";

    private static readonly Expression<Func<ILoggerProvider, ILogger>> _createLoggerExpression =
        i => i.CreateLogger(It.IsAny<string>());


    private readonly ITestOutputHelper TestOutputHelper;

    // Pass ITestOutputHelper into the test class, which xunit provides per-test
    public LoggerTests(ITestOutputHelper testOutputHelper)
    {
        TestOutputHelper = testOutputHelper;
        testOutputHelper.WriteLine("Testing...");
    }

    [Theory]
    [InlineData(KnownLevelEnum.Alert, false, typeof(IDisposable))]
    [InlineData(KnownLevelEnum.Alert, true, typeof(IDisposable))]
    [InlineData(KnownLevelEnum.All, false, typeof(IDisposable))]
    [InlineData(KnownLevelEnum.All, true, typeof(IDisposable))]
    [InlineData(KnownLevelEnum.Critical, false, typeof(Mock))]
    [InlineData(KnownLevelEnum.Critical, true, typeof(IDisposable))]
    [InlineData(KnownLevelEnum.Debug, false, typeof(Expression))]
    [InlineData(KnownLevelEnum.Debug, true, typeof(string))]
    [InlineData(KnownLevelEnum.Emergency, false, typeof(IDisposable))]
    [InlineData(KnownLevelEnum.Emergency, true, typeof(IDisposable))]
    [InlineData(KnownLevelEnum.Error, false, typeof(EventId))]
    [InlineData(KnownLevelEnum.Error, true, typeof(Exception))]
    [InlineData(KnownLevelEnum.Fatal, false, typeof(IDisposable))]
    [InlineData(KnownLevelEnum.Fatal, true, typeof(IDisposable))]
    [InlineData(KnownLevelEnum.Fine, false, typeof(IDisposable))]
    [InlineData(KnownLevelEnum.Fine, true, typeof(IDisposable))]
    [InlineData(KnownLevelEnum.Finer, false, typeof(IDisposable))]
    [InlineData(KnownLevelEnum.Finer, true, typeof(IDisposable))]
    [InlineData(KnownLevelEnum.Finest, false, typeof(IDisposable))]
    [InlineData(KnownLevelEnum.Finest, true, typeof(IDisposable))]
    [InlineData(KnownLevelEnum.Info, false, typeof(KnownLevelEnum))]
    [InlineData(KnownLevelEnum.Info, true, typeof(LoggerTests))]
    [InlineData(KnownLevelEnum.Log4Net_Debug, false, typeof(Mock))]
    [InlineData(KnownLevelEnum.Log4Net_Debug, true, typeof(Mock))]
    [InlineData(KnownLevelEnum.Notice, false, typeof(IDisposable))]
    [InlineData(KnownLevelEnum.Notice, true, typeof(IDisposable))]
    [InlineData(KnownLevelEnum.Off, false, typeof(ILogger))]
    [InlineData(KnownLevelEnum.Off, true, typeof(ILogger))]
    [InlineData(KnownLevelEnum.Severe, false, typeof(IDisposable))]
    [InlineData(KnownLevelEnum.Severe, true, typeof(IDisposable))]
    [InlineData(KnownLevelEnum.Trace, false, typeof(Mock))]
    [InlineData(KnownLevelEnum.Trace, true, typeof(Mock))]
    [InlineData(KnownLevelEnum.Verbose, false, typeof(Mock))]
    [InlineData(KnownLevelEnum.Verbose, true, typeof(Mock))]
    [InlineData(KnownLevelEnum.Warn, false, typeof(Action))]
    [InlineData(KnownLevelEnum.Warn, true, typeof(StringBuilder))]
    public void TestLogLevel(KnownLevelEnum logLevel, bool iLoggerEnabled, Type loggerType)
    {
        var testContainer = GetLogEntryTestContainer(iLoggerEnabled);
        var startDate = DateTime.Now;

        testContainer.ServiceProvider.StartLog4NetCapture();
        var log = LogManager.GetLogger(loggerType);

        var times = Times.Once();
        Level? currentLevel = null;
        switch (logLevel)
        {
            case KnownLevelEnum.Warn:
                log.Warn(message);
                break;
            case KnownLevelEnum.Critical:
                log.Fatal(message);
                break;
            case KnownLevelEnum.Info:
                log.Info(message);

                break;
            case KnownLevelEnum.Error:
                log.Error(message);
                break;
            case KnownLevelEnum.Debug:
                log.Debug(message);
                break;
            default:
                currentLevel = KnownLevelMapper.GetLevel(logLevel);
                break;
        }

        if (currentLevel != null)
            log.Logger.Log(new LoggingEvent(loggerType, log.Logger.Repository,
                new LoggingEventData { LoggerName = log.Logger.Name, Level = currentLevel, Message = message }));

        var loggerCreated = testContainer.LoggerStubs.ContainsKey(loggerType.FullName);

        if (logLevel == KnownLevelEnum.Off)
        {
            Assert.False(loggerCreated);
            Assert.Empty(testContainer.LogEntries);
        }
        else
        {
            Assert.True(loggerCreated);
            var logger = testContainer.LoggerStubs[loggerType.FullName];
            testContainer.LoggerProviderMock.Verify(_createLoggerExpression, Times.AtLeastOnce);
            if (iLoggerEnabled)
                times.Validate(logger.LogInvocationCount);
            else
                Times.Never().Validate(logger.LogInvocationCount);
            //Assert.Equal(logger.LogCount, iLoggerEnabled ? times.Validate(logger.LogCount) : Times.Never());
            if (iLoggerEnabled)
            {
                Assert.NotEmpty(testContainer.LogEntries);
                if (currentLevel != null)
                    Assert.Contains(testContainer.LogEntries, i => i.Level.Equals(currentLevel.DisplayName));
                Assert.Contains(testContainer.LogEntries, i => i.Logger.Equals(loggerType.FullName));
                Assert.Contains(testContainer.LogEntries, i => i.Message == message);
                Assert.Single(testContainer.LogEntries);
            }
            else
            {
                Assert.Empty(testContainer.LogEntries);
            }
        }

        if (iLoggerEnabled && currentLevel != Level.Off)
            Assert.Contains(testContainer.LogEntries, i => i.Logger == loggerType.FullName);
    }

    private TestContainer<LogEntryLoggerStub, LogEntry> GetLogEntryTestContainer(bool iLoggerEnabled,
        Action<Log4NetCaptureBuilder>? action = null)
    {
        return new TestContainer<LogEntryLoggerStub, LogEntry>(iLoggerEnabled, TestOutputHelper, config =>
        {
            var serializedLayout = new SerializedLayout();
            serializedLayout.AddRenderer(new JsonDotNetRenderer
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented
            });
            //serializedLayout.AddDefault("");
            //serializedLayout.AddRemove("message");
            //serializedLayout.AddRemove("message:messageobject");
            serializedLayout.ActivateOptions();
            config.WithLayout(serializedLayout);
            config.WithRootAppenderLevel(Level.All)
                .WithAppenderLevel(Level.All);
            action?.Invoke(config);
        });
    }


    [Fact]
    public void TestSimplestMessageLayout()
    {
        var testContainer =
            new TestContainer<StringLoggerStub, string>(true, TestOutputHelper, i => i.WithMessageOnlyLayout());
        testContainer.ServiceProvider.StartLog4NetCapture();
        var logger = LogManager.GetLogger(typeof(string));
        logger.Info(message);
        Assert.NotNull(testContainer.LogEntries);
        Assert.NotEmpty(testContainer.LogEntries);
        Assert.Single(testContainer.LogEntries);
        Assert.Equal(message, testContainer.LogEntries.First());
    }
    [Fact]
    public void TestWrapperFlushing()
    {
        var testContainer =
            new TestContainer<StringLoggerStub, string>(true, TestOutputHelper);
        testContainer.ServiceProvider.StartLog4NetCapture();
        var logger = LogManager.GetLogger(typeof(string));
        logger.Info(message);
        Assert.NotNull(testContainer.LogEntries);
        Assert.NotEmpty(testContainer.LogEntries);
        Assert.Single(testContainer.LogEntries);
        Assert.Equal(message, testContainer.LogEntries.First());
        var z = testContainer.ServiceProvider.GetRequiredService<IMicrosoftExtensionsLoggingScopeManager>();
        Assert.Equal(1, z.GetLoggerScopeWrapperCount());
        Thread.Sleep(MicrosoftExtensionsLoggingScopeManager.CleanupInterval.Add(TimeSpan.FromSeconds(5)));
        Assert.Equal(0, z.GetLoggerScopeWrapperCount());
    }

    [Fact]
    public void TestWhenRenderingThrowsException()
    {
        var mockILayout = new Mock<ILayout>();
        mockILayout.Setup(i => i.Format(It.IsAny<TextWriter>(), It.IsAny<LoggingEvent>()))
            .Throws(() => new InvalidOperationException());
        mockILayout.Setup(i => i.IgnoresException).Returns(false);
        var testContainer =
            new TestContainer<StringLoggerStub, string>(true, TestOutputHelper, i => i.WithLayout(mockILayout.Object));
        testContainer.ServiceProvider.StartLog4NetCapture();
        var logger = LogManager.GetLogger(typeof(string));
        logger.Info(message);
        Assert.NotNull(testContainer.LogEntries);
        Assert.NotEmpty(testContainer.LogEntries);
        Assert.Single(testContainer.LogEntries);
        Assert.Equal(MicrosoftExtensionsLoggingAppender.ErrorRenderingMessage, testContainer.LogEntries.First());
    }

    [Theory]
    [InlineData(typeof(string))]
    [InlineData(typeof(int))]
    [InlineData(typeof(IDisposable))]
    [InlineData(typeof(Nullable<>))]
    [InlineData(typeof(bool))]
    public void TestCustomLayout(Type type)
    {
        var testContainer =
            new TestContainer<StringLoggerStub, string>(true, TestOutputHelper,
                i => i.WithLayout(new PatternLayout("%logger %message")));
        testContainer.ServiceProvider.StartLog4NetCapture();
        var logger = LogManager.GetLogger(type);
        logger.Info(message);
        Assert.NotNull(testContainer.LogEntries);
        Assert.NotEmpty(testContainer.LogEntries);
        Assert.Single(testContainer.LogEntries);
        Assert.Equal($"{type.FullName} {message}", testContainer.LogEntries.First());
    }

    [Fact]
    public void TestFilterThrowsExceptionWhenNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            var testContainer = new TestContainer<StringLoggerStub, string>(true, TestOutputHelper, i => i
                .WithFilters(null));
        });
    }

    [Fact]
    public void TestNullLayoutThrowsException()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            var testContainer = new TestContainer<StringLoggerStub, string>(true, TestOutputHelper, i => i
                .WithLayout(null));
        });
    }

    [Fact]
    public void TestFilter()
    {
        var testContainer = new TestContainer<StringLoggerStub, string>(true, TestOutputHelper, i => i
            .WithMessageOnlyLayout()
            .WithFilters(
                new LoggerMatchFilter { LoggerToMatch = typeof(string).FullName, AcceptOnMatch = false },
                new LoggerMatchFilter { LoggerToMatch = typeof(int).FullName, AcceptOnMatch = true },
                new DenyAllFilter()
            ));
        testContainer.ServiceProvider.StartLog4NetCapture();
        LogManager.GetLogger(typeof(string)).Info("Item1");
        LogManager.GetLogger(typeof(int)).Info("Item2");
        LogManager.GetLogger(typeof(decimal)).Info("Item3");

        Assert.NotNull(testContainer.LogEntries);
        Assert.NotEmpty(testContainer.LogEntries);
        Assert.Single(testContainer.LogEntries);
        Assert.Equal("Item2", testContainer.LogEntries.First());
    }

    [Fact]
    public void TestLogException()
    {
        var testContainer = GetLogEntryTestContainer(true, config => { config.WithUseScopes(true); });
        var serviceProvider = testContainer.ServiceProvider;
        serviceProvider.StartLog4NetCapture();

        var log = LogManager.GetLogger(typeof(string));
        Exception? lastException;
        try
        {
            throw new InvalidOperationException("la la la");
        }
        catch (Exception ex)
        {
            lastException = ex;
            log.Error("zulu", ex);
        }

        Assert.NotNull(testContainer.LogEntries);
        Assert.NotEmpty(testContainer.LogEntries);
        Assert.Single(testContainer.LogEntries);
        Assert.True(testContainer.LogEntries.First().EventualPassedException == lastException);
    }

    [Fact]
    public void TestUseScopesWrong()
    {
        var toCompare = new List<LogResult>
        {
            new("zulu", "(null)", null),
            new("lima", "abc", "abc"),
            new("mike", "abc def", "abc.def"),
            new("november", "(null)", null),
            new("oscar", "(null)", null)
        };
        var testContainer = GetLogEntryTestContainer(true, config => { config.WithUseScopes(true); });
        var serviceProvider = testContainer.ServiceProvider;
        serviceProvider.StartLog4NetCapture();

        var log = LogManager.GetLogger(typeof(string));
        log.Debug("zulu");
        var scope = ThreadContext.Stacks["NDC"].Push("abc");
        log.Debug("lima");
        var scope2 = ThreadContext.Stacks["NDC"].Push("def");
        log.Debug("mike");
        scope.Dispose();
        log.Debug("november");
        scope2.Dispose();
        log.Debug("oscar");
        DoCompares(true, toCompare, testContainer);
    }


    [Fact]
    public void TestScopesWithMultipleThreads()
    {
        var testContainer = GetLogEntryTestContainer(true, config => { config.WithUseScopes(true); });
        testContainer.ServiceProvider.StartLog4NetCapture();
        var types = new List<Type>
        {
            typeof(IDisposable), typeof(string), typeof(StringBuilder), typeof(IDisposable), typeof(Thread),
            typeof(Type), typeof(KeyValuePair), typeof(KeyNotFoundException)
        };

        types = new List<Type>
        {
            typeof(string), typeof(string)
        };
        var threads = new List<Thread>();
        const string singleScopePrefix = "SINGLESCOPE|";
        const string doubleScopePrefix = "DOUBLESCOPE|";
        const string threadIdPrefix = "THREADID|";
        const string scope1Suffix = "Context";
        const string scope2Suffix = "InnerContext";

        void MyAction(object? arg)
        {
            var threadStartInfo = arg as MyThreadStartInfo;

            var logger = LogManager.GetLogger(threadStartInfo.Type);
            logger.Info($"{threadIdPrefix}{Thread.CurrentThread.ManagedThreadId}");
            using (var scope = ThreadContext.Stacks["NDC"].Push(threadStartInfo.Type.FullName + scope1Suffix))
            {
                var r = new Random();
                for (var i = 0; i < r.Next(5, 10); i++)
                {
                    if (r.NextDouble() < .5d)
                        using (var scope2 = ThreadContext.Stacks["NDC"].Push(scope2Suffix))
                        {
                            logger.Info($"{doubleScopePrefix}{Thread.CurrentThread.ManagedThreadId}");
                        }
                    else
                        logger.Info($"{singleScopePrefix}{Thread.CurrentThread.ManagedThreadId}");

                    Thread.Sleep(r.Next(100, 1000));
                }
            }
        }

        foreach (var _ in types) threads.Add(new Thread(MyAction));

        for (var i = 0; i < types.Count; i++) threads[i].Start(new MyThreadStartInfo { Type = types[i] });

        foreach (var thread in threads) thread.Join();


        foreach (var type in types)
        {
            var p1 = testContainer.LogEntries.Where(i => i.Logger == type.FullName).OrderBy(i => i.Date)
                .GroupBy(i => i.Thread).ToList();
            foreach (var p2 in p1)
            {
                var p = p2.ToList();
                Assert.NotEmpty(p);
                Assert.StartsWith(threadIdPrefix, p.First().Message);
                var threadid = p.First().Message.Substring(threadIdPrefix.Length);
                Assert.True(p.All(i => i.Thread == threadid));
                Assert.Contains(p, i => i.Scope != null && i.Scope.StartsWith(type.FullName + scope1Suffix));
                Assert.True(p.Where(i => i.Scope != null).All(i => i.Scope.StartsWith(type.FullName + scope1Suffix)));
                var doubleScopeEntries = p.Where(i => i.Scope != null && i.Message.StartsWith($"{doubleScopePrefix}"))
                    .ToList();
                Assert.True(
                    doubleScopeEntries.All(i => i.Scope.Equals(type.FullName + $"{scope1Suffix}.{scope2Suffix}")));
                Assert.True(doubleScopeEntries.All(i => i.Message.EndsWith(threadid)));
                Assert.True(p.Where(i => i.Scope != null && i.Message.StartsWith($"{singleScopePrefix}"))
                    .All(i => i.Message.EndsWith(threadid)));
            }
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TestUseScopes(bool withScope)
    {
        var toCompare = new List<LogResult>
        {
            new("zulu", "(null)", null),
            new("alpha", "context", "context"),
            new("bravo", "context context2", "context.context2"),
            new("charlie", "context context2 context3", "context.context2.context3"),
            new("delta", "context context4", "context.context4"),
            new("echo", "(null)", null)
        };
        var testContainer = LogWithScopes(withScope);

        Assert.NotNull(testContainer.LogEntries);
        Assert.NotEmpty(testContainer.LogEntries);
        Assert.Equal(6, testContainer.LogEntries.Count);
        DoCompares(withScope, toCompare, testContainer);
    }

    private static void DoCompares(bool withScope, List<LogResult> toCompare,
        TestContainer<LogEntryLoggerStub, LogEntry> testContainer)
    {
        for (var i = 0; i < toCompare.Count; i++)
        {
            var item = toCompare[i];
            if (testContainer.LogEntries.Count > i)
            {
                var logEntry = testContainer.LogEntries[i];
                Assert.Equal(item.Message, logEntry.Message);

                Assert.Equal(item.NDC, logEntry.NDC);
                if (withScope)
                    Assert.Equal(item.Scope, logEntry.Scope);
                else
                    Assert.Null(logEntry.Scope);
            }
        }
    }


    private TestContainer<LogEntryLoggerStub, LogEntry> LogWithScopes(bool withScopes)
    {
        var testContainer = GetLogEntryTestContainer(true, config => { config.WithUseScopes(withScopes); });
        testContainer.ServiceProvider.StartLog4NetCapture();

        var logger = LogManager.GetLogger(typeof(string));
        logger.Debug("zulu");

        using (ThreadContext.Stacks["NDC"].Push("context"))
        {
            logger.Debug("alpha");
            using (ThreadContext.Stacks["NDC"].Push("context2"))
            {
                logger.Debug("bravo");
                using (ThreadContext.Stacks["NDC"].Push("context3"))
                {
                    logger.Debug("charlie");
                }
            }

            using (ThreadContext.Stacks["NDC"].Push("context4"))
            {
                logger.Debug("delta");
            }
        }

        logger.Debug("echo");
        return testContainer;
    }

    private class MyThreadStartInfo
    {
        public Type Type { get; set; }
    }

    public class LogResult : LogEntry
    {
        public LogResult(string? message, string? ndc, string? scope)
        {
            Message = message;
            NDC = ndc;
            Scope = scope;
        }
    }

    private class TestContainer<T, U> where T : LoggerStubBase<U>, new()
    {
        /// <summary>
        /// </summary>
        /// <param name="iLoggerEnabled">The internal ILogger instance is enabled for logging</param>
        /// <param name="action"></param>
        public TestContainer(bool iLoggerEnabled, ITestOutputHelper testOutputHelper,
            Action<Log4NetCaptureBuilder>? action = null)
        {
            LoggerProviderMock.Setup(_createLoggerExpression).Returns((string categoryName) =>
            {
                if (LoggerStubs.ContainsKey(categoryName)) return LoggerStubs[categoryName];
                var loggerStub = new T
                {
                    Enabled = iLoggerEnabled,
                    Items = LogEntries
                };
                LoggerStubs[categoryName] = loggerStub;
                return loggerStub;
            });

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddConfiguration();
                loggingBuilder.Services.TryAddEnumerable(ServiceDescriptor.Singleton(LoggerProviderMock.Object));
                loggingBuilder.SetMinimumLevel(LogLevel.Trace);
            });
            serviceCollection.AddLog4NetCapture(i =>
            {

                action?.Invoke(i);
                i.WithInternalLogger(new TestInternalLogger(testOutputHelper));
            });

            ServiceProvider = serviceCollection.BuildServiceProvider();
        }

        public IServiceProvider ServiceProvider { get; }
        public List<U> LogEntries { get; } = new();

        public Dictionary<string, T> LoggerStubs { get; } = new();

        public Mock<ILoggerProvider> LoggerProviderMock { get; } = new();
    }
}

internal class TestInternalLogger : IInternalLogger
{
    private readonly ITestOutputHelper _testOutputHelper;

    public TestInternalLogger(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        _testOutputHelper.WriteLine($"{DateTime.Now.ToString("O")} [{logLevel}] {Thread.CurrentThread.ManagedThreadId} {formatter(state, exception)}");
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        throw new NotImplementedException();
    }
}