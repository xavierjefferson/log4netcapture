using log4net.Core;

namespace Snork.Log4NetCapture.Tests;

internal static class KnownLevelMapper
{
    public static Level? GetLevel(KnownLevelEnum logLevel)
    {
        switch (logLevel)
        {
            case KnownLevelEnum.All: return Level.All;
            case KnownLevelEnum.Alert: return Level.Alert;
            case KnownLevelEnum.Critical: return Level.Critical;
            case KnownLevelEnum.Debug: return Level.Debug;
            case KnownLevelEnum.Emergency: return Level.Emergency;
            case KnownLevelEnum.Error: return Level.Error;
            case KnownLevelEnum.Fatal: return Level.Fatal;
            case KnownLevelEnum.Fine: return Level.Fine;
            case KnownLevelEnum.Finer: return Level.Finer;
            case KnownLevelEnum.Finest: return Level.Finest;
            case KnownLevelEnum.Info: return Level.Info;
            case KnownLevelEnum.Log4Net_Debug: return Level.Log4Net_Debug;
            case KnownLevelEnum.Notice: return Level.Notice;
            case KnownLevelEnum.Off: return Level.Off;
            case KnownLevelEnum.Severe: return Level.Severe;
            case KnownLevelEnum.Trace: return Level.Trace;
            case KnownLevelEnum.Verbose: return Level.Verbose;
            case KnownLevelEnum.Warn: return Level.Warn;
            default: return null;
        }
    }
}