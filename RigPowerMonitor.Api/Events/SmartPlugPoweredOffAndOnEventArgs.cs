using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RigPowerMonitor.Api.Events
{
    public class SmartPlugPoweredOffAndOnEventArgs : EventArgs
    {
        public RpmSmartPlugs PlugType { get; set; }
        public string IpAddress { get; set; }
        public double Power { get; set; }
        public DateTime PoweredOff { get; set; }
        public DateTime PoweredOn { get; set; }
        public string PlugName { get; set; }

        public SmartPlugPoweredOffAndOnEventArgs(RpmSmartPlugs plugType, string ipAddress, string plugName, double power, DateTime poweredOff, DateTime poweredOn)
        {
            PlugType = plugType;
            IpAddress = ipAddress;
            PlugName = plugName;
            Power = power;
            PoweredOff = poweredOff;
            PoweredOn = poweredOn;
        }
    }
}
