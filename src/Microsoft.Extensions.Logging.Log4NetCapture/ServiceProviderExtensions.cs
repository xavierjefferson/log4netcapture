using System;
using log4net;
using log4net.Repository.Hierarchy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Snork.Log4NetCapture
{
    public static class ServiceProviderExtensions
    {
        public static IServiceCollection AddLog4NetCapture(this IServiceCollection serviceCollection,
            Action<Log4NetCaptureBuilder>? action = null)
        {
            var builder = new Log4NetCaptureBuilder();
            action?.Invoke(builder);
            var configuration = builder.Build();
            serviceCollection.AddSingleton(configuration);
            serviceCollection.AddSingleton<ILogLevelMapper, LogLevelMapper>();
            if (configuration.InternalLogger == null)
                serviceCollection.AddSingleton<IInternalLogger, InternalLoggerStub>();
            else
                serviceCollection.AddSingleton(configuration.InternalLogger);
            serviceCollection
                .AddSingleton<IMicrosoftExtensionsLoggingScopeManager, MicrosoftExtensionsLoggingScopeManager>();
            serviceCollection.AddSingleton<MicrosoftExtensionsLoggingAppender>();
            return serviceCollection;
        }

        public static IServiceProvider StartLog4NetCapture(this IServiceProvider serviceProvider)
        {
            //var logLevelMapper = serviceProvider.GetRequiredService<ILogLevelMapper>();
            //var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var configuration = serviceProvider.GetService<Log4NetCaptureConfiguration>() ??
                                new Log4NetCaptureConfiguration();
            //var appender = new MicrosoftExtensionsLoggingAppender(loggerFactory, configuration,
            //    serviceProvider.GetRequiredService<ILogger<MicrosoftExtensionsLoggingAppender>>(), logLevelMapper,
            //    serviceProvider.GetRequiredService<IMicrosoftExtensionsLoggingScopeManager>());
            var appender = serviceProvider.GetRequiredService<MicrosoftExtensionsLoggingAppender>();
            appender.ActivateOptions();
            var hierarchy = (Hierarchy)LogManager.GetRepository();
            hierarchy.Root.AddAppender(appender);
            if (configuration.RootAppenderLevel != null) hierarchy.Root.Level = configuration.RootAppenderLevel;

            hierarchy.Configured = true;
            return serviceProvider;
        }

        private class InternalLoggerStub : IInternalLogger
        {
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return false;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                throw new NotImplementedException();
            }
        }
    }
}