using System.Collections.Generic;
using log4net.Core;
using log4net.Filter;
using log4net.Layout;

namespace Microsoft.Extensions.Logging.Log4NetCapture
{
    public class Log4NetCaptureConfiguration
    {
        internal void EnableMessageLayout()
        {
            this.Layout = new PatternLayout("%message");
        }

        public Log4NetCaptureConfiguration()
        {
            EnableMessageLayout();
        }
        public Level? RootAppenderLevel { get; set; }
        public Level? AppenderLevel { get; set; }
        public ILayout? Layout { get; set; } 

        public List<IFilter>? Filters { get; set; } = new List<IFilter>();
        public bool UseScopes { get; internal set; } = true;
    }
}