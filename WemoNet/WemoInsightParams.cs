using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WemoNet
{
    public class WemoInsightParams
    {
        public int State { get; set; }
        public DateTime LastChange { get; set; }
        public TimeSpan OnFor { get; set; }
        public TimeSpan OnToday { get; set;}
        public TimeSpan OnTotal { get; set; }
        public TimeSpan AverageCalculationPeriod { get; set; }
        public int AveragePowerConsumption { get; set; }
        public double CurrentPowerConsumption { get; set; }
        public double PowerConsumptionToday { get; set; }
        public double PowerConsumptionTotal { get; set; }
        public int PowerThreshold { get; set; }

        public WemoInsightParams()
        {
        }

        public WemoInsightParams(Communications.Responses.GetInsightParamsResponse response)
        {
            if (response == null || string.IsNullOrWhiteSpace(response.InsightParams)) return;

            var _params = response.InsightParams.Split(new char[] { '|' });

            State = int.Parse(_params[0]);
            LastChange = UnixTimeStampToDateTime(double.Parse(_params[1]));
            OnFor = new TimeSpan(0, 0, 0, int.Parse(_params[2]));
            OnToday = new TimeSpan(0, 0, 0, int.Parse(_params[3]));
            OnTotal = new TimeSpan(0, 0, 0, int.Parse(_params[4]));
            AverageCalculationPeriod = new TimeSpan(0, 0, 0, int.Parse(_params[5]));
            AveragePowerConsumption = int.Parse(_params[6]);
            CurrentPowerConsumption = double.Parse(_params[7]) / 1000;
            PowerConsumptionToday = double.Parse(_params[8]) / 1000;
            PowerConsumptionTotal = double.Parse(_params[9], new CultureInfo("en-US")) / 1000;
            PowerThreshold = int.Parse(_params[10]);
        }

        private DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }
    }
}
