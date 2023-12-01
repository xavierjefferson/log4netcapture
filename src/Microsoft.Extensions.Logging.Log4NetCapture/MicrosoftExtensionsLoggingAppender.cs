using System;
using System.Collections.Generic;
using System.Linq;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Util;

namespace Microsoft.Extensions.Logging.Log4NetCapture
{
    public class MicrosoftExtensionsLoggingAppender : AppenderSkeleton
    {
        private readonly ILogger<MicrosoftExtensionsLoggingAppender> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogLevelMapper _logLevelMapper;

        private readonly Stack<IDisposable> _scopeStack = new Stack<IDisposable>();
        private readonly bool _useScopes;
        private int _lastContextLength;
        private string? _lastContextName;

        public MicrosoftExtensionsLoggingAppender(ILoggerFactory loggerFactory,
            Log4NetCaptureConfiguration configuration, ILogger<MicrosoftExtensionsLoggingAppender> logger,
            ILogLevelMapper logLevelMapper)
        {
            _logLevelMapper = logLevelMapper;
            _logger = logger;
            _loggerFactory = loggerFactory;
            _useScopes = configuration.UseScopes;
            Threshold = configuration.AppenderLevel ?? Level.All;
            Layout = configuration.Layout ?? new SimpleLayout();
            if (configuration.Filters != null && configuration.Filters.Any())
                foreach (var filter in configuration.Filters)
                    AddFilter(filter);
        }

        private void AdjustScope(LoggingEvent loggingEvent, ILogger logger)
        {
            var properties = loggingEvent.GetProperties();

            var action = ActionEnum.None;
            string? toPush = null;
            if (properties.Contains("NDC"))
            {
                var threadContextStack = properties["NDC"] as ThreadContextStack;
                if (threadContextStack == null)
                {
                    action = ActionEnum.PopAll;
                }
                else
                {
                    var currentContextName = threadContextStack.ToString();
                    if (threadContextStack.Count == 0 && _scopeStack.Any())
                        action = ActionEnum.PopAll;
                    else if (threadContextStack.Count > 0 && _lastContextName == null)
                        action = ActionEnum.BeginScope;
                    else if (_lastContextName != null && threadContextStack.Count > _lastContextLength &&
                             !_lastContextName.Equals(currentContextName))
                        action = ActionEnum.BeginScope;
                    else if (_lastContextName != null && threadContextStack.Count < _lastContextLength) action = ActionEnum.PopSingle;

                    if (action == ActionEnum.BeginScope)
                    {
                        if (_lastContextName != null && currentContextName.StartsWith(_lastContextName) &&
                            _lastContextName.Length < currentContextName.Length)
                            toPush = currentContextName.Substring(_lastContextName.Length).Trim();
                        else
                            toPush = currentContextName.Trim();
                    }

                    _lastContextLength = threadContextStack.Count;
                    _lastContextName = currentContextName;
                }
            }
            else
            {
                action = ActionEnum.PopAll;
            }

            switch (action)
            {
                case ActionEnum.BeginScope:
                    _scopeStack.Push(logger.BeginScope(toPush));
                    break;
                case ActionEnum.PopSingle:
                    if (_scopeStack.Any()) _scopeStack.Pop().Dispose();

                    break;
                case ActionEnum.PopAll:
                    while (_scopeStack.Any()) _scopeStack.Pop().Dispose();
                    _lastContextName = null;
                    _lastContextLength = 0;

                    break;
            }
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            var logLevel = _logLevelMapper.Map(loggingEvent.Level);
            if (logLevel == LogLevel.None) return;
            if (string.IsNullOrWhiteSpace(loggingEvent.LoggerName)) return;

            var logger = _loggerFactory.CreateLogger(loggingEvent.LoggerName);
            if (_useScopes) AdjustScope(loggingEvent, logger);
            if (logger.IsEnabled(logLevel))
            {
                string? rendered;
                try
                {
                    rendered = RenderLoggingEvent(loggingEvent);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error rendering Log4Net event");
                    return;
                }

                logger.Log(logLevel, loggingEvent.ExceptionObject, rendered);
            }
        }

        private enum ActionEnum
        {
            None = 0,
            BeginScope,
            PopSingle,
            PopAll
        }
    }
}