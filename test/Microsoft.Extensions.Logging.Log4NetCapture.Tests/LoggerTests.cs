using System.Linq.Expressions;
using System.Text;
using log4net;
using log4net.Core;
using log4net.Filter;
using log4net.Layout;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Configuration;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Logging.Log4NetCapture.Tests;

public class LoggerTests
{
    private const string message =
        "Integer est tortor, lobortis ac leo nec, sollicitudin porttitor metus. Nunc molestie ornare mi, vitae ultrices mi bibendum sed. Donec venenatis volutpat nibh eu blandit. Aenean laoreet nulla turpis, a sollicitudin lorem mollis ac. Curabitur et tempor lectus. Curabitur placerat vel felis sit amet luctus. Donec eu mauris egestas, semper sapien a, lacinia leo. Donec faucibus egestas nisl vitae tincidunt. Ut lorem lorem, posuere ac mollis at, sagittis at lacus. Mauris sollicitudin sit amet nibh et luctus.";

    private static readonly Expression<Func<ILoggerProvider, ILogger>> _createLoggerExpression =
        i => i.CreateLogger(It.IsAny<string>());

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
    public void TestProvider(KnownLevelEnum logLevel, bool iLoggerEnabled, Type loggerType)
    {
        var testContainer = GetLogEntryTestContainer(iLoggerEnabled);
        var startDate = DateTime.Now;
        var dict = testContainer.LoggerStubs;
        var loggerProviderMock = testContainer.LoggerProviderMock;
        var messages = testContainer.LogEntries;

        var serviceProvider = testContainer.ServiceProvider;
        serviceProvider.StartLog4NetCapture();
        var log = LogManager.GetLogger(loggerType);

        var times = Times.Once();
        Level? currentLevel = null;
        switch (logLevel)
        {
            case KnownLevelEnum.Warn:
                log.Warn("abc");
                break;
            case KnownLevelEnum.Critical:
                log.Fatal("abc");
                break;
            case KnownLevelEnum.Info:
                log.Info("abc");

                break;
            case KnownLevelEnum.Error:
                log.Error("abc");
                log.Error("def", new InvalidCastException());
                times = Times.Exactly(2);
                break;
            case KnownLevelEnum.Debug:
                log.Debug("abc");
                break;
            //case KnownLevelEnum.Off:
            //    times = Times.Never();
            //    break;
            default:
                currentLevel = KnownLevelMapper.GetLevel(logLevel);
                break;
        }

        if (currentLevel != null)
            log.Logger.Log(new LoggingEvent(loggerType, log.Logger.Repository,
                new LoggingEventData { LoggerName = log.Logger.Name, Level = currentLevel, Message = "abc" }));
        //  log2.Debug("ok");

        var loggerCreated = dict.ContainsKey(loggerType.FullName);

        if (logLevel == KnownLevelEnum.Off)
        {
            Assert.False(loggerCreated);
            //Assert.Empty(messages);
        }
        else
        {
            Assert.True(loggerCreated);
            var logger = dict[loggerType.FullName];
            loggerProviderMock.Verify(_createLoggerExpression, Times.AtLeastOnce);
            if (iLoggerEnabled)
                times.Validate(logger.LogCount);
            else
                Times.Never().Validate(logger.LogCount);
            //Assert.Equal(logger.LogCount, iLoggerEnabled ? times.Validate(logger.LogCount) : Times.Never());
            if (iLoggerEnabled)
            {
                Assert.NotEmpty(messages);
                if (currentLevel != null) Assert.Contains(messages, i => i.Level.Equals(currentLevel.DisplayName));
                Assert.Contains(messages, i => i.Logger.Equals(loggerType.FullName));
                Assert.Contains(messages, i => i.Date >= startDate);
            }
            else
            {
                Assert.Empty(messages);
            }
        }

        if (iLoggerEnabled && currentLevel != Level.Off)
            Assert.Contains(messages, i => i.Logger == loggerType.FullName);
    }

    private static TestContainer<LogEntryLoggerStub, LogEntry> GetLogEntryTestContainer(bool iLoggerEnabled,
        Action<Log4NetCaptureBuilder>? action = null)
    {
        return new TestContainer<LogEntryLoggerStub, LogEntry>(iLoggerEnabled, config =>
        {
            var serializedLayout = new SerializedLayout();
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
        var testContainer = new TestContainer<StringLoggerStub, string>(true, i => i.WithMessageOnlyLayout());
        var serviceProvider = testContainer.ServiceProvider;
        serviceProvider.StartLog4NetCapture();
        var mylog = LogManager.GetLogger(typeof(string));
        mylog.Info(message);
        Assert.NotNull(testContainer.LogEntries);
        Assert.NotEmpty(testContainer.LogEntries);
        Assert.Single(testContainer.LogEntries);
        Assert.Equal(message, testContainer.LogEntries.First());
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
            new TestContainer<StringLoggerStub, string>(true, i => i.WithLayout(new PatternLayout("%logger %message")));
        var serviceProvider = testContainer.ServiceProvider;
        serviceProvider.StartLog4NetCapture();
        var mylog = LogManager.GetLogger(type);
        mylog.Info(message);
        Assert.NotNull(testContainer.LogEntries);
        Assert.NotEmpty(testContainer.LogEntries);
        Assert.Single(testContainer.LogEntries);
        Assert.Equal($"{type.FullName} {message}", testContainer.LogEntries.First());
    }

    [Fact]
    public void TestFilter()
    {
        var testContainer = new TestContainer<StringLoggerStub, string>(true, i => i.WithMessageOnlyLayout()
            .WithFilters(
                new LoggerMatchFilter { LoggerToMatch = typeof(string).FullName, AcceptOnMatch = false },
                new LoggerMatchFilter { LoggerToMatch = typeof(int).FullName, AcceptOnMatch = true },
                new DenyAllFilter()
            ));
        var serviceProvider = testContainer.ServiceProvider;
        serviceProvider.StartLog4NetCapture();
        LogManager.GetLogger(typeof(string)).Info("Item1");
        LogManager.GetLogger(typeof(int)).Info("Item2");
        LogManager.GetLogger(typeof(decimal)).Info("Item3");

        Assert.NotNull(testContainer.LogEntries);
        Assert.NotEmpty(testContainer.LogEntries);
        Assert.Single(testContainer.LogEntries);
        Assert.Equal("Item2", testContainer.LogEntries.First());
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


    private static TestContainer<LogEntryLoggerStub, LogEntry> LogWithScopes(bool withScopes)
    {
        var testContainer = GetLogEntryTestContainer(true, config => { config.WithUseScopes(withScopes); });
        var serviceProvider = testContainer.ServiceProvider;
        serviceProvider.StartLog4NetCapture();

        var log = LogManager.GetLogger(typeof(string));
        log.Debug("zulu");

        using (ThreadContext.Stacks["NDC"].Push("context"))
        {
            log.Debug("alpha");
            using (ThreadContext.Stacks["NDC"].Push("context2"))
            {
                log.Debug("bravo");
                using (ThreadContext.Stacks["NDC"].Push("context3"))
                {
                    log.Debug("charlie");
                }
            }

            using (ThreadContext.Stacks["NDC"].Push("context4"))
            {
                log.Debug("delta");
            }
        }

        log.Debug("echo");
        return testContainer;
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
        public TestContainer(bool iLoggerEnabled, Action<Log4NetCaptureBuilder>? action = null)
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
            serviceCollection.AddLog4NetCapture(action);

            ServiceProvider = serviceCollection.BuildServiceProvider();
        }

        public IServiceProvider ServiceProvider { get; }
        public List<U> LogEntries { get; } = new();

        public Dictionary<string, T> LoggerStubs { get; } = new();

        public Mock<ILoggerProvider> LoggerProviderMock { get; } = new();
    }
}