# RigPowerMonitor
----------------------------------
RIG POWER CONSUMPTION MONITOR v1.1
----------------------------------

By Dennis Moesby.

WeMo API: https://github.com/seanksullivan/Wemo.net

TP-Link API: https://www.codeproject.com/Tips/1169091/How-to-Control-TP-Link-Smart-Plug-HS-XX

Monitors the power consumption of a smart plug and turns it off and back on again if the power consumption drops below a specified threshold. useful to force a mining rig to reboot if it hangs. 
Remember to set bios on the miner to automatically boot after a power failure.

Usage: RigPowerMonitor.exe [-OPTIONS]

Options:

-t&nbsp;&nbsp;&nbsp;&nbsp;Smart Plug Type. 0 = WeMo Insight Switch. 1 = TP-Link HS110. Default: 0.

-a&nbsp;&nbsp;&nbsp;&nbsp;Ip address of the smart plug to monitor. Required.

-p&nbsp;&nbsp;&nbsp;&nbsp;Power Consumption Threshold. The lowest allowed power consumption of the mining rig before powering off and back on. Required.

-w&nbsp;&nbsp;&nbsp;&nbsp;Seconds to wait for the power consumption to go back up over the threshold again before powering off. Default: 300.

-o&nbsp;&nbsp;&nbsp;&nbsp;Seconds to wait before powering back on after the power has been cut. Default: 30.

-tp&nbsp;&nbsp;&nbsp;Display a list of available TP-Link devices with their corresponding IP address.

-h&nbsp;&nbsp;&nbsp;&nbsp;Display this help.

Supported Smart Plugs:

Belkin WeMo Insight Switch

TP-Link HS 110 Smart Plug

This tool is provided as is. If you like it, consider donating:
BTC (3Lm6h9Zb5R8ov6yZwoPnbDd4apGw9itqpD) or ETH (0x0CC2b5257BC86D5D744fb93c681A1aBa0fFc8d4E)


IP Addresses and Ports

The IP address of the WeMo switch can be found via the WeMo app for your smart phone. WeMo switch uses port 49153 (TCP and UDP)

The IP address of the TP-Link can be found by running RigPowerMonitor -tp. TP-Link plug uses port 9999 (TCP and UDP).
