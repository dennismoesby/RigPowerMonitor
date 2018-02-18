using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RigPowerMonitor.Api
{
    public enum RpmLogEntryItemType
    {
        INFO,
        WARNING,
        ERROR
    }

    public enum RpmLogOperation
    {
        Instantiate,
        AddToLog,
        LogEntries,
        SaveLog,
        ClearLog,
        parseErrors,
        Dispose
    }

    public enum RpmExceptionType
    {
        Warning,
        DataError,
        Exception
    }

    public enum RpmSmartPlugs
    {
        WeMoInsightSwitch,
        TPLinkHS110
    }

    public enum RpmSmartPlugState
    {
        off,
        on,
        unknown
    }

    public enum RpmLoggingLevel
    {
        InfoWarningError,
        WarningErrors,
        Errors
    }

}
