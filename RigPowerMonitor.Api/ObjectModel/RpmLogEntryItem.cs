using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RigPowerMonitor.Api.ObjectModel
{

    public class RpmLogEntryItem
    {
        public string Message { get; set; }
        public int Result { get; set; }
        public Exception Error { get; set; }
        public RpmLogEntryItemType ItemType { get; set; }
        public DateTime CreatedOn { get; }
        public RpmLogEntryItem()
        {
            this.CreatedOn = DateTime.UtcNow;
        }

    }

}
