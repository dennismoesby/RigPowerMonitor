using System;
using System.Net;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;

namespace TPLink_SmartPlug
{
    internal sealed class Common
    {
        internal static string StringParams(string value, params string[] parameters)
        {
            string result = value;
            string expression = @"(\{[0-9]+\}){1}";
            MatchCollection foundResults;
            Match foundResult;

            if (parameters != null)
            {
                foundResults = Regex.Matches(value, expression);
                for (int i = foundResults.Count - 1; i >= 0; i--)
                {
                    foundResult = foundResults[i];
                    int indexParam = 0;
                    if (int.TryParse((foundResult.Value.Substring(1, foundResult.Value.Length - 2)), out indexParam) == true)
                    {
                        if (indexParam < parameters.Length)
                        {
                            result = result.Remove(foundResult.Index, foundResult.Length);
                            result = result.Insert(foundResult.Index, parameters[indexParam]);
                        }
                    }
                }
            }

            return result;
        }

        internal static IPAddress GetLocalIPv4(NetworkInterfaceType _type)
        {
            IPAddress output = null;
            foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (item.NetworkInterfaceType == _type && item.OperationalStatus == OperationalStatus.Up)
                {
                    IPInterfaceProperties adapterProperties = item.GetIPProperties();

                    if (adapterProperties.GatewayAddresses.FirstOrDefault() != null)
                    {
                        foreach (UnicastIPAddressInformation ip in adapterProperties.UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                output = ip.Address;
                            }
                        }
                    }
                }
            }

            return output;
        }

		internal static IPAddress GetLocalIPv4Mask(NetworkInterfaceType _type)
		{
			IPAddress output = null;
			foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
			{
				if (item.NetworkInterfaceType == _type && item.OperationalStatus == OperationalStatus.Up)
				{
					IPInterfaceProperties adapterProperties = item.GetIPProperties();

					if (adapterProperties.GatewayAddresses.FirstOrDefault() != null)
					{
						foreach (UnicastIPAddressInformation ip in adapterProperties.UnicastAddresses)
						{
							if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
							{
								output = ip.IPv4Mask;
							}
						}
					}
				}
			}

			return output;
		}

		internal static IPAddress GetGatewayIP(NetworkInterfaceType _type)
        {
            IPAddress output = null;
            foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (item.NetworkInterfaceType == _type && item.OperationalStatus == OperationalStatus.Up)
                {
                    IPInterfaceProperties adapterProperties = item.GetIPProperties();

                    if (adapterProperties.GatewayAddresses.FirstOrDefault() != null)
                    {
                        output = adapterProperties.GatewayAddresses[0].Address;
                    }
                }
            }

            return output;
        }

        internal static bool IsNetworkAvailable() 
        {
            return NetworkInterface.GetIsNetworkAvailable();
        }

		internal static IPAddress GetBroadcastAddress(IPAddress address, IPAddress mask)
		{
			uint ipAddress = BitConverter.ToUInt32(address.GetAddressBytes(), 0);
			uint ipMaskV4 = BitConverter.ToUInt32(mask.GetAddressBytes(), 0);
			uint broadCastIpAddress = ipAddress | ~ipMaskV4;

			return new IPAddress(BitConverter.GetBytes(broadCastIpAddress));
		}

		/// <summary>
		/// Encrypt the command message
		/// </summary>
		/// <param name="pMessage">Message</param>
		/// <param name="pProtocolType">Protocol type</param>
		/// <returns>Returns the encrypted bytes of the message</returns>
		internal static byte[] EncryptMessage(byte[] pMessage, ProtocolType pProtocolType)
        {
            List<byte> mBuffer = new List<byte>();
            int key = 0xAB;

            if ((pMessage != null) && (pMessage.Length > 0))
            {
                //Añadimos el prefijo del mensaje
				if (pProtocolType == ProtocolType.TCP)
				{
					mBuffer.Add(0x00);
					mBuffer.Add(0x00);
					mBuffer.Add(0x00);
					mBuffer.Add(0x00);
				}

				//Codificamos el mensaje
				for (int i = 0; i < pMessage.Length; i++)
                {
                    byte b = (byte)(key ^ pMessage[i]);
                    key = b;
                    mBuffer.Add(b);
                }
            }

            return mBuffer.ToArray();
        }

		/// <summary>
		/// Decrypt the message
		/// </summary>
		/// <param name="pMessage">Message</param>
		/// <param name="pProtocolType">Protocol type</param>
		/// <returns>Returns the decrypted message</returns>
		internal static byte[] DecryptMessage(byte[] pMessage, ProtocolType pProtocolType)
        {
            List<byte> mBuffer = new List<byte>();
            int key = 0xAB;

			//Skip the first 4 bytes in TCP communications (4 bytes header)
			byte header = (pProtocolType == ProtocolType.UDP) ? (byte)0x00 : (byte)0x04;

            if ((pMessage != null) && (pMessage.Length > 0))
            {
                for (int i = header; i < pMessage.Length; i++)
                {
                    byte b = (byte)(key ^ pMessage[i]);
                    key = pMessage[i];
                    mBuffer.Add(b);
                }
            }

            return mBuffer.ToArray();
        }
    }
}
