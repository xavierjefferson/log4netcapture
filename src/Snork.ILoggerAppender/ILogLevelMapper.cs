using log4net.Core;

namespace Microsoft.Extensions.Logging.Log4NetCapture
{
    public interface ILogLevelMapper
    {
        LogLevel Map(Level level);
    }
}