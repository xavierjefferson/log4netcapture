using System.Linq.Expressions;
using log4net;
using log4net.Core;
using log4net.Layout;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Configuration;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Logging.Log4NetCapture.Tests;

public class LoggerTests
{
    private readonly Expression<Func<ILoggerProvider, ILogger>> _createLoggerExpression =
        i => i.CreateLogger(It.IsAny<string>());


    [Theory]
    [InlineData(KnownLevelEnum.Alert, false, typeof(float))]
    [InlineData(KnownLevelEnum.Alert, true, typeof(float))]
    [InlineData(KnownLevelEnum.All, false, typeof(float))]
    [InlineData(KnownLevelEnum.All, true, typeof(float))]
    [InlineData(KnownLevelEnum.Critical, false, typeof(Mock<>))]
    [InlineData(KnownLevelEnum.Critical, true, typeof(float))]
    [InlineData(KnownLevelEnum.Debug, false, typeof(Expression))]
    [InlineData(KnownLevelEnum.Debug, true, typeof(string))]
    [InlineData(KnownLevelEnum.Emergency, false, typeof(float))]
    [InlineData(KnownLevelEnum.Emergency, true, typeof(float))]
    [InlineData(KnownLevelEnum.Error, false, typeof(EventId))]
    [InlineData(KnownLevelEnum.Error, true, typeof(Exception))]
    [InlineData(KnownLevelEnum.Fatal, false, typeof(float))]
    [InlineData(KnownLevelEnum.Fatal, true, typeof(float))]
    [InlineData(KnownLevelEnum.Fine, false, typeof(float))]
    [InlineData(KnownLevelEnum.Fine, true, typeof(float))]
    [InlineData(KnownLevelEnum.Finer, false, typeof(float))]
    [InlineData(KnownLevelEnum.Finer, true, typeof(float))]
    [InlineData(KnownLevelEnum.Finest, false, typeof(float))]
    [InlineData(KnownLevelEnum.Finest, true, typeof(float))]
    [InlineData(KnownLevelEnum.Info, false, typeof(KnownLevelEnum))]
    [InlineData(KnownLevelEnum.Info, true, typeof(LoggerTests))]
    [InlineData(KnownLevelEnum.Log4Net_Debug, false, typeof(Mock<>))]
    [InlineData(KnownLevelEnum.Log4Net_Debug, true, typeof(Mock<>))]
    [InlineData(KnownLevelEnum.Notice, false, typeof(float))]
    [InlineData(KnownLevelEnum.Notice, true, typeof(float))]
    [InlineData(KnownLevelEnum.Off, false, typeof(ILogger))]
    [InlineData(KnownLevelEnum.Off, true, typeof(decimal))]
    [InlineData(KnownLevelEnum.Severe, false, typeof(float))]
    [InlineData(KnownLevelEnum.Severe, true, typeof(float))]
    [InlineData(KnownLevelEnum.Trace, false, typeof(Mock<>))]
    [InlineData(KnownLevelEnum.Trace, true, typeof(Mock<>))]
    [InlineData(KnownLevelEnum.Verbose, false, typeof(Mock<>))]
    [InlineData(KnownLevelEnum.Verbose, true, typeof(Mock<>))]
    [InlineData(KnownLevelEnum.Warn, false, typeof(Action))]
    [InlineData(KnownLevelEnum.Warn, true, typeof(int))]
    public void Test1(KnownLevelEnum logLevel, bool iLoggerEnabled, Type loggerType)
    {
        var type2 = typeof(Type);
        var messages = new List<LogEntry>();
        var dict = new Dictionary<string, LoggerStub>();
        var loggerProviderMock = new Mock<ILoggerProvider>();

        loggerProviderMock.Setup(_createLoggerExpression).Returns((string categoryName) =>
        {
            if (dict.ContainsKey(categoryName)) return dict[categoryName];
            var loggerStub = new LoggerStub(iLoggerEnabled, messages);
            dict[categoryName] = loggerStub;
            return loggerStub;
        });

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddConfiguration();
            loggingBuilder.Services.TryAddEnumerable(ServiceDescriptor.Singleton(loggerProviderMock.Object));
            loggingBuilder.SetMinimumLevel(LogLevel.Trace);
        });
        serviceCollection.AddLog4NetCapture(config =>
        {
            var serializedLayout = new SerializedLayout();
            serializedLayout.ActivateOptions();
            config.WithRootAppenderLevel(Level.All).WithLayout(serializedLayout)
                .WithAppenderLevel(Level.Verbose); //.WithPatternLayout("%-5level [%thread]: %message%newline");
        });

        var serviceProvider = serviceCollection.BuildServiceProvider();
        serviceProvider.CaptureLog4Net();
        var log = LogManager.GetLogger(loggerType);
        var log2 = LogManager.GetLogger(type2);
        using (ThreadContext.Stacks["NDC"].Push("context"))
        {
            log2.Debug("Hey hey hey");
            using (ThreadContext.Stacks["NDC"].Push("context2"))
            {
                log2.Debug("It's fat albert");
                using (ThreadContext.Stacks["NDC"].Push("context2"))
                {
                    log2.Debug("It's fat albert3");
                }
            }

            using (ThreadContext.Stacks["NDC"].Push("context2"))
            {
                log2.Debug("It's fat albert34");
            }
        }

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
            }
            else
            {
                Assert.Empty(messages);
            }
        }

        if (iLoggerEnabled) Assert.Contains(messages, i => i.Logger == type2.FullName);
    }
}