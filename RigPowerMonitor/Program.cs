using Communications.Responses;
using RigPowerMonitor.Api;
using RigPowerMonitor.Api.Handlers;
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
        static Api.ObjectModel.RpmMonitorSettings Settings;
        static string previousLogMessage;
        static bool previousLogMessageOverwrite;

        static void Main(string[] args)
        {
            try
            {
                //args = new string[] { "-a", "192.168.1.118", "-t", "0", "-p", "700", "-w", "10", "-o", "10", "-l", "-1" };
                //args = new string[] { "-tp" };

                Settings = new Api.ObjectModel.RpmMonitorSettings();

                showHeader();

                if (args == null || args.Length == 0)
                {
                    Console.WriteLine("ERROR: Required options not set. Use -h for help.");
                }
                else if (args.Any(x => x.Equals("-h", StringComparison.InvariantCultureIgnoreCase)))
                {
                    showHelp();
                }
                else if (args.Any(x => x.Equals("-tp", StringComparison.InvariantCultureIgnoreCase)))
                {
                    showTpLinkDevices();
                }
                else
                {
                    parseArgs(args);

                    if (string.IsNullOrWhiteSpace(Settings.IpAddress))
                        Console.WriteLine("ERROR: Ip address not set. Use -h for help.");
                    else if (Settings.PowerConsumptionThreshold == 0)
                        Console.WriteLine("ERROR: Minimum power consumption threshold not set. Use -h for help.");
                    else if (!string.IsNullOrWhiteSpace(Settings.TextbeltApiKey) && string.IsNullOrWhiteSpace(Settings.MobileNumber))
                        Console.WriteLine("ERROR: Textbelt API key set, but mobile number is not set. Use -h for help.");
                    else if (string.IsNullOrWhiteSpace(Settings.TextbeltApiKey) && !string.IsNullOrWhiteSpace(Settings.MobileNumber))
                        Console.WriteLine("ERROR: Mobile number is set, but Textbelt API key is not set. Use -h for help.");
                    else
                    {
                        showDonationInfo();

                        using (var monitor = new MonitorHandler(Settings))
                        {
                            monitor.OnLogEntriesAdded += Monitor_OnLogEntriesAdded;
                            monitor.OnLogOperationFailed += Monitor_OnLogOperationFailed;
                            monitor.OnSmartPlugPoweredOffAndOn += Monitor_OnSmartPlugPoweredOffAndOn;

                            monitor.Monitor();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }

        private static void Monitor_OnSmartPlugPoweredOffAndOn(object sender, Api.Events.SmartPlugPoweredOffAndOnEventArgs e)
        {
        }

        private static void Monitor_OnLogOperationFailed(object sender, Api.Events.LogHandlerOperationFailedEventArgs e)
        {
            try
            {
                var msg = $"Log handler failed. Operation: {e.Operation}. Error: {e?.Message}";
                Console.WriteLine(msg);
                if (previousLogMessageOverwrite)
                    Console.Write("\n");

                previousLogMessageOverwrite = false;
                previousLogMessage = msg;
            }
            catch { }
        }

        private static void Monitor_OnLogEntriesAdded(object sender, Api.Events.LogEntriesAddedEventArgs e)
        {
            try
            {
                foreach (var item in e.Entries)
                {
                    var msg = $"{item.CreatedOn.ToShortDateString()} {item.CreatedOn.ToShortTimeString()}: {item.Message}";

                    if (e.OutputOnNewLine)
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
                }
                previousLogMessageOverwrite = !e.OutputOnNewLine;

            }
            catch { }
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
                                Settings.IpAddress = args[_argno];
                    }
                    else if (args[_argno].Equals("-p", StringComparison.InvariantCultureIgnoreCase))
                    {
                        _argno++;
                        if (_argno < args.Length)
                            if (!args[_argno].StartsWith("-"))
                            {
                                int _pVal = 0;
                                int.TryParse(args[_argno], out _pVal);
                                Settings.PowerConsumptionThreshold = _pVal;
                            }
                    }
                    else if (args[_argno].Equals("-w", StringComparison.InvariantCultureIgnoreCase))
                    {
                        _argno++;
                        if (_argno < args.Length)
                            if (!args[_argno].StartsWith("-"))
                            {
                                int _wVal = 0;
                                int.TryParse(args[_argno], out _wVal);
                                Settings.SecondsToWaitAfterPowerDecline = _wVal;
                            }
                    }
                    else if (args[_argno].Equals("-o", StringComparison.InvariantCultureIgnoreCase))
                    {
                        _argno++;
                        if (_argno < args.Length)
                            if (!args[_argno].StartsWith("-"))
                            {
                                int _oVal = 0;
                                int.TryParse(args[_argno], out _oVal);
                                Settings.SecondsToWaitBeforePoweringBackOn = _oVal;
                            }
                    }
                    else if (args[_argno].Equals("-t", StringComparison.InvariantCultureIgnoreCase))
                    {
                        _argno++;
                        if (_argno < args.Length)
                            if (!args[_argno].StartsWith("-"))
                            {
                                int _plugtype = 0;
                                int.TryParse(args[_argno], out _plugtype);
                                Settings.PlugType = (RpmSmartPlugs)_plugtype;
                            }
                    }
                    else if (args[_argno].Equals("-l", StringComparison.InvariantCultureIgnoreCase))
                    {
                        _argno++;
                        if (_argno < args.Length)
                        {
                            int _logginglevel = 0;
                            int.TryParse(args[_argno], out _logginglevel);
                            if (_logginglevel == -1)
                                Settings.DoNotSaveLog = true;
                            else
                                Settings.LoggingLevel = (RpmLoggingLevel)_logginglevel;
                        }
                    }
                    else if (args[_argno].Equals("-tb-key", StringComparison.InvariantCultureIgnoreCase))
                    {
                        _argno++;
                        if (_argno < args.Length)
                            if (!args[_argno].StartsWith("-"))
                                Settings.TextbeltApiKey = args[_argno];
                    }
                    else if (args[_argno].Equals("-tb-num", StringComparison.InvariantCultureIgnoreCase))
                    {
                        _argno++;
                        if (_argno < args.Length)
                            if (!args[_argno].StartsWith("-"))
                            {
                                if (!args[_argno].StartsWith("+", StringComparison.InvariantCultureIgnoreCase))
                                    throw new Exception("-tb-num invalid value. Use -h for help.");

                                Settings.MobileNumber = args[_argno];
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
            Console.WriteLine("RIG POWER CONSUMPTION MONITOR v1.2 - 2018-02-18");
            Console.WriteLine("-----------------------------------------------");
            Console.WriteLine("");
            Console.WriteLine("By Dennis Moesby. https://github.com/dennismoesby/RigPowerMonitor");
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
            Console.WriteLine("-l   File logging level. 0 = Log everything. 1 = Log warnings and errors. 2 = Log only errors. -1 = Turn off logging. Default: 1.");
            Console.WriteLine("-tb-key   textbelt.com API key. To get a text message when plug is powered off and back on. Visit www.textbelt.com to generate key and fund account. Requires -tb-num option also.");
            Console.WriteLine("-tb-num   Mobile phone number to send text message to when plug is powered off and back on. Use international format including +<country code>, i.e. +4512345678 for Denmark or +1123456789 for USA. Requires -tb-key option also.");
            Console.WriteLine("-tp  Display a list of available TP-Link devices with their corresponding IP address.");
            Console.WriteLine("-h   Display this help.");
            Console.WriteLine("");
            Console.WriteLine("While Rig Power Monitor is running, press 'q' to quit.");
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
                        Console.WriteLine($"{dev_info.Key.ToString().PadRight(15, ' ')} {dev_info.Value.Alias}");

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
