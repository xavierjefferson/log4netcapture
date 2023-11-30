﻿using System.Collections.Generic;
using System.Linq;
using log4net.Core;
using log4net.Filter;
using log4net.Layout;

namespace Microsoft.Extensions.Logging.Log4NetCapture
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

        public Log4NetCaptureBuilder WithFilters(IEnumerable<IFilter> filters)
        {
            _configuration.Filters = filters?.ToList();
            return this;
        }

        public Log4NetCaptureBuilder WithPatternLayout(string layout)
        {
            _configuration.Layout = new PatternLayout(layout);
            return this;
        }

        public Log4NetCaptureBuilder WithLayout(ILayout layout)
        {
            _configuration.Layout = layout;
            return this;
        }

        public Log4NetCaptureBuilder WithSimpleLayout()
        {
            _configuration.Layout = new SimpleLayout();
            return this;
        }

        public Log4NetCaptureConfiguration Build()
        {
            return _configuration;
        }
    }
}