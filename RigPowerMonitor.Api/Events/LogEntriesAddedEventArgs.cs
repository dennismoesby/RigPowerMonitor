using RigPowerMonitor.Api.ObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RigPowerMonitor.Api.Events
{
    public class LogEntriesAddedEventArgs : EventArgs
    {
        public RpmLogEntryItem[] Entries { get; set; }
        public bool OutputOnNewLine { get; set; }

        public LogEntriesAddedEventArgs(RpmLogEntryItem[] entries, bool outputOnNewLine = true)
        {
            Entries = entries;
            OutputOnNewLine = outputOnNewLine;
        }

    }
}
