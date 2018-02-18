using RigPowerMonitor.Api.Events;
using RigPowerMonitor.Api.Exceptions;
using RigPowerMonitor.Api.ObjectModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RigPowerMonitor.Api.Handlers
{
    public class LogHandler : IDisposable
    {
        public delegate void LogEntriesAddedEventHandler(object sender, LogEntriesAddedEventArgs e);
        public event LogEntriesAddedEventHandler OnLogEntriesAdded;
        public delegate void LogHandlerOperationFailedEventHandler(object sender, LogHandlerOperationFailedEventArgs e);
        public event LogHandlerOperationFailedEventHandler OnLogOperationFailed;

        private List<RpmLogEntryItem> _items { get; set; }

        private bool logFileCreated { get; set; }
        private int totalRowsWrittenToFile { get; set; }
        private int logFileCount { get; set; }
        public RpmLogSettings Settings { get; set; }

        public LogHandler(RpmLogSettings settings)
        {
            try
            {
                _items = new List<RpmLogEntryItem>();

                Settings = settings ?? new RpmLogSettings();

                logFileCount = 1;
            }
            catch (Exception ex)
            {
                OnLogOperationFailed?.Invoke(this, new LogHandlerOperationFailedEventArgs(RpmLogOperation.Instantiate, ex.Message));
            }
        }

        public RpmLogEntryItem[] LogEntries
        {
            get
            {
                try
                {
                    return _items.ToArray();
                }
                catch (Exception ex)
                {
                    OnLogOperationFailed?.Invoke(this, new LogHandlerOperationFailedEventArgs(RpmLogOperation.LogEntries, ex.Message));
                    return null;
                }
            }
        }

        public void AddToLog(string Message, bool OutputOnNewLine = true, RpmLogEntryItemType ItemType = RpmLogEntryItemType.INFO, Exception ex = null)
        {
            try
            {
                AddToLog(new RpmLogEntryItem
                {
                    ItemType = ItemType,
                    Result = ex != null ? ex.HResult : 0,
                    Message = Message,
                    Error = ex
                }, OutputOnNewLine);
            }
            catch (Exception e)
            {
                OnLogOperationFailed?.Invoke(this, new LogHandlerOperationFailedEventArgs(RpmLogOperation.AddToLog, e.Message));
            }
        }

        public void AddToLog(RpmLogEntryItem Entry, bool OutputOnNewLine = true)
        {
            try
            {
                AddToLog(new RpmLogEntryItem[] { Entry }, OutputOnNewLine);
            }
            catch (Exception e)
            {
                OnLogOperationFailed?.Invoke(this, new LogHandlerOperationFailedEventArgs(RpmLogOperation.AddToLog, e.Message));
            }
        }

        public void AddToLog(RpmApiException ex)
        {
            try
            {

                List<RpmLogEntryItem> _items = new List<RpmLogEntryItem>();
                _items.Add(new RpmLogEntryItem
                {
                    ItemType = RpmLogEntryItemType.ERROR,
                    Result = ex.HResult,
                    Message = string.Format("[{0}] {1}: {2}", ex.MethodName, ex.exceptionType.ToString(), ex.Message),
                    Error = ex,
                });

                var innerex = ex.InnerException;
                var i = 0;
                while (innerex != null)
                {
                    i++;

                    _items.Add(new RpmLogEntryItem
                    {
                        ItemType = RpmLogEntryItemType.ERROR,
                        Result = innerex.HResult,
                        Message = string.Format("Inner exception #{0}: {1}", i, innerex.Message),
                        Error = innerex,
                    });

                    innerex = innerex.InnerException;
                }

                AddToLog(_items.ToArray());
            }
            catch (Exception e)
            {
                OnLogOperationFailed?.Invoke(this, new LogHandlerOperationFailedEventArgs(RpmLogOperation.AddToLog, e.Message));
            }
        }

        public void AddToLog(Exception ex)
        {
            try
            {
                List<RpmLogEntryItem> _items = new List<RpmLogEntryItem>();
                _items.Add(new RpmLogEntryItem
                {
                    ItemType = RpmLogEntryItemType.ERROR,
                    Result = ex.HResult,
                    Message = string.Format("An error occurred: {0}", ex.Message),
                    Error = ex,
                });

                var innerex = ex.InnerException;
                var i = 0;
                while (innerex != null)
                {
                    i++;

                    _items.Add(new RpmLogEntryItem
                    {
                        ItemType = RpmLogEntryItemType.ERROR,
                        Result = innerex.HResult,
                        Message = string.Format("Inner exception #{0}: {1}", i, innerex.Message),
                        Error = innerex,
                    });

                    innerex = innerex.InnerException;
                }

                AddToLog(_items.ToArray());
            }
            catch (Exception e)
            {
                OnLogOperationFailed?.Invoke(this, new LogHandlerOperationFailedEventArgs(RpmLogOperation.AddToLog, e.Message));
            }
        }

        public void AddToLog(RpmLogEntryItem[] Entries, bool OutputOnNewLine = true)
        {
            try
            {
                var entriesAdded = new List<RpmLogEntryItem>();

                foreach (var entry in Entries)
                {
                    if (!_items.OrderByDescending(x => x.CreatedOn).Take(5).Any(x => x.Message == entry.Message))
                        entriesAdded.Add(entry);
                }

                if (entriesAdded.Count > 0)
                {
                    _items.AddRange(entriesAdded);
                    OnLogEntriesAdded?.Invoke(this, new LogEntriesAddedEventArgs(entriesAdded.ToArray(), OutputOnNewLine));
                }

                if (_items.Count >= 500)
                    this.SaveLog();
            }
            catch (Exception e)
            {
                OnLogOperationFailed?.Invoke(this, new LogHandlerOperationFailedEventArgs(RpmLogOperation.AddToLog, e.Message));
            }
        }

        public void ClearLog()
        {
            try
            {
                _items.Clear();
            }
            catch (Exception e)
            {
                OnLogOperationFailed?.Invoke(this, new LogHandlerOperationFailedEventArgs(RpmLogOperation.ClearLog, e.Message));
            }
        }

        public void SaveLog()
        {
            try
            {
                if (Settings.DoNotSaveLog)
                    return;

                if (totalRowsWrittenToFile > 100000)
                {
                    logFileCount++;
                    totalRowsWrittenToFile = 0;
                    logFileCreated = false;
                }

                var fullfilename = $"{(!string.IsNullOrWhiteSpace(Settings.LogFilePath) ? Path.Combine(Settings.LogFilePath, Settings.LogFileName) : Settings.LogFileName)}.{logFileCount.ToString().PadLeft(4,'0')}.csv";

                using (var logFile = File.AppendText(fullfilename))
                {
                    if (!logFileCreated)
                    {
                        logFile.WriteLine($"Rig Power Consumption Monitor Log (File #{logFileCount.ToString().PadLeft(4,'0')})");
                        logFile.WriteLine("");
                        logFile.WriteLine($"Smart plug:;{Settings.SmartPlugName}");
                        logFile.WriteLine($"IP:;{Settings.SmartPlugIpAddress}");
                        logFile.WriteLine("");
                        logFile.WriteLine("Created On;Type;Result;Message;Errors");

                        logFileCreated = true;
                    }

                    var itemsToSave = Settings.LoggingLevel == RpmLoggingLevel.Errors ? _items.Where(x => x.ItemType == RpmLogEntryItemType.ERROR)
                                    : Settings.LoggingLevel == RpmLoggingLevel.WarningErrors ? _items.Where(x => x.ItemType != RpmLogEntryItemType.INFO)
                                    : _items;

                    foreach (var item in itemsToSave)
                    {
                        logFile.WriteLine("{0};{1};{2};\"{3}\";\"{4}\""
                            , item.CreatedOn.ToString("yyyy-MM-dd HH:mm:ss.fff")
                            , item.ItemType.ToString()
                            , item.Result
                            , item.Message?.Replace("\"", "'").Replace(";", "|")
                            , parseErrors(item.Error));
                    }

                    logFile.Close();
                    totalRowsWrittenToFile += _items.Count;
                }

                ClearLog();
            }
            catch (Exception e)
            {
                OnLogOperationFailed?.Invoke(this, new LogHandlerOperationFailedEventArgs(RpmLogOperation.SaveLog, e.Message));
            }
        }

        private string parseErrors(Exception ex)
        {
            try
            {
                if (ex == null) return null;

                StringBuilder sb = new StringBuilder();

                sb.AppendFormat("EXCEPTION: {0}. Trace: {1}. ", ex.Message?.Replace("\"", "'").Replace(";", "|"), ex.StackTrace?.Replace("\"", "'").Replace(";", "|"));

                var inner = ex.InnerException;
                var innerc = 0;
                while (inner != null)
                {
                    innerc++;
                    sb.AppendFormat("INNER EXCEPTION #{2}: {0}. Trace: {1}. ", inner.Message?.Replace("\"", "'").Replace(";", "|"), inner.StackTrace?.Replace("\"", "'").Replace(";", "|"), innerc);
                    inner = inner.InnerException;
                }

                return sb.ToString();
            }
            catch (Exception e)
            {
                OnLogOperationFailed?.Invoke(this, new LogHandlerOperationFailedEventArgs(RpmLogOperation.parseErrors, e.Message));
                return null;
            }
        }

        public void Dispose()
        {
            try
            {
                if (_items?.Count > 0)
                    SaveLog();
            }
            catch (Exception e)
            {
                OnLogOperationFailed?.Invoke(this, new LogHandlerOperationFailedEventArgs(RpmLogOperation.Dispose, e.Message));
            }
        }

    }
}
