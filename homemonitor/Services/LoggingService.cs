using System;
using System.Collections.ObjectModel;
using System.Linq;
using homemonitor.Models;

namespace homemonitor.Services;

public class LoggingService
{
    private const int MaxLogEntries = 100;
    private readonly object _lock = new();
    public ObservableCollection<LogEntry> LogEntries { get; } = new();

    public void AddLog(
        string message,
        LogLevel Level = LogLevel.Info,
        bool isMotionEvent = false,
        string? sensorId = null,
        string? location = null)
    {
        lock (_lock)
        {
            var entry = new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Message = message,
                Level = Level,
                IsMotionEvent = isMotionEvent,
                SensorId = sensorId,
                Location = location
            };
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                LogEntries.Add(entry);

                while (LogEntries.Count > MaxLogEntries)
                {
                    LogEntries.RemoveAt(0);
                }
            });
                
        }
    }

    public void ClearLogs()
    {
        lock (_lock)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    LogEntries.Clear();
                }
            );
        }
    }

    public int GetMotionEventCount()
    {
        lock (_lock)
        {
            return LogEntries.Count(e => e.IsMotionEvent);
        }
    }

    public int GetTotalMessageCount()
    {
        lock (_lock)
        {
            return LogEntries.Count;
        }
    }
}