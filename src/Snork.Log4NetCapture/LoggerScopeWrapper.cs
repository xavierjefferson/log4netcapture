using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using log4net.Core;
using log4net.Util;
using Microsoft.Extensions.Logging;
using Snork.Log4NetCapture.Models;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Snork.Log4NetCapture
{
    public class LoggerScopeWrapper
    {
        private static readonly FieldInfo? StackField;
        private readonly IInternalLogger _internalLogger;
        public string LoggerName { get; }
        private readonly Stack<StackItem> _scopeStack = new Stack<StackItem>();

        static LoggerScopeWrapper()
        {
            var type = typeof(ThreadContextStack);
            StackField = type.GetField("m_stack",
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.NonPublic);
        }

        public LoggerScopeWrapper(ILogger logger, IInternalLogger internalLogger, string loggerName)
        {
            _internalLogger = internalLogger;
            LoggerName = loggerName;
            Logger = logger;
        }

        public ILogger Logger { get; }

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

        private void PopAllStackItems()
        {
            while(_scopeStack.Any())
                PopOneItem();
        }

        
        public bool CanDiscard()
        {
            return !_scopeStack.Any();
        }

        /// <summary>
        ///     For a particular ILogger instance, adjust the scopes by calls to BeginScope or disposing
        ///     scopes as needs
        /// </summary>
        /// <param name="loggingEvent"></param>
        /// <returns>true if this object can be thrown away, else false</returns>
        public void AdjustScope(LoggingEvent loggingEvent)
        {
            var properties = loggingEvent.GetProperties();

            if (properties.Contains("NDC"))
            {
                var threadContextStack = properties["NDC"] as ThreadContextStack;
                if (threadContextStack == null || threadContextStack.Count == 0)
                {
                    PopAllStackItems();
                    return;
                }

                //this works by reflection against the log4net library.  if the underlying object changes, 
                //it won't work any more
                var items = GetStackFrameInfos(threadContextStack);
                if (items == null || items.Count == 0)
                {
                    PopAllStackItems();
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
                    {
                        PopOneItem();
                    }

                for (var i = startAt; i < items.Count; i++)
                {
                    var item = items[i];
                    _scopeStack.Push(new StackItem(Logger.BeginScope(item.Message), item));
                    if (_internalLogger.IsEnabled(LogLevel.Debug))
                        _internalLogger.LogDebug(
                            $"Created level {_scopeStack.Count} scope '{item.Message}' for {LoggerName}");
                }


                return;
            }

            PopAllStackItems();
            return;
        }

        private void PopOneItem()
        {
            if (_scopeStack.Any())
            {
                var oldCount = _scopeStack.Count;
                var stackItem = _scopeStack.Pop();
                stackItem.LoggerScope.Dispose();
                if (_internalLogger.IsEnabled(LogLevel.Debug))
                    _internalLogger.LogDebug(
                        $"Popped level {oldCount} scope '{stackItem.Info.Message}' for {LoggerName}");
            }
        }
    }
}