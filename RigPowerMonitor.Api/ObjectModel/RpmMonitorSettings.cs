using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RigPowerMonitor.Api.ObjectModel
{
    public class RpmMonitorSettings
    {
        public string IpAddress { get; set; }
        public int PowerConsumptionThreshold { get; set; }
        public int SecondsToWaitAfterPowerDecline { get; set; }
        public int SecondsToWaitBeforePoweringBackOn { get; set; }
        public RpmSmartPlugs PlugType { get; set; }
        public string LogFilePath { get; set; }
        public bool DoNotSaveLog { get; set; }
        public RpmLoggingLevel LoggingLevel { get; set; }
        public string TextbeltApiKey { get; set; }
        public string MobileNumber { get; set; }
        public bool TextNotificationEnabled
        {
            get
            {
                return !string.IsNullOrWhiteSpace(TextbeltApiKey) && !string.IsNullOrWhiteSpace(MobileNumber);
            }
        }

        public RpmMonitorSettings()
        {
            SecondsToWaitAfterPowerDecline = 300;
            SecondsToWaitBeforePoweringBackOn = 30;
            LoggingLevel = RpmLoggingLevel.WarningErrors;
        }
    }
}
