using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Util;

namespace Microsoft.Extensions.Logging.Log4NetCapture
{
    public class MicrosoftExtensionsLoggingAppender : AppenderSkeleton
    {
        private static readonly FieldInfo? StackField;
        private readonly ILogger<MicrosoftExtensionsLoggingAppender> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogLevelMapper _logLevelMapper;

        private readonly Stack<StackItem> _scopeStack = new Stack<StackItem>();

        private readonly bool _useScopes;


        static MicrosoftExtensionsLoggingAppender()
        {
            var type = typeof(ThreadContextStack);
            StackField = type.GetField("m_stack",
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.NonPublic);
        }


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

            if (properties.Contains("NDC"))
            {
                var threadContextStack = properties["NDC"] as ThreadContextStack;
                if (threadContextStack == null)
                {
                    PopAll();
                }
                else
                {
                    var items = GetStackFrameInfos(threadContextStack);
                    if (items == null || items.Count == 0)
                    {
                        PopAll();
                        return;
                    }

                    var startAt = 0;
                    var stackItems = _scopeStack.Reverse().ToList();
                    for (var i = 0; i < Math.Min(stackItems.Count, items.Count); i++)
                        if (stackItems[i].Info.Message == items[i].Message &&
                            stackItems[i].Info.Parent == items[i].Parent)
                            startAt = i + 1;
                        else
                            break;

                    for (var i = 0; i < stackItems.Count - startAt; i++)
                        if (_scopeStack.Any())
                            _scopeStack.Pop().LoggerScope.Dispose();

                    for (var i = startAt; i < items.Count; i++)
                    {
                        var item = items[i];
                        _scopeStack.Push(new StackItem(logger.BeginScope(item.Message), item));
                    }
                }
            }
            else
            {
                PopAll();
            }
        }

        private void PopAll()
        {
            while (_scopeStack.Any()) _scopeStack.Pop().LoggerScope.Dispose();
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

        /// <summary>
        ///     this works by using reflection on private log4net classes
        ///     if the class structures change, it will give up and return null
        /// </summary>
        /// <param name="threadContextStack"></param>
        /// <returns></returns>
        private List<StackFrameInfo>? GetStackFrameInfos(ThreadContextStack threadContextStack)
        {
            if (StackField == null)
                return null;
            if (StackField.FieldType != typeof(Stack)) return null;


            var stack = StackField.GetValue(threadContextStack) as Stack;
            if (stack != null)
            {
                var items = new List<StackFrameInfo>();
                Type? frameType = null;
                PropertyInfo? messageProperty = null;
                FieldInfo? parentField = null;
                foreach (var frame in stack)
                {
                    if (frame == null) return null;
                    var type = frame.GetType();
                    if (type != frameType)
                    {
                        frameType = type;
                        messageProperty = frameType.GetProperty("Message",
                            BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                        if (messageProperty == null) return null;
                        parentField = frameType.GetField("m_parent",
                            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase |
                            BindingFlags.NonPublic);
                        if (parentField == null) return null;
                    }

                    items.Insert(0,
                        new StackFrameInfo(messageProperty.GetValue(frame) as string, parentField.GetValue(frame)));
                }

                return items;
            }

            return null;
        }

        private class StackItem
        {
            public StackItem(IDisposable loggerScope, StackFrameInfo info)
            {
                LoggerScope = loggerScope;
                Info = info;
            }

            public IDisposable LoggerScope { get; }
            public StackFrameInfo Info { get; }
        }

        private class StackFrameInfo
        {
            public StackFrameInfo(string message, object parent)
            {
                Message = message;
                Parent = parent;
            }

            public string Message { get; }
            public object Parent { get; }
        }
    }
}