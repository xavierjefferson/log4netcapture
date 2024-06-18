using log4net.Core;
using Microsoft.Extensions.Logging;

namespace Snork.Log4NetCapture
{
    public interface ILogLevelMapper
    {
        LogLevel Map(Level level);
    }
}