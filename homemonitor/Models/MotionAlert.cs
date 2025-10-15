using System;
using System.Text.Json.Serialization;

namespace homemonitor.Models;

public class MotionAlertMessage
{
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("data")]
    public MotionAlertData? Data { get; set; }
}

public class MotionAlertData
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    
    [JsonPropertyName("sensorId")] public string SensorId { get; set; } = "";
    
    [JsonPropertyName("eventType")] public string EventType { get; set; } = "";
    
    [JsonPropertyName("detectedAt")] public DateTime DetectedAt { get; set; }
    
    [JsonPropertyName("location")] public string Location { get; set; } = "";
}