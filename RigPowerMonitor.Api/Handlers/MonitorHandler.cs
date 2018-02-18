using RigPowerMonitor.Api.Events;
using RigPowerMonitor.Api.Exceptions;
using RigPowerMonitor.Api.ObjectModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RigPowerMonitor.Api.Handlers
{
    public class MonitorHandler : IDisposable
    {
        public delegate void LogEntriesAddedEventHandler(object sender, LogEntriesAddedEventArgs e);
        public event LogEntriesAddedEventHandler OnLogEntriesAdded;
        public delegate void LogHandlerOperationFailedEventHandler(object sender, LogHandlerOperationFailedEventArgs e);
        public event LogHandlerOperationFailedEventHandler OnLogOperationFailed;
        public delegate void SmartPlugPoweredOffAndOnEventHandler(object sender, SmartPlugPoweredOffAndOnEventArgs e);
        public event SmartPlugPoweredOffAndOnEventHandler OnSmartPlugPoweredOffAndOn;

        public LogHandler Log { get; private set; }
        public SmartPlugHandler SmartPlug { get; private set; }
        public RpmMonitorSettings Settings { get; private set; }

        private bool powerHasDroppedBelowThreshold;
        private Stopwatch timeSincePowerDroppedBelowThreshold;
        private string PlugFriendlyName;
        private bool quit;

        public MonitorHandler(RpmMonitorSettings settings)
        {
            try
            {
                if (settings == null) throw new ArgumentNullException("settings", "settings must not be NULL when instatiating MonitorHandler.");

                Settings = settings;

                var logFileName = $"rpmlog_{Settings.IpAddress}_{DateTime.Now.ToString("yyyyMMddHHmmss")}";
                Log = new LogHandler(new RpmLogSettings
                {
                    DoNotSaveLog = Settings.DoNotSaveLog,
                    LogFileName = logFileName,
                    LogFilePath = Settings.LogFilePath,
                    LoggingLevel = Settings.LoggingLevel,
                    SmartPlugIpAddress = Settings.IpAddress,
                });
                Log.OnLogEntriesAdded += Log_OnLogEntriesAdded;
                Log.OnLogOperationFailed += Log_OnLogOperationFailed;

            }
            catch (RpmSmartPlugCommunicationException ex)
            {
                Log?.AddToLog($"ERROR: Plug communication failed (IP:{ex.IpAddress}, type: {ex.PlugType}): {ex.Message}", true, RpmLogEntryItemType.ERROR, ex);
                Log?.Dispose();

                throw new RpmApiException("Failed to instantiate MonitorHandler", ".ctor", RpmExceptionType.Exception, ex);
            }
            catch (Exception ex)
            {
                throw new RpmApiException("Failed to instantiate MonitorHandler", ".ctor", RpmExceptionType.Exception, ex);
            }
        }

        public void Monitor()
        {
            try
            {
                SmartPlug = new SmartPlugHandler(Settings.PlugType, Settings.IpAddress);
                PlugFriendlyName = SmartPlug.Name;

                Log.Settings.SmartPlugName = SmartPlug.Name;

                Log.AddToLog($"Connected to: {PlugFriendlyName ?? "Smart plug" } ({Settings.IpAddress}).");
                Log.AddToLog($"Plug type: {(Settings.PlugType == RpmSmartPlugs.WeMoInsightSwitch ? "Belkin WeMo Insight Switch" : "TP-Link HS110")}");
                Log.AddToLog($"Min. power consumption threshold: {Settings.PowerConsumptionThreshold} W.");
                Log.AddToLog($"Wait time before power off: {Settings.SecondsToWaitAfterPowerDecline} seconds.");
                Log.AddToLog($"Wait time before power back on: {Settings.SecondsToWaitBeforePoweringBackOn} seconds.");

                Log.AddToLog("Monitoring started.");
                timeSincePowerDroppedBelowThreshold = new Stopwatch();

                do
                {
                    var plugState = SmartPlug.GetState();

                    if (plugState == RpmSmartPlugState.unknown)
                        Log.AddToLog("Could not determine the state of the smart plug.", true, RpmLogEntryItemType.ERROR);
                    else
                    {
                        if (plugState == RpmSmartPlugState.off)
                        {
                            Log.AddToLog($"{PlugFriendlyName ?? "Smart plug"} is OFF.", true, RpmLogEntryItemType.WARNING);
                            break;
                        }
                        else
                        {
                            var plugPower = SmartPlug.GetCurrentPowerConsumption();

                            var msg = $"Power: {Math.Round(plugPower, 0, MidpointRounding.AwayFromZero)} W. ";

                            if (powerHasDroppedBelowThreshold)
                            {
                                // Power consumption has previously dropped below the threshold and we're waiting for it to either go back up or for the timer to run out before powering off.

                                if (plugPower < Settings.PowerConsumptionThreshold && timeSincePowerDroppedBelowThreshold.ElapsedMilliseconds > (Settings.SecondsToWaitAfterPowerDecline * 1000))
                                {
                                    Log.AddToLog("Monitoring paused.");
                                    powerOffAndOn(plugPower);
                                    if (quit) break;
                                    Log.AddToLog("Monitoring resumed.");

                                    timeSincePowerDroppedBelowThreshold.Stop();
                                    timeSincePowerDroppedBelowThreshold.Reset();
                                    powerHasDroppedBelowThreshold = false;
                                }
                                else if (plugPower < Settings.PowerConsumptionThreshold)
                                {
                                    var timeToWait = new TimeSpan(0, 0, Settings.SecondsToWaitAfterPowerDecline).Subtract(timeSincePowerDroppedBelowThreshold.Elapsed);

                                    msg += $"Power consumption below threshold. Powering off in {timeToWait.Minutes}:{timeToWait.Seconds.ToString().PadLeft(2, '0')}.";
                                    Log.AddToLog(msg, false);
                                }
                                else
                                {
                                    powerHasDroppedBelowThreshold = false;
                                    timeSincePowerDroppedBelowThreshold.Stop();
                                    timeSincePowerDroppedBelowThreshold.Reset();

                                    msg += $"Power consumption is back up over threshold. Powering off cancelled.";
                                    Log.AddToLog(msg);
                                }
                            }
                            else
                            {
                                // Power consumption has not dropped below threshold. Normal monitoring...

                                if (plugPower < Settings.PowerConsumptionThreshold)
                                {
                                    // power just dropped below threshold. Starting count down

                                    var timeToWait = new TimeSpan(0, 0, Settings.SecondsToWaitAfterPowerDecline);
                                    powerHasDroppedBelowThreshold = true;
                                    timeSincePowerDroppedBelowThreshold.Start();

                                    msg += $"Power consumption below threshold. Powering off in {timeToWait.Minutes}:{timeToWait.Seconds.ToString().PadLeft(2, '0')}.";
                                    Log.AddToLog(msg, false);
                                }
                                else
                                {
                                    Log.AddToLog(msg, false);
                                }
                            }
                        }
                    }

                    for (int i = 0; i < 4; i++)
                    {
                        Thread.Sleep(250);

                        if (Console.KeyAvailable)
                        {
                            var keyInfo = Console.ReadKey(true);
                            if (keyInfo.KeyChar == 'q')
                            {
                                quit = true;
                                break;
                            }
                        }
                    }
                } while (!quit);
            }
            catch (RpmSmartPlugCommunicationException ex)
            {
                Log.AddToLog($"ERROR: Plug communication failed (IP:{ex.IpAddress}, type: {ex.PlugType}): {ex.Message}", true, RpmLogEntryItemType.ERROR, ex);
                throw new RpmApiException("Monitoring failed in MonitorHandler", "Monitor", RpmExceptionType.Exception, ex);
            }
            catch (RpmApiException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.AddToLog($"ERROR: {ex.Message}",true, RpmLogEntryItemType.ERROR, ex);
            }

            Log.AddToLog("Monitoring stopped.");
        }

        private void powerOffAndOn(double Power)
        {
            try
            {
                var eventArgs = new SmartPlugPoweredOffAndOnEventArgs(Settings.PlugType, Settings.IpAddress, PlugFriendlyName, Power, DateTime.Now, DateTime.Now);
                
                Log.AddToLog("Powering off.");

                var result = SmartPlug.SetPlugState(RpmSmartPlugState.off);

                Log.AddToLog($"{PlugFriendlyName} succesfully switched off.");

                var sw = new Stopwatch();
                sw.Start();
                var timeToWait = new TimeSpan(0, 0, Settings.SecondsToWaitBeforePoweringBackOn);

                while (timeToWait.TotalMilliseconds > 0 && !quit)
                {
                    Log.AddToLog($"Powering back on in {timeToWait.Minutes}:{timeToWait.Seconds.ToString().PadLeft(2, '0')}.", false);

                    for (int i = 0; i < 4; i++)
                    {
                        Thread.Sleep(250);

                        if (Console.KeyAvailable)
                        {
                            var keyInfo = Console.ReadKey(true);
                            if (keyInfo.KeyChar == 'q')
                            {
                                quit = true;
                                break;
                            }
                        }
                    }
                    if (quit) return;

                    timeToWait = new TimeSpan(0, 0, Settings.SecondsToWaitBeforePoweringBackOn).Subtract(sw.Elapsed);
                }
                sw.Stop();
                sw.Reset();

                Log.AddToLog("Powering on.");
                result = SmartPlug.SetPlugState(RpmSmartPlugState.on);
                Log.AddToLog($"{PlugFriendlyName} succesfully switched on.");
                eventArgs.PoweredOn = DateTime.Now;

                sw.Start();
                timeToWait = new TimeSpan(0, 0, Settings.SecondsToWaitAfterPowerDecline);
                while (timeToWait.TotalMilliseconds > 0 && !quit)
                {
                    Log.AddToLog($"Resuming monitoring in {timeToWait.Minutes}:{timeToWait.Seconds.ToString().PadLeft(2, '0')}.", false);

                    for (int i = 0; i < 4; i++)
                    {
                        Thread.Sleep(250);

                        if (Console.KeyAvailable)
                        {
                            var keyInfo = Console.ReadKey(true);
                            if (keyInfo.KeyChar == 'q')
                            {
                                quit = true;
                                break;
                            }
                        }
                    }
                    if (quit) return;

                    timeToWait = new TimeSpan(0, 0, Settings.SecondsToWaitAfterPowerDecline).Subtract(sw.Elapsed);
                }

                OnSmartPlugPoweredOffAndOn?.Invoke(this, eventArgs);

                Log.AddToLog($"The plug was powered off at {eventArgs.PoweredOff.ToShortTimeString()} due to power consumption being lower than {Settings.PowerConsumptionThreshold.ToString()} W for {Settings.SecondsToWaitAfterPowerDecline} seconds. Power consumption at the time of powering off was {Power.ToString()} W. The plug was powered back on at {eventArgs.PoweredOn.ToShortTimeString()}", true, RpmLogEntryItemType.WARNING);

                if(Settings.TextNotificationEnabled)
                {
                    try
                    {
                        var tbHandler = new TextbeltHandler(Settings.TextbeltApiKey, Settings.MobileNumber);
                        var tbQuotaRemaining = tbHandler.SendText($"{PlugFriendlyName} was powered off at {eventArgs.PoweredOff.ToShortTimeString()} due to power consumption being lower than {Settings.PowerConsumptionThreshold.ToString()} W for {Settings.SecondsToWaitAfterPowerDecline} seconds. Power consumption at the time of powering off was {Power.ToString()} W. The plug was powered back on at {eventArgs.PoweredOn.ToShortTimeString()}.");

                        Log.AddToLog($"Text notification sent via Textbelt.com. Quota remaining : {tbQuotaRemaining}");
                    }
                    catch(RpmApiException ex)
                    {
                        Log.AddToLog(ex);
                    }
                }
            }
            catch (RpmSmartPlugCommunicationException ex)
            {
                Log.AddToLog($"ERROR: Plug communication failed (IP:{ex.IpAddress}, type: {ex.PlugType}): {ex.Message}", true, RpmLogEntryItemType.ERROR, ex);
                throw new RpmApiException("Powering off and on failed in MonitorHandler", ".powerOffAndOn", RpmExceptionType.Exception, ex);
            }
            catch (RpmApiException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw new RpmApiException("Powering off and on failed in MonitorHandler", "powerOffAndOn", RpmExceptionType.Exception, ex);
            }

        }




        public void Dispose()
        {
            Log?.Dispose();
        }

        #region Private methods
        private void Log_OnLogOperationFailed(object sender, LogHandlerOperationFailedEventArgs e)
        {
            OnLogOperationFailed?.Invoke(sender, e);
        }

        private void Log_OnLogEntriesAdded(object sender, LogEntriesAddedEventArgs e)
        {
            OnLogEntriesAdded?.Invoke(sender, e);
        }

        #endregion
    }
}
