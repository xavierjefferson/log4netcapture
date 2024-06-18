using System;

namespace Snork.Log4NetCapture.Models
{
    public class StackItem
    {
        public StackItem(IDisposable loggerScope, StackFrameInfo info)
        {
            LoggerScope = loggerScope;
            Info = info;
        }
     
        public IDisposable LoggerScope { get; }
        public StackFrameInfo Info { get; }
    }
}