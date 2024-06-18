using System;
using System.Linq;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using Microsoft.Extensions.Logging;

namespace Snork.Log4NetCapture
{
    public class MicrosoftExtensionsLoggingAppender : AppenderSkeleton
    {
        public const string ErrorRenderingMessage =
            "Error rendering Log4Net event.  This is usually due to an exception occurring in the configured " +
            nameof(ILayout) + " instance.";

        private readonly IInternalLogger _internalLogger;


        private readonly ILogger<MicrosoftExtensionsLoggingAppender> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogLevelMapper _logLevelMapper;
        private readonly IMicrosoftExtensionsLoggingScopeManager _scopeManager;


        private readonly bool _useScopes;


        public MicrosoftExtensionsLoggingAppender(ILoggerFactory loggerFactory,
            Log4NetCaptureConfiguration configuration, ILogger<MicrosoftExtensionsLoggingAppender> logger,
            ILogLevelMapper logLevelMapper, IMicrosoftExtensionsLoggingScopeManager scopeManager,
            IInternalLogger internalLogger)
        {
            _logLevelMapper = logLevelMapper;
            _scopeManager = scopeManager;
            _internalLogger = internalLogger;
            _logger = logger;
            _loggerFactory = loggerFactory;
            _useScopes = configuration.UseScopes;
            Threshold = configuration.AppenderLevel ?? Level.All;
            Layout = configuration.Layout ?? new SimpleLayout();
            if (configuration.Filters != null && configuration.Filters.Any())
                foreach (var filter in configuration.Filters)
                    AddFilter(filter);
        }


        protected override void Append(LoggingEvent loggingEvent)
        {
            var logLevel = _logLevelMapper.Map(loggingEvent.Level);
            if (logLevel == LogLevel.None) return;

            var logger = _loggerFactory.CreateLogger(loggingEvent.LoggerName);

            if (_useScopes) _scopeManager.AdjustScope(loggingEvent, logger);


            if (logger.IsEnabled(logLevel))
            {
                string? rendered;
                try
                {
                    rendered = RenderLoggingEvent(loggingEvent);
                }
                catch (Exception ex)
                {
                    _internalLogger.LogError(ex, ErrorRenderingMessage);
                    _logger.LogError(ex, ErrorRenderingMessage);
                    return;
                }

                logger.Log(logLevel, loggingEvent.ExceptionObject, rendered);
            }
        }
    }
}