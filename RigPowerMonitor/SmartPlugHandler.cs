using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RigPowerMonitor
{
    public class SmartPlugHandler
    {
        private SmartPlugs plugtype;
        private string ipaddress;

        public SmartPlugHandler(SmartPlugs plugType, string ipAddress)
        {
            plugtype = plugType;
            ipaddress = ipAddress;
        }

        public string Name
        {
            get
            {
                if(string.IsNullOrWhiteSpace(__name))
                {
                    try
                    {
                        switch (plugtype)
                        {
                            case SmartPlugs.WeMoInsightSwitch:
                                __name = getWemoFriendlyName();
                                break;
                            case SmartPlugs.TPLinkHS110:
                                __name = getTpLinkAlias();
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                    }
                    catch
                    {
                        throw;
                    }
                }

                return __name;
            }
        } private string __name;


        public SmartPlugState GetState()
        {
            try
            {
                switch (plugtype)
                {
                    case SmartPlugs.WeMoInsightSwitch:
                        return getWemoState();
                    case SmartPlugs.TPLinkHS110:
                        return getTpLinkState();
                    default:
                        throw new NotImplementedException();
                }
            }
            catch
            {
                throw;
            }
        }

        public double GetCurrentPowerConsumption()
        {
            try
            {
                switch (plugtype)
                {
                    case SmartPlugs.WeMoInsightSwitch:
                        return getWemoCurrentPowerConsumption();
                    case SmartPlugs.TPLinkHS110:
                        return getTpLinkCurrentPowerConsumption();
                    default:
                        throw new NotImplementedException();
                }

            }
            catch { throw; }
        }

        public bool SetPlugState(SmartPlugState plugState)
        {
            try
            {
                switch (plugtype)
                {
                    case SmartPlugs.WeMoInsightSwitch:
                        return setWemoState(plugState);
                    case SmartPlugs.TPLinkHS110:
                        return setTpLinkState(plugState);
                    default:
                        throw new NotImplementedException();
                }

            }
            catch { throw; }
        }

        #region WeMo
        private string getWemoFriendlyName()
        {
            try
            {
                var ApiAddress = $"http://{ipaddress}";
                var v = new WemoNet.Wemo();
                var result = v.GetWemoResponseObjectAsync<Communications.Responses.GetFriendlyNameResponse>(Communications.Utilities.Soap.WemoGetCommands.GetFriendlyName, ApiAddress).GetAwaiter().GetResult();
                return result?.FriendlyName;
            }
            catch
            {
                throw;
            }
        }

        private SmartPlugState getWemoState()
        {
            try
            {
                var ApiAddress = $"http://{ipaddress}";
                var v = new WemoNet.Wemo();
                var insight = v.GetInsightParams(ApiAddress).GetAwaiter().GetResult();
                return insight != null ? (SmartPlugState)insight.State : SmartPlugState.unknown;
            }
            catch
            {
                throw;
            }
        }

        private double getWemoCurrentPowerConsumption()
        {
            try
            {
                var ApiAddress = $"http://{ipaddress}";
                var v = new WemoNet.Wemo();
                var insight = v.GetInsightParams(ApiAddress).GetAwaiter().GetResult();
                return insight != null ? insight.CurrentPowerConsumption : 0;
            }
            catch
            {
                throw;
            }
        }

        private bool setWemoState(SmartPlugState plugState)
        {
            try
            {

                var ApiAddress = $"http://{ipaddress}";
                var v = new WemoNet.Wemo();
                switch (plugState)
                {
                    case SmartPlugState.off:
                        return v.TurnOffWemoPlugAsync(ApiAddress).GetAwaiter().GetResult();
                    case SmartPlugState.on:
                        return v.TurnOnWemoPlugAsync(ApiAddress).GetAwaiter().GetResult();
                    case SmartPlugState.unknown:
                    default:
                        return false;
                }
            }
            catch { throw; }
        }

        #endregion

        #region TP-Link HS110
        private string getTpLinkAlias()
        {
            try
            {
                var tp = new TPLink_SmartPlug.HS1XX(System.Net.IPAddress.Parse(ipaddress), 10000, 0, 0);
                var dev_info = tp.GetDeviceInfo();
                return dev_info?.Alias;
            }
            catch
            {
                throw;
            }
        }

        private SmartPlugState getTpLinkState()
        {
            try
            {
                var tp = new TPLink_SmartPlug.HS1XX(System.Net.IPAddress.Parse(ipaddress), 10000, 0, 0);
                var dev_info = tp.GetDeviceInfo();
                return dev_info != null ? dev_info.RelayState == (byte)0 ? SmartPlugState.off : SmartPlugState.on : SmartPlugState.unknown;
            }
            catch
            {
                throw;
            }
        } 

        private double getTpLinkCurrentPowerConsumption()
        {
            try
            {
                var tp = new TPLink_SmartPlug.HS1XX(System.Net.IPAddress.Parse(ipaddress), 10000, 0, 0);
                var e = tp.GetEmeterRealtime();
                return e != null ? e.Power : 0;
            }
            catch
            {
                throw;
            }
        }

        private bool setTpLinkState(SmartPlugState plugState)
        {
            try
            {
                var tp = new TPLink_SmartPlug.HS1XX(System.Net.IPAddress.Parse(ipaddress), 10000, 0, 0);
                switch (plugState)
                {
                    case SmartPlugState.off:
                        var rOff = tp.SwitchRelayState(TPLink_SmartPlug.RelayAction.TurnOff);
                        return rOff.ErrorCode == 0;
                    case SmartPlugState.on:
                        var rOn = tp.SwitchRelayState(TPLink_SmartPlug.RelayAction.TurnOn);
                        return rOn.ErrorCode == 0;
                    case SmartPlugState.unknown:
                    default:
                        return false;
                }

            }
            catch { throw; }

        }
        #endregion
    }

    public enum SmartPlugs
    {
        WeMoInsightSwitch,
        TPLinkHS110
    }

    public enum SmartPlugState
    {
        off,
        on,
        unknown
    }
}
