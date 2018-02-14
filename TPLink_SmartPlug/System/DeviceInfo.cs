using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

using Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TPLink_SmartPlug
{
    public sealed class DeviceInfo
    {
        #region "Propiedades"
            public int ErrorCode { get; internal set; }
            public string ErrorMessage { get; internal set; }
            public string SoftwareVersion { get; internal set; }
            public string HardwareVersion { get; internal set; }
            public string DeviceType { get; internal set; }
            public string Model { get; internal set; }
            public string MAC { get; internal set; }
            public string ID { get; internal set; }
            public string HardwareID { get; internal set; }
            public string FirmwareID { get; internal set; }
            public string OEMID { get; internal set; }
            public string Alias { get; internal set; }
            public string DeviceName { get; internal set; }
            public string IconHash { get; internal set; }
            public byte RelayState { get; internal set; }
            public long OnTime { get; internal set; }
            public string ActiveMode { get; internal set; }
            public string Feature { get; internal set; }
            public long Updating { get; internal set; }
            public long RSSI { get; internal set; }
            public byte LedOffState { get; internal set; }
            public long Latitude { get; internal set; }
            public long Longitude { get; internal set; }
        #endregion
        #region "Metodos internos"
            internal void LoadFromJson(JToken pJson) 
            {
                this.ErrorCode = pJson["err_code"].Value<int>();
                this.ErrorMessage = (this.ErrorCode != 0) ? pJson["err_msg"].Value<string>() : "";
                this.SoftwareVersion = (this.ErrorCode == 0) ? pJson["sw_ver"].Value<string>() : "";
                this.HardwareVersion = (this.ErrorCode == 0) ? pJson["hw_ver"].Value<string>() : "";
                this.DeviceType = (this.ErrorCode == 0) ? pJson["type"].Value<string>() : "";
                this.Model = (this.ErrorCode == 0) ? pJson["model"].Value<string>() : "";
                this.MAC = (this.ErrorCode == 0) ? pJson["mac"].Value<string>() : "";
                this.ID = (this.ErrorCode == 0) ? pJson["deviceId"].Value<string>() : "";
                this.HardwareID = (this.ErrorCode == 0) ? pJson["hwId"].Value<string>() : "";
                this.FirmwareID = (this.ErrorCode == 0) ? pJson["fwId"].Value<string>() : "";
                this.OEMID = (this.ErrorCode == 0) ? pJson["oemId"].Value<string>() : "";
                this.Alias = (this.ErrorCode == 0) ? pJson["alias"].Value<string>() : "";
                this.DeviceName = (this.ErrorCode == 0) ? pJson["dev_name"].Value<string>() : "";
                this.IconHash = (this.ErrorCode == 0) ? pJson["icon_hash"].Value<string>() : "";
                this.RelayState = (this.ErrorCode == 0) ? pJson["relay_state"].Value<byte>() : (byte)0;
                this.OnTime = (this.ErrorCode == 0) ? pJson["on_time"].Value<long>() : 0;
                this.ActiveMode = (this.ErrorCode == 0) ? pJson["active_mode"].Value<string>() : "";
                this.Feature = (this.ErrorCode == 0) ? pJson["feature"].Value<string>() : "";
                this.Updating = (this.ErrorCode == 0) ? pJson["updating"].Value<long>() : 0;
                this.RSSI = (this.ErrorCode == 0) ? pJson["rssi"].Value<long>() : 0;
                this.LedOffState = (this.ErrorCode == 0) ? pJson["led_off"].Value<byte>() : (byte)0;
                this.Latitude = (this.ErrorCode == 0) ? pJson["latitude"].Value<long>() : 0;
                this.Longitude = (this.ErrorCode == 0) ? pJson["longitude"].Value<long>() : 0;
            }
        #endregion
    }
}
