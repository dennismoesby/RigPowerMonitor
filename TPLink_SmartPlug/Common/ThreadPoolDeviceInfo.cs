using System;
using System.Net;
using System.Threading;

using TPLink_SmartPlug.Exceptions;

namespace TPLink_SmartPlug
{
	public class ThreadPoolDeviceInfo
	{
		#region "Variables globales"
		private IPAddress mIP;
		private bool mConnected;
		private bool mNonCompatible;
		private ManualResetEvent mDoneEvent;
		private int mConnectionTimeOut;
		private int mSendTimeOut;
		private int mReceiveTimeOut;
		private DeviceInfo mDevice;
		#endregion
		#region "Propiedades"
		public IPAddress GetIP
		{
			get
			{
				return this.mIP;
			}
		}
		public bool Connected
		{
			get 
			{
				return this.mConnected;
			}
			set 
			{
				this.mConnected = value;
			}
		}
		public bool NonCompatible
		{
			get 
			{
				return this.mNonCompatible;
			}
			set 
			{
				this.mNonCompatible = value;
			}
		}
		public DeviceInfo GetDeviceInfo
		{
			get
			{
				return this.mDevice;
			}
		}
		#endregion
		#region "Constructor"
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="pIP">Device IP Address</param>
		/// <param name="pDoneEvent">Done event signal</param>
		/// <param name="pConnectionTimeOut">Connection timeout (Milliseconds)</param>
		/// <param name="pSendTimeOut">Send command timeout (Milliseconds)</param>
		/// <param name="pReceiveTimeOut">Receive answer timeout (Milliseconds)</param>
		public ThreadPoolDeviceInfo(IPAddress pIP, ManualResetEvent pDoneEvent, int pConnectionTimeOut, int pSendTimeOut, int pReceiveTimeOut)
		{
			mConnected = false;
			mNonCompatible = false;
			mIP = pIP;
			mDoneEvent = pDoneEvent;
			mConnectionTimeOut = pConnectionTimeOut;
			mSendTimeOut = pSendTimeOut;
			mReceiveTimeOut = pReceiveTimeOut;
		}
		#endregion
		#region "Metodos publicos"
		public void GetDeviceInfo_ThreadPoolCallback(Object threadContext)
		{
			this.mDoneEvent.Reset();
			this.mConnected = false;
			this.mNonCompatible = false;
			this.mDevice = null;

			try
			{
				HS1XX mDeviceManager = new HS1XX(this.mIP, mConnectionTimeOut, mSendTimeOut, mReceiveTimeOut);
				this.mDevice = mDeviceManager.GetDeviceInfo();
				this.mConnected = true;
				this.mNonCompatible = false;
			}
			catch (Exception ex)
			{
				this.mNonCompatible = (ex.GetType() == typeof(NonCompatibleDeviceException));
				this.mConnected = (ex.GetType() != typeof(ConnectionErrorException)); ;
				this.mDevice = null;
			}
			finally
			{
				this.mDoneEvent.Set();
			}
		}
		#endregion
	}
}
