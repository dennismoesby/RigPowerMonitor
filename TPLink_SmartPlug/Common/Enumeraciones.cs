using System.ComponentModel;

namespace TPLink_SmartPlug
{
	public enum ProtocolType
	{
		TCP = 0,
		UDP = 1,
	}
	public enum LedAction
    {
		[Description("Turn ON")]
		TurnOn = 0, //Day mode

		[Description("TurnOFF")]
		TurnOff = 1, //Night mode
    }
    public enum RelayAction
    {
		[Description("Turn ON")]
		TurnOff = 0,

		[Description("Turn OFF")]
		TurnOn = 1,
    }
    public enum WirelessType 
    {
		[Description("Open")]
        Open = 0,

		[Description("WEP")]
		WEP = 1,

		[Description("WPA")]
		WPA = 2,

		[Description("WPA 2")]
		WPA2 = 3,
    }
}
