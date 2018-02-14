using System;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Net.NetworkInformation;

using Newtonsoft.Json.Linq;

namespace TPLink_SmartPlug
{
	internal sealed class DiscoverSmartPlugs
	{
		#region "Variables globales"
		private UdpClient mUDP_Client;
		private Dictionary<IPAddress, string> mListDevices;
		#endregion
		#region "Constructor"
		public DiscoverSmartPlugs() 
		{
			this.mUDP_Client = new UdpClient(9999);
			this.mListDevices = new Dictionary<IPAddress, string>();

			//Enable broadcast messages
			this.mUDP_Client.EnableBroadcast = true;
		}
		#endregion
		#region "Metodos privados"
		private void StartListening() 
		{
			this.mUDP_Client.BeginReceive(new AsyncCallback(this.DataReceiver), null);
		}
		private void DataReceiver(IAsyncResult res)
		{
			try
			{
				IPEndPoint mRemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 9999);
				byte[] received = mUDP_Client.EndReceive(res, ref mRemoteIpEndPoint);

				string returnData = Encoding.ASCII.GetString(Common.DecryptMessage(received, ProtocolType.UDP));

				//Add string result to the list of device answers
				this.mListDevices.Add(mRemoteIpEndPoint.Address, returnData);

				//Listen again
				this.StartListening();
			}
			catch (ObjectDisposedException obe)
			{
				//Socket was closed (expected error)
			}
		}
		#endregion
		#region "Metodos publicos"
		/// <summary>
		/// Get the list of all connected devices
		/// </summary>
		/// <param name="pNetworkType">Network type</param>
		/// <param name="pMillisecondsReceiveTimeOut">Milliseconds to end the receive period</param>
		/// <returns>Return the list of all connected devices</returns>
		public Dictionary<IPAddress, DeviceInfo> GetAllDevices(NetworkInterfaceType pNetworkType, int pMillisecondsReceiveTimeOut)
		{
			Dictionary<IPAddress, DeviceInfo> mDevices = new Dictionary<IPAddress, DeviceInfo>();

			try
			{		
				//Start listening
				this.StartListening();

				//Check the internet connection
				if (Common.IsNetworkAvailable())
				{
					IPAddress ip_address = Common.GetLocalIPv4(pNetworkType);

					if (ip_address != null)
					{
						IPAddress ip_mask = Common.GetLocalIPv4Mask(pNetworkType);
						IPAddress ip_broadcast = Common.GetBroadcastAddress(ip_address, ip_mask);

						this.mListDevices.Clear();

						byte[] mMessage = Encoding.ASCII.GetBytes("{\"system\":{\"get_sysinfo\":{}}}");
						byte[] mEncryptedMessage = Common.EncryptMessage(mMessage, ProtocolType.UDP);

						//Send broadcast query
						this.mUDP_Client.Send(mEncryptedMessage, mEncryptedMessage.Length, new IPEndPoint(ip_broadcast, 9999));

						//Wait 2 seconds
						System.Threading.Thread.Sleep(pMillisecondsReceiveTimeOut);

						//Scan and convert to DeviceInfo each answer
						foreach (KeyValuePair<IPAddress, string> mInfo in this.mListDevices)
						{
							try
							{
								//Discart the current IP address
								if (mInfo.Key.Equals(ip_address) == false)
								{
									DeviceInfo mDeviceInfo = new DeviceInfo();
									JObject json_object = JObject.Parse(mInfo.Value);
									mDeviceInfo.LoadFromJson(json_object["system"]["get_sysinfo"]);
									mDevices.Add(mInfo.Key, mDeviceInfo);
								}
							}
							catch
							{
								//Non compatible device
							}
						}
					}
				}
			}
			finally
			{
				this.mUDP_Client.Client.Shutdown(SocketShutdown.Both);
				this.mUDP_Client.Close();
			}

			return mDevices;
		}
		#endregion
	}
}
