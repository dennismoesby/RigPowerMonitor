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
    public sealed class AddCountDownRuleResult
    {
        #region "Propiedades"
            public string Id { get; internal set; }
            public int ErrorCode { get; internal set; }
            public string ErrorMessage { get; internal set; }
        #endregion
        #region "Metodos internos"
            internal void LoadFromJson(JToken pJson) 
            {
                this.ErrorCode = pJson["err_code"].Value<int>();
                this.Id = (this.ErrorCode == 0) ? pJson["id"].Value<string>() : "";
                this.ErrorMessage = (this.ErrorCode != 0) ? pJson["err_msg"].Value<string>() : "";
            }
        #endregion
    }
}
