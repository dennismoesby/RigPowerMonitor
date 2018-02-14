using Communications.Responses;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TPLink_SmartPlug;

namespace RigPowerMonitor
{
    class Program
    {
        static string IpAddress;
        static int PowerConsumptionThreshold;
        static int SecondsToWaitAfterPowerDecline;
        static int SecondsToWaitBeforePoweringBackOn;
        static SmartPlugs PlugType;

        static bool powerHasDroppedBelowThreshold;
        static Stopwatch timeSincePowerDroppedBelowThreshold;
        static string PlugFriendlyName;
        static string ApiAddress;
        static string previousLogMessage;
        static bool previousLogMessageOverwrite;

        static void Main(string[] args)
        {
            try
            {
                showHeader();

                if (args == null || args.Length == 0)
                {
                    Console.WriteLine("ERROR: Required options not set. Use -h for help.");
                }
                else if (args.Any(x => x.Equals("-h", StringComparison.InvariantCultureIgnoreCase)))
                {
                    showHelp();
                }
                else if(args.Any(x=> x.Equals("-tp", StringComparison.InvariantCultureIgnoreCase)))
                {
                    showTpLinkDevices();
                }
                else
                {
                    parseArgs(args);

                    if (string.IsNullOrWhiteSpace(IpAddress))
                        Console.WriteLine("ERROR: Ip address not set. Use -h for help.");
                    else if (PowerConsumptionThreshold == 0)
                        Console.WriteLine("ERROR: Minimum power consumption threshold not set. Use -h for help.");
                    else
                    {
                        if (SecondsToWaitAfterPowerDecline == 0) SecondsToWaitAfterPowerDecline = 300;
                        if (SecondsToWaitBeforePoweringBackOn == 0) SecondsToWaitBeforePoweringBackOn = 30;
                        ApiAddress = "http://" + IpAddress;

                        try
                        {
                            getPlugName();
                            Console.WriteLine($"Connected to: {PlugFriendlyName} ({IpAddress}).");
                            Console.WriteLine($"Plug type: {(PlugType == SmartPlugs.WeMoInsightSwitch ? "Belkin WeMo Insight Switch" : "TP-Link HS110")}");
                            Console.WriteLine($"Min. power consumption threshold: {PowerConsumptionThreshold} W.");
                            Console.WriteLine($"Wait time before power off: {SecondsToWaitAfterPowerDecline} seconds.");
                            Console.WriteLine($"Wait time before power back on: {SecondsToWaitBeforePoweringBackOn} seconds.");
                            Console.WriteLine("");

                            showDonationInfo();
                            Monitor();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"ERROR: Can't connect to WeMo Insight Switch on ip address {IpAddress}. Error message: {ex.Message}");
                        }

                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }

        private static void getPlugName()
        {
            try
            {
                var pHander = new SmartPlugHandler(PlugType, IpAddress);
                PlugFriendlyName = pHander.Name;
            }
            catch
            {
                throw;
            }
        }

        private static void Monitor()
        {
            try
            {
                logMessage("Monitoring started.");
                timeSincePowerDroppedBelowThreshold = new Stopwatch();

                do
                {
                    var pHandler = new SmartPlugHandler(PlugType, IpAddress);
                    var plugState = pHandler.GetState();

                    if (plugState == SmartPlugState.unknown)
                        logMessage("Could not determine the state of the smart plug.");
                    else
                    {
                        if (plugState == SmartPlugState.off)
                        {
                            logMessage($"{PlugFriendlyName} is OFF.");
                            break;
                        }
                        else
                        {
                            var plugPower = pHandler.GetCurrentPowerConsumption();

                            var msg = $"Power: {Math.Round(plugPower, 0, MidpointRounding.AwayFromZero)} W. ";

                            if (powerHasDroppedBelowThreshold)
                            {
                                // Power consumption has previously dropped below the threshold and we're waiting for it to either go back up or for the timer to run out before powering off.

                                if (plugPower < PowerConsumptionThreshold && timeSincePowerDroppedBelowThreshold.ElapsedMilliseconds > (SecondsToWaitAfterPowerDecline * 1000))
                                {
                                    logMessage("Monitoring paused.");
                                    PowerOffAndOn();
                                    logMessage("Monitoring resumed.");

                                    timeSincePowerDroppedBelowThreshold.Stop();
                                    timeSincePowerDroppedBelowThreshold.Reset();
                                    powerHasDroppedBelowThreshold = false;
                                }
                                else if (plugPower < PowerConsumptionThreshold)
                                {
                                    var timeToWait = new TimeSpan(0, 0, SecondsToWaitAfterPowerDecline).Subtract(timeSincePowerDroppedBelowThreshold.Elapsed);

                                    msg += $"Power consumption below threshold. Powering off in {timeToWait.Minutes}:{timeToWait.Seconds.ToString().PadLeft(2, '0')}.";
                                    logMessage(msg, true);
                                }
                                else
                                {
                                    powerHasDroppedBelowThreshold = false;
                                    timeSincePowerDroppedBelowThreshold.Stop();
                                    timeSincePowerDroppedBelowThreshold.Reset();

                                    msg += $"Power consumption is back up over threshold. Powering off cancelled.";
                                    logMessage(msg);
                                }
                            }
                            else
                            {
                                // Power consumption has not dropped below threshold. Normal monitoring...

                                if (plugPower < PowerConsumptionThreshold)
                                {
                                    // power just dropped below threshold. Starting count down

                                    var timeToWait = new TimeSpan(0, 0, SecondsToWaitAfterPowerDecline);
                                    powerHasDroppedBelowThreshold = true;
                                    timeSincePowerDroppedBelowThreshold.Start();

                                    msg += $"Power consumption below threshold. Powering off in {timeToWait.Minutes}:{timeToWait.Seconds.ToString().PadLeft(2, '0')}.";
                                    logMessage(msg, true);
                                }
                                else
                                {
                                    logMessage(msg, true);
                                }
                            }
                        }
                    }

                    Thread.Sleep(1000);

                } while (true);
            }
            catch (Exception ex)
            {
                logMessage($"ERROR: {ex.Message}");
            }

            logMessage("Monitoring stopped.");
        }

        private static void PowerOffAndOn()
        {
            try
            {
                logMessage("Powering off.");

                var pHandler = new SmartPlugHandler(PlugType, IpAddress);
                var result = pHandler.SetPlugState(SmartPlugState.off);

                logMessage($"{PlugFriendlyName} succesfully switched off.");

                var sw = new Stopwatch();
                sw.Start();
                var timeToWait = new TimeSpan(0, 0, SecondsToWaitBeforePoweringBackOn);

                while (timeToWait.TotalMilliseconds > 0)
                {
                    logMessage($"Powering back on in {timeToWait.Minutes}:{timeToWait.Seconds.ToString().PadLeft(2, '0')}.", true);
                    Thread.Sleep(1000);
                    timeToWait = new TimeSpan(0, 0, SecondsToWaitBeforePoweringBackOn).Subtract(sw.Elapsed);
                }
                sw.Stop();
                sw.Reset();

                logMessage("Powering on.");
                result = pHandler.SetPlugState(SmartPlugState.on);
                logMessage($"{PlugFriendlyName} succesfully switched on.");

                sw.Start();
                timeToWait = new TimeSpan(0, 0, SecondsToWaitAfterPowerDecline);
                while (timeToWait.TotalMilliseconds > 0)
                {
                    logMessage($"Resuming monitoring in {timeToWait.Minutes}:{timeToWait.Seconds.ToString().PadLeft(2, '0')}.", true);
                    Thread.Sleep(1000);
                    timeToWait = new TimeSpan(0, 0, SecondsToWaitAfterPowerDecline).Subtract(sw.Elapsed);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Exception during Power Off and On. Message: {ex.Message}");
            }
        }

        private static void logMessage(string messsage, bool overwrite = false)
        {
            try
            {
                var msg = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} {messsage}";
                if (!overwrite)
                {
                    if (previousLogMessageOverwrite)
                        Console.Write("\n");

                    Console.WriteLine(msg);
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(previousLogMessage) && msg.Length < previousLogMessage.Length)
                        msg = msg.PadRight(previousLogMessage.Length, ' ');

                    Console.Write("\r{0}", msg);
                }

                previousLogMessage = msg.Trim();
                previousLogMessageOverwrite = overwrite;
            }
            catch
            {
                // do nothing.
            }
        }


        private static void parseArgs(string[] args)
        {
            try
            {
                int _argno = 0;
                while (_argno < args.Length)
                {
                    if (args[_argno].Equals("-a", StringComparison.InvariantCultureIgnoreCase))
                    {
                        _argno++;
                        if (_argno < args.Length)
                            if (!args[_argno].StartsWith("-"))
                                IpAddress = args[_argno];
                    }
                    else if (args[_argno].Equals("-p", StringComparison.InvariantCultureIgnoreCase))
                    {
                        _argno++;
                        if (_argno < args.Length)
                            if (!args[_argno].StartsWith("-"))
                                int.TryParse(args[_argno], out PowerConsumptionThreshold);
                    }
                    else if (args[_argno].Equals("-w", StringComparison.InvariantCultureIgnoreCase))
                    {
                        _argno++;
                        if (_argno < args.Length)
                            if (!args[_argno].StartsWith("-"))
                                int.TryParse(args[_argno], out SecondsToWaitAfterPowerDecline);
                    }
                    else if (args[_argno].Equals("-o", StringComparison.InvariantCultureIgnoreCase))
                    {
                        _argno++;
                        if (_argno < args.Length)
                            if (!args[_argno].StartsWith("-"))
                                int.TryParse(args[_argno], out SecondsToWaitBeforePoweringBackOn);
                    }
                    else if (args[_argno].Equals("-t", StringComparison.InvariantCultureIgnoreCase))
                    {
                        _argno++;
                        if (_argno < args.Length)
                            if (!args[_argno].StartsWith("-"))
                            {
                                int _plugtype = 0;
                                int.TryParse(args[_argno], out _plugtype);
                                PlugType = (SmartPlugs)_plugtype;
                            }
                    }
                    else
                        throw new Exception($"Unknown option '{args[_argno]}'. Use -h for help.");

                    _argno++;
                }
            }
            catch 
            {
                throw;
            }
        }

        private static void showHeader()
        {
            Console.WriteLine("-----------------------------------------------");
            Console.WriteLine("RIG POWER CONSUMPTION MONITOR v1.1 - 2018-02-14");
            Console.WriteLine("-----------------------------------------------");
            Console.WriteLine("");
            Console.WriteLine("By Dennis Moesby. (http://https://github.com/dennismoesby/RigPowerMonitor)");
            Console.WriteLine("WeMo API: https://github.com/seanksullivan/Wemo.net");
            Console.WriteLine("TP-Link API: https://www.codeproject.com/Tips/1169091/How-to-Control-TP-Link-Smart-Plug-HS-XX");
            Console.WriteLine("");
        }

        private static void showDonationInfo()
        {
            Console.WriteLine("This tool is provided as is. If you like it, consider donating:");
            Console.WriteLine("BTC (3Lm6h9Zb5R8ov6yZwoPnbDd4apGw9itqpD) or ETH (0x0CC2b5257BC86D5D744fb93c681A1aBa0fFc8d4E)");
            Console.WriteLine("");
        }

        private static void showHelp()
        {
            Console.WriteLine("Monitors the power consumption of a smart plug and turns it off and back on again");
            Console.WriteLine("if the power consumption drops below a specified threshold. useful to force a mining rig to reboot if it hangs.");
            Console.WriteLine("Remember to set bios on the miner to automatically boot after a power failure.");
            Console.WriteLine("");
            Console.WriteLine("Usage: RigPowerMonitor.exe [-OPTIONS]");
            Console.WriteLine("");
            Console.WriteLine("Options:");
            Console.WriteLine("");
            Console.WriteLine("-t   Smart Plug Type. 0 = WeMo Insight Switch. 1 = TP-Link HS110. Default: 0.");
            Console.WriteLine("-a   Ip address of the smart plug to monitor. Required.");
            Console.WriteLine("-p   Power Consumption Threshold. The lowest allowed power consumption of the mining rig before powering off and back on. Required.");
            Console.WriteLine("-w   Seconds to wait for the power consumption to go back up over the threshold again before powering off. Default: 300.");
            Console.WriteLine("-o   Seconds to wait before powering back on after the power has been cut. Default: 30.");
            Console.WriteLine("-tp  Display a list of available TP-Link devices with their corresponding IP address.");
            Console.WriteLine("-h   Display this help.");
            Console.WriteLine("");

            showDonationInfo();
        }

        private static void showTpLinkDevices()
        {
            try
            {
                Console.WriteLine("Available TP-Link Devices");
                Console.WriteLine("");
                Console.WriteLine("IPv4 address    Name");
                Console.WriteLine("--------------------------------------------------------------------------------------");

                foreach (NetworkInterfaceType ntype in Enum.GetValues(typeof(NetworkInterfaceType)))
                    foreach (KeyValuePair<IPAddress, DeviceInfo> dev_info in HS1XX.GetAllDevices_UDP_Broadcast(ntype, 2000))
                        Console.WriteLine($"{dev_info.Key.ToString().PadRight(15,' ')} {dev_info.Value.Alias}");

                Console.WriteLine("");
                showDonationInfo();
            }
            catch
            {
                throw;
            }
        }
    }
}
