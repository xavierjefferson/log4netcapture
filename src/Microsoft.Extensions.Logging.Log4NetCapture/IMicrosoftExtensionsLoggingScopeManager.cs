using log4net.Core;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Snork.Log4NetCapture
{
    public interface IMicrosoftExtensionsLoggingScopeManager
    {
        int GetLoggerScopeWrapperCount();
        void AdjustScope(LoggingEvent loggingEvent, ILogger logger);
    }
}