using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TPLink_SmartPlug
{
    public sealed class RealtimeEmeter
    {
        #region Properties
        public double Current { get; set; }
        public double Voltage { get; set; }
        public double Power { get; set; }
        public double Total { get; set; }
        public int ErrorCode { get; set; }

        #endregion

        internal void LoadFromJson(JToken pJson)
        {
            this.ErrorCode = pJson["err_code"].Value<int>();
            this.Current = pJson["current"].Value<double>();
            this.Voltage = pJson["voltage"].Value<double>();
            this.Power = pJson["power"].Value<double>();
            this.Total = pJson["total"].Value<double>();
        }
    }
}
