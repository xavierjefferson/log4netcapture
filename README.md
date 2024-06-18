
# Log4NetCapture

[![Latest version](https://img.shields.io/nuget/v/Snork.Log4NetCapture.svg)](https://www.nuget.org/packages/Snork.Log4NetCapture/) 

This package, Log4NetCapture, allows you to pipe Log4Net log entries to your configured Microsoft.Extensions.Logging log providers.  It is designed for use with .NET Core 6.0 and higher.

This will allow you to turn on logging for legacy code that logs to Log4Net, when you really want your output through modern Microsoft.Extensions.Logging log providers and **not** the older Log4Net appenders.

In order to make this work, you must be using the built-in dependency injection container Microsoft.Extensions.DependencyInjection version 6.0 or higher.

## Usage
Add a using statement to your startup code:

    using Snork.Log4NetCapture;

During initialization of your `ServiceCollection` instance during application startup, turn on logging to the Extensions.Logging provider of your choice, which might look like:

    serviceCollection.AddLogging(loggingBuilder =>
    {
    //configure here.  Console provider?  File provider?  Other?
    });

Next, you configure the Log4Net capture:

    //this is the simplest call, but see configuration information below
    serviceCollection.AddLog4NetCapture();

This is the **simplest** configuration of all.  All Log4Net events will be mapped to Extensions.Logging calls.  What actually gets logged depends on the configured minimum log level for Extensions.Logging, i.e. if a Log4Net event is "debug" but your Extensions.Logging minimum is "info", nothing gets logged.

Only the `Message` and `Exception` properties from the Log4net event will be mapped, by default.  It is possibly to specify a different message layout that includes other Log4Net properties -- see below.

After you have an instance of `IServiceProvider`, you need one more line of code:

    serviceProvider.StartLog4NetCapture();

That's it!

## Nested Diagnostic Context (NDC) Support
By default, Log4NetCapture supports Log4Net's nested diagnostic context features.  In the Extensions.Logging ecosystem, this will be automatically translated to logging scopes, i.e. method calls to `ILogger.BeginScope()`. 

You can turn this on or off during the `IServiceCollection` initialization:

    //turn scopes off
    serviceCollection.AddLog4NetCapture(config=>{
	    config.WithUseScopes(false);
    });

## Log4Net Filter Support
By default, Log4NetCapture will capture everything that Log4Net sends it.  You can also use Log4Net's filter mechanism to disable certain classes from logging. 

You can do this during the `IServiceCollection` initialization:

    //Turn on filters such that System.String is not accepted,
    //int is accepted, and nothing else is accepted
    serviceCollection.AddLog4NetCapture(config=>{
	    config.WithFilters(new LoggerMatchFilter { 
			    LoggerToMatch = typeof(string).FullName, 
			    AcceptOnMatch = false
			},
			new LoggerMatchFilter { 
				LoggerToMatch = typeof(int).FullName,
				AcceptOnMatch = true 
			},
			new DenyAllFilter()
		);
    });

## Log4Net Layout Support
By default, this package takes the `Message` and `Exception` properties from Log4Net entries, and sends them to Extensions.Logging.  Log4Net has many other properties available, but you have to render them by what's known as a Layout in the Log4Net ecosystem.

Several layouts are built into Log4Net, such as [XmlLayout](https://logging.apache.org/log4net/log4net-1.2.13/release/sdk/log4net.Layout.XmlLayout.html), [PatternLayout](https://logging.apache.org/log4net/log4net-1.2.13/release/sdk/log4net.Layout.PatternLayout.html), [ExceptionLayout](https://logging.apache.org/log4net/log4net-1.2.13/release/sdk/log4net.Layout.ExceptionLayout.html), and [SimpleLayout](https://logging.apache.org/log4net/log4net-1.2.13/release/sdk/log4net.Layout.SimpleLayout.html).

Here's an example showing use of a [PatternLayout](https://logging.apache.org/log4net/log4net-1.2.13/release/sdk/log4net.Layout.PatternLayout.html):

    //Use different layout to include more Log4Net properties
    serviceCollection.AddLog4NetCapture(config=>{
	    config.WithLayout(new PatternLayout(
		    "%location %line %method %message"));
    });

To go back to the default for this package, do this:

    //Use different layout to include more Log4Net properties
    serviceCollection.AddLog4NetCapture(config=>{
	    config.WithMessageOnlyLayout();
    });


### Level Mappings
Log4Net has 18 built-in log levels, but Extensions.Logging only has seven.  Log4Net also assigns a numeric value with each built-in level.  Some built-in levels use the same numeric value, such as Finest and Verbose.

The following table shows how the Log4Net levels have been mapped to Extensions.Logging enumeration values.  It's also possible that developers have used custom Log4Net levels.  For those, this package will do its best to pick an appropriate Extensions.Logging enumeration value, based on the numeric Log4Net level value of any custom Log4Net level.

| Log4Net Level Name|Log4Net Level Value|Extensions.Logging Enum Value  |
|--|--|--|
| Off |2147483647|None  |
| All |-2147483648|Trace  |
| Finest |10000|Trace  |
| Verbose |10000|Trace  |
| Finer |20000|Trace  |
| Trace |20000|Trace  |
| Fine |30000|Debug  |
| Debug |30000|Debug  |
| Info |40000|Information  |
| Notice |50000|Information  |
| Warn |60000|Warning  |
| Error |70000|Error  |
| Severe |80000|Error  |
| Critical |90000|Critical  |
| Alert |100000|Critical  |
| Fatal |110000|Critical  |
| Emergency |120000|Critical  |
| Log4Net_Debug |120000|Critical  |