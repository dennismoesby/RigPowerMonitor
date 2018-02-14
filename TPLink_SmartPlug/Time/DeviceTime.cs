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
    public sealed class DeviceTime
    {
        #region "Propiedades"
            public int ErrorCode { get; internal set; }
            public string ErrorMessage { get; internal set; }
            public int Year { get; internal set; }
            public byte Month { get; internal set; }
            public byte Day { get; internal set; }
            public byte DayOfWeek { get; internal set; }
            public byte Hour { get; internal set; }
            public byte Minute { get; internal set; }
            public byte Second { get; internal set; }
        #endregion
        #region "Metodos privados"
            internal void LoadFromJson(JToken pJson) 
            {
                this.ErrorCode = pJson["err_code"].Value<int>();
                this.ErrorMessage = (this.ErrorCode != 0) ? pJson["err_msg"].Value<string>() : "";
                this.Year = (this.ErrorCode == 0) ? pJson["year"].Value<int>() : 0;
                this.Month = (this.ErrorCode == 0) ? pJson["month"].Value<byte>() : (byte)0;
                this.Day = (this.ErrorCode == 0) ? pJson["mday"].Value<byte>() : (byte)0;
                this.DayOfWeek = (this.ErrorCode == 0) ? pJson["wday"].Value<byte>() : (byte)0;
                this.Hour = (this.ErrorCode == 0) ? pJson["hour"].Value<byte>() : (byte)0;
                this.Minute = (this.ErrorCode == 0) ? pJson["min"].Value<byte>() : (byte)0;
                this.Second = (this.ErrorCode == 0) ? pJson["sec"].Value<byte>() : (byte)0;
            }
        #endregion
    }
}
