using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RigPowerMonitor.Api.Exceptions
{
    public class RpmSmartPlugCommunicationException : Exception
    {
        public RpmSmartPlugs PlugType { get; set; }
        public string IpAddress { get; set; }
        

        public RpmSmartPlugCommunicationException(RpmSmartPlugs plugType, string ipAddress)
        {
            PlugType = plugType;
            IpAddress = ipAddress;
        }

        public RpmSmartPlugCommunicationException(string message, RpmSmartPlugs plugType, string ipAddress) : base(message)
        {
            PlugType = plugType;
            IpAddress = ipAddress;
        }

        public RpmSmartPlugCommunicationException(string message, RpmSmartPlugs plugType, string ipAddress, Exception innerException) : base(message, innerException)
        {
            PlugType = plugType;
            IpAddress = ipAddress;
        }

        protected RpmSmartPlugCommunicationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
