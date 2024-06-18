using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Timers;
using log4net.Core;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using Timer = System.Timers.Timer;

namespace Snork.Log4NetCapture
{
    public class MicrosoftExtensionsLoggingScopeManager : IMicrosoftExtensionsLoggingScopeManager, IDisposable
    {
        public static readonly TimeSpan CleanupInterval = TimeSpan.FromSeconds(30);
        private readonly IInternalLogger _internalLogger;

        private readonly ConcurrentDictionary<(ILogger, int), LoggerScopeWrapper> _loggerScopeWrappers =
            new ConcurrentDictionary<(ILogger, int), LoggerScopeWrapper>();

        private readonly Timer _timer = new Timer { Interval = CleanupInterval.TotalMilliseconds, AutoReset = true };

        public MicrosoftExtensionsLoggingScopeManager(IInternalLogger internalLogger)
        {
            _internalLogger = internalLogger;
            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
        }

        public int GetLoggerScopeWrapperCount()
        {
            return _loggerScopeWrappers.Count;
        }

        public void AdjustScope(LoggingEvent loggingEvent, ILogger logger)
        {
            lock (logger)
            {
                var key = (logger, Thread.CurrentThread.ManagedThreadId);
                if (_internalLogger.IsEnabled(LogLevel.Debug))
                    _internalLogger.LogDebug($"Finding dictionary item for {loggingEvent.LoggerName}");
                var loggerScopeWrapper = _loggerScopeWrappers.GetOrAdd(key, _ =>
                {
                    if (_internalLogger.IsEnabled(LogLevel.Debug))
                        _internalLogger.LogDebug($"Creating dictionary item for {loggingEvent.LoggerName}");
                    return new LoggerScopeWrapper(logger, _internalLogger, loggingEvent.LoggerName);
                });

                loggerScopeWrapper.AdjustScope(loggingEvent);
            }
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_internalLogger.IsEnabled(LogLevel.Debug))
                _internalLogger.LogDebug("Cleanup timer elapsed");
            lock (_loggerScopeWrappers)
            {
                foreach (var key in _loggerScopeWrappers.Keys)
                {
                    var scopeWrapper = _loggerScopeWrappers[key];
                    if (scopeWrapper.CanDiscard())
                    {
                        if (_internalLogger.IsEnabled(LogLevel.Debug))
                            _internalLogger.LogDebug($"Discarding dictionary item for {scopeWrapper.LoggerName}");
                        _loggerScopeWrappers.Remove(key, out _);
                    }
                }
            }
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}