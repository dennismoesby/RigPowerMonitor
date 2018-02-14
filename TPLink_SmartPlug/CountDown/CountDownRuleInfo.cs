using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

using Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TPLink_SmartPlug.CountDown
{
    public sealed class CountDownRuleInfo
    {
        #region "Propiedades"
            public byte Enable { get; internal set; }

            public string ID { get; internal set; }

            public string Name { get; internal set; }

            public int Delay { get; internal set; }

            public byte Action { get; internal set; }

            public int Remain { get; internal set; }
        #endregion
        #region "Metodos internos"
            internal void LoadFromJson(JToken pJson) 
            {
                this.Enable = pJson["enable"].Value<byte>();
                this.ID = pJson["id"].Value<string>();
                this.Name = pJson["name"].Value<string>();
                this.Delay = pJson["delay"].Value<int>();
                this.Action = pJson["act"].Value<byte>();
                this.Remain = (this.Enable == 1) ? pJson["remain"].Value<int>() : 0;
            }
        #endregion
    }
}
