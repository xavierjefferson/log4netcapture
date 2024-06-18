using System.Collections.Generic;
using log4net.Core;
using log4net.Filter;
using log4net.Layout;

namespace Snork.Log4NetCapture
{
    public class Log4NetCaptureConfiguration
    {
        public Log4NetCaptureConfiguration()
        {
            EnableMessageLayout();
        }

        public IInternalLogger InternalLogger { get; set; }
        public Level? RootAppenderLevel { get; set; }
        public Level? AppenderLevel { get; set; }
        public ILayout? Layout { get; set; }

        public List<IFilter> Filters { get; } = new List<IFilter>();
        public bool UseScopes { get; internal set; } = true;

        internal void EnableMessageLayout()
        {
            Layout = new PatternLayout("%message");
        }
    }
}