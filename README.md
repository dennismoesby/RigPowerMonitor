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

OPTIONS:

-t&nbsp;&nbsp;&nbsp;&nbsp;Smart Plug Type. 0 = WeMo Insight Switch. 1 = TP-Link HS110. Default: 0.

-a&nbsp;&nbsp;&nbsp;&nbsp;Ip address of the smart plug to monitor. Required.

-p&nbsp;&nbsp;&nbsp;&nbsp;Power Consumption Threshold. The lowest allowed power consumption of the mining rig before powering off and back on. Required.

-w&nbsp;&nbsp;&nbsp;&nbsp;Seconds to wait for the power consumption to go back up over the threshold again before powering off. Default: 300.

-o&nbsp;&nbsp;&nbsp;&nbsp;Seconds to wait before powering back on after the power has been cut. Default: 30.

-l&nbsp;&nbsp;&nbsp;&nbsp;File logging level. 0 = Log everything. 1 = Log warnings and errors. 2 = Log only errors. -1 = Turn off logging. Default: 1.

-tb-key&nbsp;&nbsp;&nbsp;&nbsp;textbelt.com API key. To get a text message when plug is powered off and back on. Visit www.textbelt.com to generate key and fund account. Requires -tb-num option also.

-tb-num&nbsp;&nbsp;&nbsp;&nbsp;Mobile phone number to send text message to when plug is powered off and back on. Use international format including +<country code>, i.e. +4512345678 for Denmark or +1123456789 for USA. Requires -tb-key option also.
  
-tp&nbsp;&nbsp;&nbsp;Display a list of available TP-Link devices with their corresponding IP address.

-h&nbsp;&nbsp;&nbsp;&nbsp;Display this help.

SUPPORTED SMART PLUGS:

- Belkin WeMo Insight Switch

- TP-Link HS 110 Smart Plug

IP ADRESSES AND PORTS:

The IP address of the WeMo switch can be found via the WeMo app for your smart phone. WeMo switch uses port 49153 (TCP and UDP)

The IP address of the TP-Link can be found by running RigPowerMonitor -tp. TP-Link plug uses port 9999 (TCP and UDP).

DISCLAIMER AND DONATION:

This tool is provided as is with no guarantees or warranties. If you chose to use it, it's at your own responsibility. If you like it, consider donating:
BTC (3Lm6h9Zb5R8ov6yZwoPnbDd4apGw9itqpD) or ETH (0x0CC2b5257BC86D5D744fb93c681A1aBa0fFc8d4E)

