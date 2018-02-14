using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

using Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TPLink_SmartPlug.Time
{
    public sealed class DeviceTimeZone
    {
        #region "Propiedades"
            public int ErrorCode { get; internal set; }
            public string ErrorMessage { get; internal set; }
            public byte TimeZoneIndex { get; internal set; }
            public string TimeZoneDescription { get; internal set; }
            public string TimeZoneString { get; internal set; }
            public int UTC_OffsetDuringDaylightSavingTime { get; internal set; }
        #endregion
        #region "Metodos privados"
            internal void LoadFromJson(JToken pJson) 
            {
                this.ErrorCode = pJson["err_code"].Value<int>();
                this.ErrorMessage = (this.ErrorCode != 0) ? pJson["err_msg"].Value<string>() : "";
                this.TimeZoneIndex = (this.ErrorCode == 0) ? pJson["index"].Value<byte>() : (byte)0;
                this.TimeZoneDescription = (this.ErrorCode == 0) ? pJson["zone_str"].Value<string>() : "";
                this.TimeZoneString = (this.ErrorCode == 0) ? pJson["tz_str"].Value<string>() : "";
                this.UTC_OffsetDuringDaylightSavingTime = (this.ErrorCode == 0) ? pJson["dst_offset"].Value<int>() : 0;
            }
        #endregion
    }
}
