using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RigPowerMonitor.Api.ObjectModel
{
    public class RpmLogSettings
    {
        public string LogFilePath { get; set; }
        public string LogFileName { get; set; }
        public RpmLoggingLevel LoggingLevel { get; set; }
        public bool DoNotSaveLog { get; set; }
        public string SmartPlugName { get; set; }
        public string SmartPlugIpAddress { get; set; }

        public RpmLogSettings()
        {
            LoggingLevel = RpmLoggingLevel.WarningErrors;
            DoNotSaveLog = false;
            LogFileName = string.Format("rpmlog_{0}", DateTime.Now.ToString("yyyyMMddHHmmss"));
        }
    }

}
