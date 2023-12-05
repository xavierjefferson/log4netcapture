using log4net.Core;

namespace Microsoft.Extensions.Logging.Log4NetCapture
{
    public interface IMicrosoftExtensionsLoggingScopeManager
    {
        int GetLoggerScopeWrapperCount();
        void AdjustScope(LoggingEvent loggingEvent, ILogger logger);
    }
}