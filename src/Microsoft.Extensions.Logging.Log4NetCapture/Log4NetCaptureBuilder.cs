using System;
using log4net.Core;
using log4net.Filter;
using log4net.Layout;

namespace Snork.Log4NetCapture
{
    public class Log4NetCaptureBuilder
    {
        private readonly Log4NetCaptureConfiguration _configuration = new Log4NetCaptureConfiguration();

        public Log4NetCaptureBuilder WithAppenderLevel(Level? level)
        {
            _configuration.AppenderLevel = level;
            return this;
        }

        public Log4NetCaptureBuilder WithUseScopes(bool value)
        {
            _configuration.UseScopes = value;
            return this;
        }

        public Log4NetCaptureBuilder WithRootAppenderLevel(Level? level)
        {
            _configuration.RootAppenderLevel = level;
            return this;
        }

        public Log4NetCaptureBuilder WithFilters(params IFilter[] filters)
        {
            if (filters == null) throw new ArgumentNullException(nameof(filters));
            _configuration.Filters.AddRange(filters);
            ;
            return this;
        }

        public Log4NetCaptureBuilder WithMessageOnlyLayout()
        {
            _configuration.EnableMessageLayout();
            return this;
        }

        public Log4NetCaptureBuilder WithLayout(ILayout layout)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            _configuration.Layout = layout;
            return this;
        }

        public Log4NetCaptureConfiguration Build()
        {
            return _configuration;
        }

        public Log4NetCaptureBuilder WithInternalLogger(IInternalLogger testInternalLogger)
        {
            _configuration.InternalLogger = testInternalLogger;
            return this;
        }
    }
}