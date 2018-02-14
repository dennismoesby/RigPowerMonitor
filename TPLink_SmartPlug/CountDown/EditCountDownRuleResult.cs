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
    public sealed class EditCountDownRuleResult
    {
        #region "Propiedades"
            public int ErrorCode { get; internal set; }
            public string ErrorMessage { get; internal set; }
        #endregion
        #region "Metodos internos"
            internal void LoadFromJson(JToken pJson)
            {
                this.ErrorCode = pJson["err_code"].Value<int>();
                this.ErrorMessage = (this.ErrorCode != 0) ? pJson["err_msg"].Value<string>() : "";
            }
        #endregion
    }
}
