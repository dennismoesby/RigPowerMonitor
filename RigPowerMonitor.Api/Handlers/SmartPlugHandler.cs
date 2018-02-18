using RigPowerMonitor.Api;
using RigPowerMonitor.Api.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RigPowerMonitor.Api.Handlers
{
    public class SmartPlugHandler
    {
        public SmartPlugHandler(RpmSmartPlugs? plugType, string ipAddress)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ipAddress)) throw new ArgumentNullException("ipAddress", "Ip address must not be NULL when instatiating SmartPlugHandler.");
                if (plugType == null) throw new ArgumentNullException("plugType", "Plug type must not be NULL when instantiating SmartPlugHandler.");

                PlugType = plugType.Value;
                IpAddress = ipAddress;
                Name = getName();
            }
            catch (RpmSmartPlugCommunicationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RpmApiException("Failed to instantiate SmartPlugHandler", ".ctor", RpmExceptionType.Exception, ex);
            }
        }

        public RpmSmartPlugs PlugType { get; set; }
        public string IpAddress { get; set; }

        public string Name { get; private set; }

        public RpmSmartPlugState GetState()
        {
            try
            {
                switch (PlugType)
                {
                    case RpmSmartPlugs.WeMoInsightSwitch:
                    default:
                        return getWemoState();
                    case RpmSmartPlugs.TPLinkHS110:
                        return getTpLinkState();
                }
            }
            catch (RpmSmartPlugCommunicationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RpmApiException("Getting plug state failed in SmartPlugHandler", "GetState", RpmExceptionType.Exception, ex);
            }
        }

        public double GetCurrentPowerConsumption()
        {
            try
            {
                switch (PlugType)
                {
                    case RpmSmartPlugs.WeMoInsightSwitch:
                    default:
                        return getWemoCurrentPowerConsumption();
                    case RpmSmartPlugs.TPLinkHS110:
                        return getTpLinkCurrentPowerConsumption();
                }

            }
            catch (RpmSmartPlugCommunicationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RpmApiException("Getting current power consumption failed in SmartPlugHandler", "GetCurrentPowerConsumption", RpmExceptionType.Exception, ex);
            }
        }

        public bool SetPlugState(RpmSmartPlugState plugState)
        {
            try
            {
                switch (PlugType)
                {
                    case RpmSmartPlugs.WeMoInsightSwitch:
                    default:
                        return setWemoState(plugState);
                    case RpmSmartPlugs.TPLinkHS110:
                        return setTpLinkState(plugState);
                }

            }
            catch (RpmSmartPlugCommunicationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RpmApiException("Setting plug state failed in SmartPlugHandler", "SetPlugState", RpmExceptionType.Exception, ex);
            }
        }

        private string getName()
        {
            try
            {
                switch (PlugType)
                {
                    case RpmSmartPlugs.WeMoInsightSwitch:
                    default:
                        return getWemoFriendlyName();
                    case RpmSmartPlugs.TPLinkHS110:
                        return getTpLinkAlias();
                }
            }
            catch (RpmSmartPlugCommunicationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RpmApiException("Getting plug name failed in SmartPlugHandler", "getName", RpmExceptionType.Exception, ex);
            }
        }

        #region WeMo
        private string getWemoFriendlyName()
        {
            try
            {
                var ApiAddress = $"http://{IpAddress}";
                var v = new WemoNet.Wemo();
                var result = v.GetWemoResponseObjectAsync<Communications.Responses.GetFriendlyNameResponse>(Communications.Utilities.Soap.WemoGetCommands.GetFriendlyName, ApiAddress).GetAwaiter().GetResult();
                return result?.FriendlyName;
            }
            catch (Exception ex)
            {
                throw new RpmSmartPlugCommunicationException($"Could not get name from plug. Message: {ex.Message}", RpmSmartPlugs.WeMoInsightSwitch, IpAddress, ex);
            }
        }

        private RpmSmartPlugState getWemoState()
        {
            try
            {
                var ApiAddress = $"http://{IpAddress}";
                var v = new WemoNet.Wemo();
                var insight = v.GetInsightParams(ApiAddress).GetAwaiter().GetResult();
                return insight != null ? (RpmSmartPlugState)insight.State : RpmSmartPlugState.unknown;
            }
            catch (Exception ex)
            {
                throw new RpmSmartPlugCommunicationException($"Could not get state from plug. Message: {ex.Message}", RpmSmartPlugs.WeMoInsightSwitch, IpAddress, ex);
            }
        }

        private double getWemoCurrentPowerConsumption()
        {
            try
            {
                var ApiAddress = $"http://{IpAddress}";
                var v = new WemoNet.Wemo();
                var insight = v.GetInsightParams(ApiAddress).GetAwaiter().GetResult();
                return insight != null ? insight.CurrentPowerConsumption : 0;
            }
            catch (Exception ex)
            {
                throw new RpmSmartPlugCommunicationException($"Could not get current power consumption from plug. Message: {ex.Message}", RpmSmartPlugs.WeMoInsightSwitch, IpAddress, ex);
            }
        }

        private bool setWemoState(RpmSmartPlugState plugState)
        {
            try
            {

                var ApiAddress = $"http://{IpAddress}";
                var v = new WemoNet.Wemo();
                switch (plugState)
                {
                    case RpmSmartPlugState.off:
                        return v.TurnOffWemoPlugAsync(ApiAddress).GetAwaiter().GetResult();
                    case RpmSmartPlugState.on:
                        return v.TurnOnWemoPlugAsync(ApiAddress).GetAwaiter().GetResult();
                    case RpmSmartPlugState.unknown:
                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                throw new RpmSmartPlugCommunicationException($"Could not set the state of plug. Message: {ex.Message}", RpmSmartPlugs.WeMoInsightSwitch, IpAddress, ex);
            }
        }

        #endregion

        #region TP-Link HS110
        private string getTpLinkAlias()
        {
            try
            {
                var tp = new TPLink_SmartPlug.HS1XX(System.Net.IPAddress.Parse(IpAddress), 10000, 0, 0);
                var dev_info = tp.GetDeviceInfo();
                return dev_info?.Alias;
            }
            catch (Exception ex)
            {
                throw new RpmSmartPlugCommunicationException($"Could not get name from plug. Message: {ex.Message}", RpmSmartPlugs.TPLinkHS110, IpAddress, ex);
            }
        }

        private RpmSmartPlugState getTpLinkState()
        {
            try
            {
                var tp = new TPLink_SmartPlug.HS1XX(System.Net.IPAddress.Parse(IpAddress), 10000, 0, 0);
                var dev_info = tp.GetDeviceInfo();
                return dev_info != null ? dev_info.RelayState == (byte)0 ? RpmSmartPlugState.off : RpmSmartPlugState.on : RpmSmartPlugState.unknown;
            }
            catch (Exception ex)
            {
                throw new RpmSmartPlugCommunicationException($"Could not get state from plug. Message: {ex.Message}", RpmSmartPlugs.TPLinkHS110, IpAddress, ex);
            }
        }

        private double getTpLinkCurrentPowerConsumption()
        {
            try
            {
                var tp = new TPLink_SmartPlug.HS1XX(System.Net.IPAddress.Parse(IpAddress), 10000, 0, 0);
                var e = tp.GetEmeterRealtime();
                return e != null ? e.Power : 0;
            }
            catch (Exception ex)
            {
                throw new RpmSmartPlugCommunicationException($"Could not get current power consumption from plug. Message: {ex.Message}", RpmSmartPlugs.TPLinkHS110, IpAddress, ex);
            }
        }

        private bool setTpLinkState(RpmSmartPlugState plugState)
        {
            try
            {
                var tp = new TPLink_SmartPlug.HS1XX(System.Net.IPAddress.Parse(IpAddress), 10000, 0, 0);
                switch (plugState)
                {
                    case RpmSmartPlugState.off:
                        var rOff = tp.SwitchRelayState(TPLink_SmartPlug.RelayAction.TurnOff);
                        return rOff.ErrorCode == 0;
                    case RpmSmartPlugState.on:
                        var rOn = tp.SwitchRelayState(TPLink_SmartPlug.RelayAction.TurnOn);
                        return rOn.ErrorCode == 0;
                    case RpmSmartPlugState.unknown:
                    default:
                        return false;
                }

            }
            catch (Exception ex)
            {
                throw new RpmSmartPlugCommunicationException($"Could not set the state of plug. Message: {ex.Message}", RpmSmartPlugs.TPLinkHS110, IpAddress, ex);
            }

        }
        #endregion
    }

}
