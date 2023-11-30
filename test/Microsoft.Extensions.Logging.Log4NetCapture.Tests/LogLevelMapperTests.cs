using Xunit;

namespace Microsoft.Extensions.Logging.Log4NetCapture.Tests;

public class LogLevelMapperTests
{
    [Theory]
    [InlineData(KnownLevelEnum.Alert, LogLevel.Critical)]
    [InlineData(KnownLevelEnum.All, LogLevel.Trace)]
    [InlineData(KnownLevelEnum.Critical, LogLevel.Critical)]
    [InlineData(KnownLevelEnum.Debug, LogLevel.Debug)]
    [InlineData(KnownLevelEnum.Emergency, LogLevel.Critical)]
    [InlineData(KnownLevelEnum.Error, LogLevel.Error)]
    [InlineData(KnownLevelEnum.Fatal, LogLevel.Critical)]
    [InlineData(KnownLevelEnum.Fine, LogLevel.Debug)]
    [InlineData(KnownLevelEnum.Finer, LogLevel.Trace)]
    [InlineData(KnownLevelEnum.Finest, LogLevel.Trace)]
    [InlineData(KnownLevelEnum.Info, LogLevel.Information)]
    [InlineData(KnownLevelEnum.Log4Net_Debug, LogLevel.Critical)]
    [InlineData(KnownLevelEnum.Notice, LogLevel.Information)]
    [InlineData(KnownLevelEnum.Off, LogLevel.None)]
    [InlineData(KnownLevelEnum.Severe, LogLevel.Error)]
    [InlineData(KnownLevelEnum.Trace, LogLevel.Trace)]
    [InlineData(KnownLevelEnum.Verbose, LogLevel.Trace)]
    [InlineData(KnownLevelEnum.Warn, LogLevel.Warning)]
    public void TestMapping(KnownLevelEnum knownLevel, LogLevel expectedLogLevel)
    {
        var logLevelMapper = new LogLevelMapper();
        var logLevel = logLevelMapper.Map(KnownLevelMapper.GetLevel(knownLevel));
        Assert.Equal(expectedLogLevel, logLevel);
    }
}