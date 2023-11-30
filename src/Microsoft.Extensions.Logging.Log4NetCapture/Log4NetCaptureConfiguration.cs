using System.Collections.Generic;
using log4net.Core;
using log4net.Filter;
using log4net.Layout;

namespace Microsoft.Extensions.Logging.Log4NetCapture
{
    public class Log4NetCaptureConfiguration
    {
        public Level? RootAppenderLevel { get; set; }
        public Level? AppenderLevel { get; set; }
        public ILayout? Layout { get; set; } = new SimpleLayout();

        public List<IFilter>? Filters { get; set; } = new List<IFilter>();
        public bool UseScopes { get; internal set; } = true;
    }
}