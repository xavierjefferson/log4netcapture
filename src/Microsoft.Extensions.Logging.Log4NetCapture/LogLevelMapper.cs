using System.Collections.Generic;
using System.Linq;
using log4net.Core;

namespace Microsoft.Extensions.Logging.Log4NetCapture
{
    public class LogLevelMapper : ILogLevelMapper
    {
        private static readonly List<LevelInfo> LevelInfos = new List<LevelInfo>
        {

            new LevelInfo(Level.All, LogLevel.Trace),
            new LevelInfo(Level.Verbose, LogLevel.Trace),
            new LevelInfo(Level.Trace, LogLevel.Trace),
            new LevelInfo(Level.Debug, LogLevel.Debug),
            new LevelInfo(Level.Info, LogLevel.Information),
            new LevelInfo(Level.Warn, LogLevel.Warning),
            new LevelInfo(Level.Error, LogLevel.Error),
            new LevelInfo(Level.Critical, LogLevel.Critical),
            new LevelInfo(new Level(Level.Off.Value - 1, "max", "max"), LogLevel.Critical)
        };

        private static readonly List<LevelInfo> LevelTuples = LevelInfos.OrderByDescending(x => x.Level.Value).ToList();

        private static readonly Dictionary<int, LogLevel> LevelPairs =
            LevelInfos.ToDictionary(i => i.Level.Value, i => i.LogLevel);

        public LogLevel Map(Level level)
        {
            if (level == Level.Off) return LogLevel.None;

            if (LevelPairs.ContainsKey(level.Value)) return LevelPairs[level.Value];

            var tuple = LevelTuples.FirstOrDefault(i => i.Level.Value <= level.Value);
            if (tuple != null) return tuple.LogLevel;

            return LogLevel.None;
        }


        private class LevelInfo
        {
            public LevelInfo(Level level, LogLevel logLevel)
            {
                Level = level;
                LogLevel = logLevel;
            }

            public Level Level { get; }
            public LogLevel LogLevel { get; }
        }
    }
}