using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RigPowerMonitor.Api.Events
{
    public class LogHandlerOperationFailedEventArgs : EventArgs
    {
        public RpmLogOperation Operation { get; set; }
        public string Message { get; set; }
        public bool OutputOnNewLine { get; set; }

        public LogHandlerOperationFailedEventArgs(RpmLogOperation operation, string message, bool outputOnNewLine = true)
        {
            Operation = operation;
            Message = message;
            OutputOnNewLine = outputOnNewLine;
        }
    }
}
