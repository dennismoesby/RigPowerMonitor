using Communications.Responses;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RigPowerMonitor
{
    class Program
    {
        static string IpAddress;
        static int PowerConsumptionThreshold;
        static int MinutesToWaitAfterPowerDecline;
        static int SecondsToWaitBeforePoweringBackOn;

        static bool powerHasDroppedBelowThreshold;
        static Stopwatch timeSincePowerDroppedBelowThreshold;
        static string WemoFriendlyName;
        static string ApiAddress;
        static string previousLogMessage;
        static bool previousLogMessageOverwrite;

        static void Main(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                Console.WriteLine("ERROR: Required options not set. Use -h for help.");
            }
            else if (args.Any(x => x.Equals("-h", StringComparison.InvariantCultureIgnoreCase)))
            {
                showHelp();
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
                    if (MinutesToWaitAfterPowerDecline == 0) MinutesToWaitAfterPowerDecline = 5;
                    if (SecondsToWaitBeforePoweringBackOn == 0) SecondsToWaitBeforePoweringBackOn = 30;
                    ApiAddress = "http://" + IpAddress;

                    try
                    {
                        getWemoName();
                        Console.WriteLine($"Connected to: {WemoFriendlyName} ({IpAddress}).");
                        Console.WriteLine($"Min. power consumption threshold: {PowerConsumptionThreshold} W.");
                        Console.WriteLine($"Wait time before power off: {MinutesToWaitAfterPowerDecline} minutes.");
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

        private static void getWemoName()
        {
            try
            {
                var v = new WemoNet.Wemo();
                var result = v.GetWemoResponseObjectAsync<GetFriendlyNameResponse>(Communications.Utilities.Soap.WemoGetCommands.GetFriendlyName, ApiAddress).GetAwaiter().GetResult();
                WemoFriendlyName = result.FriendlyName;
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
                    var v = new WemoNet.Wemo();
                    var insight = v.GetInsightParams(ApiAddress).GetAwaiter().GetResult();
                    if (insight == null)
                        logMessage("No data retrieved from WeMo Insight Switch");
                    else
                    {
                        if (insight.State == 0)
                        {
                            logMessage($"{WemoFriendlyName} is OFF.");
                            break;
                        }
                        else
                        {
                            var msg = $"Power: {Math.Round(insight.CurrentPowerConsumption, 0, MidpointRounding.AwayFromZero)} W. ";

                            if (powerHasDroppedBelowThreshold)
                            {
                                // Power consumption has previously dropped below the threshold and we're waiting for it to either go back up or for the timer to run out before powering off.

                                if (insight.CurrentPowerConsumption < PowerConsumptionThreshold && timeSincePowerDroppedBelowThreshold.ElapsedMilliseconds > (MinutesToWaitAfterPowerDecline * 60 * 1000))
                                {
                                    logMessage("Monitoring paused.");
                                    PowerOffAndOn();
                                    logMessage("Monitoring resumed.");

                                    timeSincePowerDroppedBelowThreshold.Stop();
                                    timeSincePowerDroppedBelowThreshold.Reset();
                                    powerHasDroppedBelowThreshold = false;
                                }
                                else if (insight.CurrentPowerConsumption < PowerConsumptionThreshold)
                                {
                                    var timeToWait = new TimeSpan(0, MinutesToWaitAfterPowerDecline, 0).Subtract(timeSincePowerDroppedBelowThreshold.Elapsed);

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

                                if (insight.CurrentPowerConsumption < PowerConsumptionThreshold)
                                {
                                    // power just dropped below threshold. Starting count down

                                    var timeToWait = new TimeSpan(0, MinutesToWaitAfterPowerDecline, 0);
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

                var v = new WemoNet.Wemo();
                var result = v.TurnOffWemoPlugAsync(ApiAddress).GetAwaiter().GetResult();

                logMessage($"{WemoFriendlyName} succesfully switched off.");

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
                result = v.TurnOnWemoPlugAsync(ApiAddress).GetAwaiter().GetResult();
                logMessage($"{WemoFriendlyName} succesfully switched on.");

                sw.Start();
                timeToWait = new TimeSpan(0, MinutesToWaitAfterPowerDecline, 0);
                while (timeToWait.TotalMilliseconds > 0)
                {
                    logMessage($"Resuming monitoring in {timeToWait.Minutes}:{timeToWait.Seconds.ToString().PadLeft(2, '0')}.", true);
                    Thread.Sleep(1000);
                    timeToWait = new TimeSpan(0, MinutesToWaitAfterPowerDecline, 0).Subtract(sw.Elapsed);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Exception during Power Off and On. Message: {ex.Message}");
            }
        }

        private static void logMessage(string messsage, bool overwrite = false)
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


        private static void parseArgs(string[] args)
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
                else if (args[_argno].Equals("-m", StringComparison.InvariantCultureIgnoreCase))
                {
                    _argno++;
                    if (_argno < args.Length)
                        if (!args[_argno].StartsWith("-"))
                            int.TryParse(args[_argno], out MinutesToWaitAfterPowerDecline);
                }
                else if (args[_argno].Equals("-o", StringComparison.InvariantCultureIgnoreCase))
                {
                    _argno++;
                    if (_argno < args.Length)
                        if (!args[_argno].StartsWith("-"))
                            int.TryParse(args[_argno], out SecondsToWaitBeforePoweringBackOn);
                }

                _argno++;
            }
        }

        private static void showHeader()
        {
            Console.WriteLine("----------------------------------");
            Console.WriteLine("RIG POWER CONSUMPTION MONITOR v1.0");
            Console.WriteLine("----------------------------------");
            Console.WriteLine("");
            Console.WriteLine("By Dennis Moesby. Based in part on Wemo.net API by seanksullivan (https://github.com/seanksullivan/Wemo.net)");
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
            Console.WriteLine("Monitors the power consumption of a Belkin WeMo Insight Switch and turns it off and back on again");
            Console.WriteLine("if the power consumption drops below a specified threshold. useful to force a mining rig to reboot if it hangs.");
            Console.WriteLine("Remember to set bios on the miner to automatically boot after a power failure.");
            Console.WriteLine("");
            Console.WriteLine("Usage: RigPowerMonitor.exe [-OPTIONS]");
            Console.WriteLine("");
            Console.WriteLine("Options:");
            Console.WriteLine("");
            Console.WriteLine("-a   Ip address of the Wemo Insight Switch to monitor. Required.");
            Console.WriteLine("-p   Power Consumption Threshold. The lowest allowed power consumption of the mining rig before powering off and back on. Required.");
            Console.WriteLine("-m   Minutes to wait for the power consumption to go back up over the threshold again before powering off. Default: 5.");
            Console.WriteLine("-o   Seconds to wait before powering back on after the power has been cut. Default: 30.");
            Console.WriteLine("-h   Display this help.");
            Console.WriteLine("");

            showDonationInfo();
        }
    }
}
