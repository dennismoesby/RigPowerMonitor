using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RigPowerMonitor.Api.Exceptions
{
    public class RpmApiException : Exception
    {
        public RpmExceptionType exceptionType { get; set; }

        public string MethodName { get; set; }

        public RpmApiException()
        {
        }

        public RpmApiException(string Message)
            : base(Message)
        {
        }

        public RpmApiException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }

        public RpmApiException(string Message, string MethodName = null, RpmExceptionType exceptionType = RpmExceptionType.Exception, Exception InnerException = null)
            : base(Message, InnerException)
        {
            this.exceptionType = exceptionType;
            this.MethodName = MethodName;
        }
    }
}
