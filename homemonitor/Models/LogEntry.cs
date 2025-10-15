using System;
using Avalonia.Logging;

namespace homemonitor.Models;

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Message { get; set; } = string.Empty;
    public LogLevel Level { get; set; }
    public bool IsMotionEvent { get; set; }
    public string? SensorId { get; set; }
    public string? Location { get; set; }
    public string FormattedTime => Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
    public string FullTimestamp => Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");

    public string Icon => Level switch
    {
        LogLevel.Error => "Error",
        LogLevel.Warning => "Warning",
        LogLevel.Motion => "Movement Detected",
        LogLevel.Success => "Success",
        _ => "idle"
    };
    
    public string BackgroundColor => IsMotionEvent ? "#FFEBEE" : "Transparent";

    public string TextColor => Level switch
    {
        LogLevel.Error => "#D32F2F",
        LogLevel.Warning => "#F57C00",
        LogLevel.Motion => "#C62828",
        LogLevel.Success => "#388E3C",
        _ => "#424242"
    };
}

public enum LogLevel
{
    Info,
    Warning,
    Error,
    Motion,
    Success
}