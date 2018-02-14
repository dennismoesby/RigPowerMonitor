using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TPLink_SmartPlug.WLan
{
    public sealed class WLanAccessPointEntryResult
    {
        #region "Propiedades"
            public string SSID { get; internal set; }
            public WirelessType WLanType { get; internal set; }
        #endregion
        #region "Metodos internos"
            internal void LoadFromJson(JToken pJson) 
            {
                this.SSID = pJson["ssid"].Value<string>();
                this.WLanType = (WirelessType)pJson["key_type"].Value<byte>();
            }
        #endregion
    }
}
