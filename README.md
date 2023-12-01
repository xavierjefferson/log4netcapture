# Log4NetCapture

This package, Log4NetCapture, allows you to pipe Log4Net log entries to your configured Microsoft.Extensions.Logging log providers.  It is designed for use with .NET Core 6.0 and higher.

This will allow you to turn on logging for legacy code that logs to Log4Net, when you really want your output through modern Microsoft.Extensions.Logging log providers and **not** the older Log4Net appenders.

In order to make this work, you must be using the built-in dependency injection container Microsoft.Extensions.DependencyInjection version 6.0 or higher.

## Usage
Add a using statement to your startup code:

    using Microsoft.Extensions.Logging.Log4NetCapture;

During initialization of your `ServiceCollection` instance during application startup, turn on logging to the Extensions.Logging provider of your choice, which might look like:

    serviceCollection.AddLogging(loggingBuilder =>
    {
    //configure here.  Console provider?  File provider?  Other?
    });

Next, you configure the Log4Net capture:

    //this is the simplest call, but see configuration information below
    serviceCollection.AddLog4NetCapture();

This is the **simplest** configuration of all.  All Log4Net events will be mapped to Extensions.Logging calls.  What actually gets logged depends on the configured minimum log level for Extensions.Logging, i.e. if a Log4Net event is "debug" but your Extensions.Logging minimum is "info", nothing gets logged.

Only the `message` property from the Log4net event will be mapped.  It is possibly to specify a different message layout that includes other Log4Net properties -- look for that below.

After you have an instance of `IServiceProvider`, you need one more line of code:

    serviceProvider.StartLog4NetCapture();

That's it!


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