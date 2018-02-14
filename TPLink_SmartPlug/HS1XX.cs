using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;

using Newtonsoft.Json.Linq;

using TPLink_SmartPlug.Exceptions;

using TPLink_SmartPlug.Time;
using TPLink_SmartPlug.CountDown;
using TPLink_SmartPlug.WLan;

namespace TPLink_SmartPlug
{
    public sealed class HS1XX
    {
		#region "Variables globales"
        private IPAddress mIP = null;
        private int mConnectionTimeOut = 5000;
        private int mSendCommandTimeOut = 1000;
        private int mReceiveCommandTimeOut = 1000;
        #endregion
        #region "Comandos"
        /*
            Cloud Commands
            ========================================
            Get Cloud Info (Server, Username, Connection Status)
            {"cnCloud":{"get_info":null}}

            Get Firmware List from Cloud Server
            {"cnCloud":{"get_intl_fw_list":{}}}

            Set Server URL
            {"cnCloud":{"set_server_url":{"server":"devs.tplinkcloud.com"}}}

            Connect with Cloud username & Password
            {"cnCloud":{"bind":{"username":"your@email.com", "password":"secret"}}}

            Unregister Device from Cloud Account
            {"cnCloud":{"unbind":null}}

            EMeter Energy Usage Statistics Commands
            (for TP-Link HS110)
            ========================================
            Get Realtime Current and Voltage Reading
            {"emeter":{"get_realtime":{}}}

            Get EMeter VGain and IGain Settings
            {"emeter":{"get_vgain_igain":{}}}

            Set EMeter VGain and Igain
            {"emeter":{"set_vgain_igain":{"vgain":13462,"igain":16835}}}

            Start EMeter Calibration
            {"emeter":{"start_calibration":{"vtarget":13462,"itarget":16835}}}

            Get Daily Statistic for given Month
            {"emeter":{"get_daystat":{"month":1,"year":2016}}}

            Get Montly Statistic for given Year
            {"emeter":{""get_monthstat":{"year":2016}}}

            Erase All EMeter Statistics
            {"emeter":{"erase_emeter_stat":null}}

            Schedule Commands
            (action to perform regularly on given weekdays)
            ========================================
            Get Next Scheduled Action
            {"schedule":{"get_next_action":null}}

            Get Schedule Rules List
            {"schedule":{"get_rules":null}}

            Add New Schedule Rule
            {"schedule":{"add_rule":{"stime_opt":0,"wday":[1,0,0,1,1,0,0],"smin":1014,"enable":1,"repeat":1,"etime_opt":-1,"name":"lights on","eact":-1,"month":0,"sact":1,"year":0,"longitude":0,"day":0,"force":0,"latitude":0,"emin":0},"set_overall_enable":{"enable":1}}}

            Edit Schedule Rule with given ID
            {"schedule":{"edit_rule":{"stime_opt":0,"wday":[1,0,0,1,1,0,0],"smin":1014,"enable":1,"repeat":1,"etime_opt":-1,"id":"4B44932DFC09780B554A740BC1798CBC","name":"lights on","eact":-1,"month":0,"sact":1,"year":0,"longitude":0,"day":0,"force":0,"latitude":0,"emin":0}}}

            Delete Schedule Rule with given ID
            {"schedule":{"delete_rule":{"id":"4B44932DFC09780B554A740BC1798CBC"}}}

            Delete All Schedule Rules and Erase Statistics
            {"schedule":{"delete_all_rules":null,"erase_runtime_stat":null}}

            Anti-Theft Rule Commands (aka Away Mode) 
            (period of time during which device will be randomly turned on and off to deter thieves) 
            ========================================
            Get Anti-Theft Rules List
            {"anti_theft":{"get_rules":null}}

            Add New Anti-Theft Rule
            {"anti_theft":{"add_rule":{"stime_opt":0,"wday":[0,0,0,1,0,1,0],"smin":987,"enable":1,"frequency":5,"repeat":1,"etime_opt":0,"duration":2,"name":"test","lastfor":1,"month":0,"year":0,"longitude":0,"day":0,"latitude":0,"force":0,"emin":1047},"set_overall_enable":1}}

            Edit Anti-Theft Rule with given ID
            {"anti_theft":{"edit_rule":{"stime_opt":0,"wday":[0,0,0,1,0,1,0],"smin":987,"enable":1,"frequency":5,"repeat":1,"etime_opt":0,"id":"E36B1F4466B135C1FD481F0B4BFC9C30","duration":2,"name":"test","lastfor":1,"month":0,"year":0,"longitude":0,"day":0,"latitude":0,"force":0,"emin":1047},"set_overall_enable":1}}

            Delete Anti-Theft Rule with given ID
            {"anti_theft":{"delete_rule":{"id":"E36B1F4466B135C1FD481F0B4BFC9C30"}}}

            Delete All Anti-Theft Rules
            "anti_theft":{"delete_all_rules":null}}
            */
        #endregion
        #region "Constructor"
        /// <summary>
        /// Create a new instance
        /// </summary>
        /// <param name="pIP">Device IP</param>
        /// <param name="pConnectionTimeOut">Connection timeout in milliseconds</param>
        /// <param name="pSendCommandTimeOut">Send command timeout in milliseconds</param>
        /// <param name="pReceiveCommandTimeOut">Recive response timeout in milliseconds</param>
        public HS1XX(IPAddress pIP, int pConnectionTimeOut = 1000, int pSendCommandTimeOut = 1000, int pReceiveCommandTimeOut = 1000) 
        {
            this.mIP = new IPAddress(pIP.GetAddressBytes());
            this.mConnectionTimeOut = pConnectionTimeOut;
            this.mSendCommandTimeOut = pSendCommandTimeOut;
            this.mReceiveCommandTimeOut = pReceiveCommandTimeOut;
        }
        #endregion
        #region "Metodos privados"
        /// <summary>
        /// Execute the command
        /// </summary>
        /// <param name="pCommand">Command</param>
        private void ExecuteCommand(string pCommand)
        {
            TcpClient mClient = new TcpClient();

			//Set send command timeout
			mClient.SendTimeout = this.mSendCommandTimeOut;

			//Get the message bytes
			byte[] mMessage = Encoding.ASCII.GetBytes(pCommand);

			//Get the bytes of encrypted message
			byte[] mEncryptedMessage = Common.EncryptMessage(mMessage, ProtocolType.TCP);

			//Connect to the device
			IAsyncResult result = mClient.BeginConnect(new IPAddress(this.mIP.GetAddressBytes()), 9999, null, null);
			bool success = result.AsyncWaitHandle.WaitOne(this.mConnectionTimeOut);

			if (success)
			{
				try
				{
					//Send the command
					using (NetworkStream stream = mClient.GetStream())
					{
						stream.Write(mEncryptedMessage, 0, mEncryptedMessage.Length);
						stream.Close();
					}		
				}
				catch (Exception ex)
				{
					throw new NonCompatibleDeviceException(ex.Message, ex);
				}
				finally
				{
					//Close TCP connection
					if (mClient.Connected)
					{
						mClient.EndConnect(result);
					}				
				}
			}
			else
			{
				throw new ConnectionErrorException("Unable to connect: " + this.mIP.ToString());
			}
        }
        /// <summary>
        /// Execute the command and get the response
        /// </summary>
        /// <param name="pCommand">Command</param>
        /// <returns>Return the response of the device</returns>
        private string ExecuteAndRead(string pCommand) 
        {
            string device_response = "";

			TcpClient mClient = new TcpClient();

			//Set send command timeout
			mClient.SendTimeout = this.mSendCommandTimeOut;

			//Set receive response timeout
			mClient.ReceiveTimeout = this.mReceiveCommandTimeOut;

			//Get the message bytes
			byte[] mMessage = Encoding.ASCII.GetBytes(pCommand);

			//Get the bytes of encrypted message
			byte[] mEncryptedMessage = Common.EncryptMessage(mMessage, ProtocolType.TCP);

			//Connect to the device
			IAsyncResult result = mClient.BeginConnect(new IPAddress(this.mIP.GetAddressBytes()), 9999, null, null);
			bool success = result.AsyncWaitHandle.WaitOne(this.mConnectionTimeOut);

			if (success)
			{
				try
				{
					//Send the command
					using (NetworkStream stream = mClient.GetStream())
					{
						//Write the message
						stream.Write(mEncryptedMessage, 0, mEncryptedMessage.Length);

						//Read the response
						byte[] buffer = new byte[1024];
						using (MemoryStream ms = new MemoryStream())
						{
							int numBytesRead;
							while ((numBytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
							{
								ms.Write(buffer, 0, numBytesRead);
							}
							device_response = Encoding.ASCII.GetString(Common.DecryptMessage(ms.ToArray(), ProtocolType.TCP));
						}

						stream.Close();
					}
				}
				catch (Exception ex)
				{
					throw new NonCompatibleDeviceException(ex.Message, ex);
				}
				finally
				{
					//Close TCP connection
					if (mClient.Connected)
					{
						mClient.EndConnect(result);
					}
				}
			}
			else
			{
				throw new ConnectionErrorException("Unable to connect: " + this.mIP.ToString());
			}

			return device_response;
        }
        #endregion
        #region "Metodos publicos"
        #region "System Commands"
        /*
            System Commands
            ========================================
            Set MAC Address
            {"system":{"set_mac_addr":{"mac":"50-C7-BF-01-02-03"}}}

            Set Device ID
            {"system":{"set_device_id":{"deviceId":"0123456789ABCDEF0123456789ABCDEF01234567"}}}

            Set Hardware ID
            {"system":{"set_hw_id":{"hwId":"0123456789ABCDEF0123456789ABCDEF"}}}

            Set Location
            {"system":{"set_dev_location":{"longitude":6.9582814,"latitude":50.9412784}}}

            Perform uBoot Bootloader Check
            {"system":{"test_check_uboot":null}}

            Get Device Icon
            {"system":{"get_dev_icon":null}}

            Set Device Icon
            {"system":{"set_dev_icon":{"icon":"xxxx","hash":"ABCD"}}}

            Set Test Mode (command only accepted coming from IP 192.168.1.100)
            {"system":{"set_test_mode":{"enable":1}}}

            Download Firmware from URL
            {"system":{"download_firmware":{"url":"http://...."}}}

            Get Download State
            {"system":{"get_download_state":{}}}

            Flash Downloaded Firmware
            {"system":{"flash_firmware":{}}}

            Check Config
            {"system":{"check_new_config":null}} 



            EMeter Energy Usage Statistics Commands
            (for TP-Link HS110)
            ========================================
            Get Realtime Current and Voltage Reading
            {"emeter":{"get_realtime":{}}}

            Get EMeter VGain and IGain Settings
            {"emeter":{"get_vgain_igain":{}}}

            Set EMeter VGain and Igain
            {"emeter":{"set_vgain_igain":{"vgain":13462,"igain":16835}}}

            Start EMeter Calibration
            {"emeter":{"start_calibration":{"vtarget":13462,"itarget":16835}}}

            Get Daily Statistic for given Month
            {"emeter":{"get_daystat":{"month":1,"year":2016}}}

            Get Montly Statistic for given Year
            {"emeter":{""get_monthstat":{"year":2016}}}

            Erase All EMeter Statistics
            {"emeter":{"erase_emeter_stat":null}}

        */
        /// <summary>
        /// Reboot the device
        /// </summary>
        public void RebootDevice() 
        {
            //Prepare the command
            string comando = "{\"system\":{\"reboot\":{\"delay\":1}}}";

            //Send the command to the device
            this.ExecuteCommand(comando);
        }
        /// <summary>
        /// Reset the device (to factory settings)
        /// </summary>
        public void ResetDevice() 
        {
            //Prepare the command
            string comando = "{\"system\":{\"reset\":{\"delay\":1}}}";

            //Send the command to the device
            this.ExecuteCommand(comando);
        }
        /// <summary>
        /// Switch the relay state
        /// </summary>
        /// <param name="pNewRelayState">New relay state</param>
        /// <returns>Return the result of this operation</returns>
        public DeviceActionResult SwitchRelayState(RelayAction pNewRelayState) 
        {
            DeviceActionResult dev_result = new DeviceActionResult();

            //Prepare the command
            string comando = Common.StringParams("{\"system\":{\"set_relay_state\":{\"state\":{0}}}}", (pNewRelayState == RelayAction.TurnOn) ? "1" : "0");

			//Send the command to the device and get the Json string result
			string sJsonResult = this.ExecuteAndRead(comando);

			try
			{
				JObject json_object = JObject.Parse(sJsonResult);
				dev_result.LoadFromJson(json_object["system"]["set_relay_state"]);			
			}
			catch (Exception ex)
			{
				throw new NonCompatibleDeviceException(ex.Message, ex);
			}

            return dev_result;
        }
        /// <summary>
        /// Get the device info
        /// </summary>
        /// <returns>Return the device info</returns>
        /// <remarks>Software & Hardware Versions, MAC, deviceID, hwID, etc.</remarks>
        public DeviceInfo GetDeviceInfo()
        {
            DeviceInfo dev_info = new DeviceInfo();

            //Prepare the command
            string comando = "{\"system\":{\"get_sysinfo\":null}}";

			//Send the command to the device and get the Json string result
			string sJsonResult = this.ExecuteAndRead(comando);

			try
			{
				JObject json_object = JObject.Parse(sJsonResult);
				dev_info.LoadFromJson(json_object["system"]["get_sysinfo"]);
			}
			catch (Exception ex)
			{
				throw new NonCompatibleDeviceException(ex.Message, ex);
			}

            return dev_info;
        }
        /// <summary>
        /// Switch the led state
        /// </summary>
        /// <param name="pNewLedState">New led state</param>
        /// <returns>Return the result of this operation</returns>
        public DeviceActionResult SwitchLedState(LedAction pNewLedState) 
        {
            DeviceActionResult dev_result = new DeviceActionResult();

            //Prepare the command
            string comando = Common.StringParams("{\"system\":{\"set_led_off\":{\"off\":{0}}}}", (pNewLedState == LedAction.TurnOff) ? "1" : "0");

			//Send the command to the device and get the Json string result
			string sJsonResult = this.ExecuteAndRead(comando);

			try
			{
				JObject json_object = JObject.Parse(sJsonResult);
				dev_result.LoadFromJson(json_object["system"]["set_led_off"]);
			}
			catch (Exception ex)
			{
				throw new NonCompatibleDeviceException(ex.Message, ex);
			}

            return dev_result;
        }
        /// <summary>
        /// Set the device alias
        /// </summary>
        /// <param name="pNewAlias">New alias</param>
        /// <returns>Return the result of this operation</returns>
        /// <remarks>Only (A-Z a-z 0-9 @ ' . - _ spaces), other characters will be removed</remarks>
        public DeviceActionResult SetDeviceAlias(string pNewAlias)
        {
            DeviceActionResult dev_result = new DeviceActionResult();

            //Delete invalid characters
            pNewAlias = Regex.Replace(pNewAlias, @"[^A-Za-z0-9@'\.\-_\s]", "");

            //Prepare the command
            string comando = Common.StringParams("{\"system\":{\"set_dev_alias\":{\"alias\":\"{0}\"}}}", pNewAlias);

			//Send the command to the device and get the Json string result
			string sJsonResult = this.ExecuteAndRead(comando);

			try
			{
				JObject json_object = JObject.Parse(sJsonResult);
				dev_result.LoadFromJson(json_object["system"]["set_dev_alias"]);
			}
			catch (Exception ex)
			{
				throw new NonCompatibleDeviceException(ex.Message, ex);
			}

            return dev_result;
        }
        #endregion
        #region "Time Commands"
        /// <summary>
        /// Get the device time
        /// </summary>
        /// <returns>Return the device time</returns>
        public DeviceTime GetDeviceTime()
        {
            DeviceTime dev_time = new DeviceTime();

            //Prepare the command
            string comando = "{\"time\":{\"get_time\":null}}";

			//Send the command to the device and get the Json string result
			string sJsonResult = this.ExecuteAndRead(comando);

			try
			{
				JObject json_object = JObject.Parse(sJsonResult);
				dev_time.LoadFromJson(json_object["time"]["get_time"]);
			}
			catch (Exception ex)
			{
				throw new NonCompatibleDeviceException(ex.Message, ex);
			}

            return dev_time;
        }
        /// <summary>
        /// Get the device timezone
        /// </summary>
        /// <returns>Return the device timezone</returns>
        public DeviceTimeZone GetDeviceTimeZone()
        {
            DeviceTimeZone dev_time_zone = new DeviceTimeZone();

            //Prepare the command
            string comando = "{\"time\":{\"get_timezone\":null}}";

			//Send the command to the device and get the Json string result
			string sJsonResult = this.ExecuteAndRead(comando);

			try
			{
				JObject json_object = JObject.Parse(sJsonResult);
				dev_time_zone.LoadFromJson(json_object["time"]["get_timezone"]);
			}
			catch (Exception ex)
			{
				throw new NonCompatibleDeviceException(ex.Message, ex);
			}

            return dev_time_zone;
        }
        /// <summary>
        /// Set time zone
        /// </summary>
        /// <param name="pYear">Year</param>
        /// <param name="pMonth">Month</param>
        /// <param name="pDay">Day</param>
        /// <param name="pHour">Hour</param>
        /// <param name="pMinutes">Minutes</param>
        /// <param name="pSeconds">Seconds</param>
        /// <param name="pTimeZoneIndex">Time zone index</param>
        /// <returns>Return the result of this operation</returns>
        public DeviceActionResult SetTimeZone(int pYear, int pMonth, int pDay, byte pHour, byte pMinutes, byte pSeconds, byte pTimeZoneIndex) 
        {
            DeviceActionResult dev_result = new DeviceActionResult();

            //Prepare the command
            string comando = Common.StringParams("{\"time\":{\"set_timezone\":{\"year\":{0},\"month\":{1},\"mday\":{2},\"hour\":{3},\"min\":{4},\"sec\":{5},\"index\":{6}}}}", pYear.ToString(), pMonth.ToString(), pDay.ToString(), pHour.ToString(), pMinutes.ToString(), pSeconds.ToString(), pTimeZoneIndex.ToString());

			//Send the command to the device and get the Json string result
			string sJsonResult = this.ExecuteAndRead(comando);

			try
			{
				JObject json_object = JObject.Parse(sJsonResult);
				dev_result.LoadFromJson(json_object["time"]["set_timezone"]);
			}
			catch (Exception ex)
			{
				throw new NonCompatibleDeviceException(ex.Message, ex);
			}

            return dev_result;
        }
        #endregion
        #region "Count Down Commands"
        /// <summary>
        /// Add new count down rule
        /// </summary>
        /// <param name="pCountDownName">Count down rule name</param>
        /// <param name="pRelayAction">Action</param>
        /// <param name="pDelay">Delay (Seconds)</param>
        /// <returns>Return the result of this operation</returns>
        /// <remarks>Action to perform after number of seconds</remarks>
        public AddCountDownRuleResult AddCountDownRule(string pCountDownName, RelayAction pRelayAction, UInt32 pDelay) 
        {
            AddCountDownRuleResult dev_result = new AddCountDownRuleResult();

            //Check the delay limit. Can't be greater than 24 hours
            if (pDelay > 86399) //Max 23:59:59 (App only allow 23:59:00 = 86340)
            {
                pDelay = 86399;
            }

            //Prepare the command
            string comando = Common.StringParams("{\"count_down\":{\"add_rule\":{\"enable\":1,\"delay\":{0},\"act\":{1},\"name\":\"{2}\"}}}", pDelay.ToString(), (pRelayAction == RelayAction.TurnOn) ? "1" : "0", pCountDownName);

			//Send the command to the device and get the Json string result
			string sJsonResult = this.ExecuteAndRead(comando);

			try
			{
				JObject json_object = JObject.Parse(sJsonResult);
				dev_result.LoadFromJson(json_object["count_down"]["add_rule"]);
			}
			catch (Exception ex)
			{
				throw new NonCompatibleDeviceException(ex.Message, ex);
			}

            return dev_result;
        }
        /// <summary>
        /// Edit an existing count down rule
        /// </summary>
        /// <param name="pEnabled">Enable or Disable the current count down rule</param>
        /// <param name="pCountDownID">Count down ID</param>
        /// <param name="pDelay">Delay (Seconds)</param>
        /// <param name="pRelayAction">Action</param>
        /// <param name="pCountDownName">Count down rule name</param>
        /// <returns>Return the result of this operation</returns>
        public EditCountDownRuleResult EditCountDownRule(bool pEnabled, string pCountDownID, UInt32 pDelay, RelayAction pRelayAction, string pCountDownName)
        {
            EditCountDownRuleResult dev_result = new EditCountDownRuleResult();

            //Check the delay limit. Can't be greater than 24 hours
            if (pDelay > 86399) //Max 23:59:59 (App only allow 23:59:00 = 86340)
            {
                pDelay = 86399;
            }

            //Prepare the command
            string comando = Common.StringParams("{\"count_down\":{\"edit_rule\":{\"enable\":{0},\"id\":\"{1}\",\"delay\":{2},\"act\":{3},\"name\":\"{4}\"}}}", (pEnabled == true) ? "1" : "0", pCountDownID, pDelay.ToString(), (pRelayAction == RelayAction.TurnOn) ? "1" : "0", pCountDownName);

			//Send the command to the device and get the Json string result
			string sJsonResult = this.ExecuteAndRead(comando);

			try
			{
				JObject json_object = JObject.Parse(sJsonResult);
				dev_result.LoadFromJson(json_object["count_down"]["edit_rule"]);
			}
			catch (Exception ex)
			{
				throw new NonCompatibleDeviceException(ex.Message, ex);
			}

            return dev_result;
        }
        /// <summary>
        /// Delete specific count down rule
        /// </summary>
        /// <param name="pCountDownID">Count down ID</param>
        /// <returns>Return the result of this operation</returns>
        public DeviceActionResult DeleteCountDownRule(string pCountDownID) 
        {
            DeviceActionResult dev_result = new DeviceActionResult();

            //Prepare the command
            string comando = Common.StringParams("{\"count_down\":{\"delete_rule\":{\"id\":\"{0}\"}}}", pCountDownID);

			//Send the command to the device and get the Json string result
			string sJsonResult = this.ExecuteAndRead(comando);

			try
			{
				JObject json_object = JObject.Parse(sJsonResult);
				dev_result.LoadFromJson(json_object["count_down"]["delete_rule"]);
			}
			catch (Exception ex)
			{
				throw new NonCompatibleDeviceException(ex.Message, ex);
			}

            return dev_result;
        }
        /// <summary>
        /// Get all count down rules
        /// </summary>
        /// <returns>Return the list of count down rules</returns>
        /// <remarks>Only one allowed</remarks>
        public List<CountDownRuleInfo> GetCountDownRules() 
        {
            List<CountDownRuleInfo> count_down_rules = new List<CountDownRuleInfo>();
            //Prepare the command
            string comando = "{\"count_down\":{\"get_rules\":null}}";

			//Send the command to the device and get the Json string result
			string sJsonResult = this.ExecuteAndRead(comando);

			try
			{
				JObject json_object = JObject.Parse(sJsonResult);

				int error_code = json_object["count_down"]["get_rules"]["err_code"].Value<int>();

				if (error_code == 0)
				{
					foreach (JToken rule in json_object["count_down"]["get_rules"]["rule_list"])
					{
						CountDownRuleInfo count_down_rule = new CountDownRuleInfo();
						count_down_rule.LoadFromJson(rule);
						count_down_rules.Add(count_down_rule);
					}
				}
				else
				{
					//string error_message = json_object["count_down"]["get_rules"]["err_msg"].Value<string>();
					//TODO
				}
			}
			catch (Exception ex)
			{
				throw new NonCompatibleDeviceException(ex.Message, ex);
			}

            return count_down_rules;
        }
        /// <summary>
        /// Delete all count down rules
        /// </summary>
        /// <returns>Return the result of this operation</returns>
        public DeviceActionResult DeleteAllCountDownRules() 
        {
            DeviceActionResult dev_result = new DeviceActionResult();

            //Prepare the command
            string comando = "{\"count_down\":{\"delete_all_rules\":null}}";

			//Send the command to the device and get the Json string result
			string sJsonResult = this.ExecuteAndRead(comando);

			try
			{
				JObject json_object = JObject.Parse(sJsonResult);
				dev_result.LoadFromJson(json_object["count_down"]["delete_all_rules"]);
			}
			catch (Exception ex)
			{
				throw new NonCompatibleDeviceException(ex.Message, ex);
			}

            return dev_result;
        }
        #endregion
        #region "WLan Commands"
        /// <summary>
        /// Scan for list of available Access Points
        /// </summary>
        /// <returns>List of available Access Points</returns>
        public List<WLanAccessPointEntryResult> GetAvailableAccessPoints()
        {
            List<WLanAccessPointEntryResult> ap_list = new List<WLanAccessPointEntryResult>();
            //Prepare the command
            string comando = "{\"netif\":{\"get_scaninfo\":{\"refresh\":1}}}";

			//Send the command to the device and get the Json string result
			string sJsonResult = this.ExecuteAndRead(comando);

			try
			{
				JObject json_object = JObject.Parse(sJsonResult);

				int error_code = json_object["netif"]["get_scaninfo"]["err_code"].Value<int>();

				if (error_code == 0)
				{
					foreach (JToken ap_entry in json_object["netif"]["get_scaninfo"]["ap_list"])
					{
						WLanAccessPointEntryResult mAccessPointEntry = new WLanAccessPointEntryResult();
						mAccessPointEntry.LoadFromJson(ap_entry);
						ap_list.Add(mAccessPointEntry);
					}
				}
				else
				{
					//string error_message = json_object["netif"]["get_scaninfo"]["err_msg"].Value<string>();
					//TODO
				}
			}
			catch (Exception ex)
			{
				throw new NonCompatibleDeviceException(ex.Message, ex);
			}

            return ap_list;
        }
        /// <summary>
        /// Connect to AP with given SSID and Password
        /// </summary>
        /// <param name="pSSID">SSID</param>
        /// <param name="pPassword">Password</param>
        /// <param name="pWLanType">Wireless type</param>
        /// <returns>Return the result of this operation</returns>
        public DeviceActionResult SetAccessPoint(string pSSID, string pPassword, WirelessType pWLanType) 
        {
            DeviceActionResult dev_result = new DeviceActionResult();

            //Prepare the command
            string comando = "{\"netif\":{\"set_stainfo\":{\"ssid\":\"{0}\",\"password\":\"{1}\",\"key_type\":{2}}}}";

            comando = Common.StringParams(comando, pSSID, pPassword, Convert.ToString((byte)pWLanType));

			//Send the command to the device and get the Json string result
			string sJsonResult = this.ExecuteAndRead(comando);

			try
			{
				JObject json_object = JObject.Parse(sJsonResult);
				dev_result.LoadFromJson(json_object["netif"]["set_stainfo"]);
			}
			catch (Exception ex)
			{
				throw new NonCompatibleDeviceException(ex.Message, ex);
			}

            return dev_result;
        }
        #endregion
        #region Emeter commands
        public RealtimeEmeter GetEmeterRealtime()
        {
            var emeter_realtime = new RealtimeEmeter();

            var command = "{\"emeter\":{\"get_realtime\":{}}}";

            //Send the command to the device and get the Json string result
            string sJsonResult = this.ExecuteAndRead(command);

            try
            {
                JObject json_object = JObject.Parse(sJsonResult);
                emeter_realtime.LoadFromJson(json_object["emeter"]["get_realtime"]);
            }
            catch (Exception ex)
            {
                throw new NonCompatibleDeviceException(ex.Message, ex);
            }

            return emeter_realtime;
        }
        #endregion
        #endregion
        #region "Metodos estáticos"
        /// <summary>
        /// Get the wireless gateway IP address
        /// </summary>
        /// <returns>Return the wireless gateway IP address</returns>
        public static IPAddress GetGatewayIPAddress() 
        {
            return Common.GetGatewayIP(NetworkInterfaceType.Wireless80211);
        }
        /// <summary>
        /// Check the connection
        /// </summary>
        /// <param name="ip">IP address</param>
        /// <returns>Return True if the device is connected and False in other cases</returns>
        public static bool ConnectionCheck(IPAddress ip)
        {
            bool bResult = false;

            try
            {
                TcpClient mClient = new TcpClient();

                //Connect to the device
                IAsyncResult result = mClient.BeginConnect(new IPAddress(ip.GetAddressBytes()), 9999, null, null);
                bool success = result.AsyncWaitHandle.WaitOne(5000);

                if (success)
                {
                    //Close TCP connection
                    if (mClient.Connected)
                    {
                        mClient.EndConnect(result);
                    }

                    //Return the result
                    bResult = true;
                }
            }
            catch
            {
                bResult = false;
            }

            return bResult;
        }
        /// <summary>
        /// Send ping to device and determine if it's connected or not
        /// </summary>
        /// <param name="ip">IP address</param>
        /// <returns>Return True if the device is connected and False in other cases</returns>
        public static bool PingDevice(IPAddress ip)
        {
            bool pingable = false;
            Ping pinger = new Ping();

            try
            {
                PingReply reply = pinger.Send(ip);
                pingable = reply.Status == IPStatus.Success;
            }
            catch (PingException)
            {
                // Discard PingExceptions and return false;
            }

            return pingable;
        }
        /// <summary>
        /// Get the list of all connected devices
        /// </summary>
		/// <param name="pNetworkType">Network interface type used for scan the LAN</param>
		/// <param name="pMillisecondsTimeOut">Data reception timeout</param>
        /// <returns>Return the list of all connected devices</returns>
		public static Dictionary<IPAddress, DeviceInfo> GetAllDevices_TCP_Scanner(NetworkInterfaceType pNetworkType, int pMillisecondsTimeOut = 10)
		{
			Dictionary<IPAddress, DeviceInfo> devices = new Dictionary<IPAddress, DeviceInfo>();

			//Check the internet connection
			if (Common.IsNetworkAvailable())
			{
				IPAddress ip_address = Common.GetGatewayIP(pNetworkType);

				if (ip_address != null)
				{
					byte[] address = ip_address.GetAddressBytes();
					List<ThreadPoolDeviceInfo> mDeviceResults = new List<ThreadPoolDeviceInfo>();
					List<ManualResetEvent> mDoneEvents = new List<ManualResetEvent>();

					for (byte i = 1; i < 255; i++)
					{
						//Discart the gateway address
						if (i != address[3])
						{
							List<byte> bytes_ip = new List<byte>() { address[0], address[1], address[2], i };
							IPAddress ip = new IPAddress(bytes_ip.ToArray());
							ManualResetEvent mre = new ManualResetEvent(false);
							ThreadPoolDeviceInfo dct = new ThreadPoolDeviceInfo(ip, mre, pMillisecondsTimeOut, pMillisecondsTimeOut, pMillisecondsTimeOut);

							mDeviceResults.Add(dct);
							mDoneEvents.Add(mre);

							//Launch the thread
							ThreadPool.QueueUserWorkItem(dct.GetDeviceInfo_ThreadPoolCallback);

							//WaitHandle only support max 64 items
							if (mDoneEvents.Count == 64) 
							{
								//Wait for all threads in pool to calculation
								WaitHandle.WaitAll(mDoneEvents.ToArray());

								//Add the devices to the result list
								foreach (ThreadPoolDeviceInfo mDeviceItem in mDeviceResults.Where(p => ((p.Connected == true) && (p.NonCompatible == false))))
								{
									devices.Add(mDeviceItem.GetIP, mDeviceItem.GetDeviceInfo);
								}

								//Clear the lists
								mDeviceResults.Clear();
								mDoneEvents.Clear();
							}
						}
					}

					//We ensure that all results were processed
					if (mDeviceResults.Count > 0)
					{
						//Wait for all threads in pool to calculation
						WaitHandle.WaitAll(mDoneEvents.ToArray());

						//Add the devices to the result list
						foreach (ThreadPoolDeviceInfo mDeviceItem in mDeviceResults.Where(p => ((p.Connected == true) && (p.NonCompatible == false))))
						{
							devices.Add(mDeviceItem.GetIP, mDeviceItem.GetDeviceInfo);
						}						
					}
				}
			}

			return devices;
		}
		/// <summary>
		/// Get the list of all connected devices
		/// </summary>
		/// <param name="pNetworkType">Network interface type used for scan the LAN</param>
		/// <param name="pMillisecondsReceiveTimeOut">Milliseconds to end the receive period</param>
		/// <returns>Return the list of all connected devices</returns>
		public static Dictionary<IPAddress, DeviceInfo> GetAllDevices_UDP_Broadcast(NetworkInterfaceType pNetworkType, int pMillisecondsReceiveTimeOut = 2000)
		{
			DiscoverSmartPlugs mDiscover = new DiscoverSmartPlugs();
			return mDiscover.GetAllDevices(pNetworkType, pMillisecondsReceiveTimeOut);
		}
		#endregion
	}
}
