using System;
using log4net;
using log4net.Repository.Hierarchy;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Logging.Log4NetCapture
{
    public static class ServiceProviderExtensions
    {
        public static IServiceCollection AddLog4NetCapture(this IServiceCollection serviceCollection,
            Action<Log4NetCaptureBuilder> action)
        {
            var builder = new Log4NetCaptureBuilder();
            action?.Invoke(builder);
            var configuration = builder.Build();
            serviceCollection.AddSingleton(configuration);
            serviceCollection.AddSingleton<ILogLevelMapper, LogLevelMapper>();
            return serviceCollection;
        }

        public static IServiceProvider CaptureLog4Net(this IServiceProvider serviceProvider)
        {
            var logLevelMapper = serviceProvider.GetRequiredService<ILogLevelMapper>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var configuration = serviceProvider.GetService<Log4NetCaptureConfiguration>() ??
                                new Log4NetCaptureConfiguration();
            var appender = new MicrosoftExtensionsLoggingAppender(loggerFactory, configuration,
                serviceProvider.GetRequiredService<ILogger<MicrosoftExtensionsLoggingAppender>>(), logLevelMapper);
            appender.ActivateOptions();
            var hierarchy = (Hierarchy)LogManager.GetRepository();
            hierarchy.Root.AddAppender(appender);
            if (configuration.RootAppenderLevel != null) hierarchy.Root.Level = configuration.RootAppenderLevel;

            hierarchy.Configured = true;
            return serviceProvider;
        }
    }
}