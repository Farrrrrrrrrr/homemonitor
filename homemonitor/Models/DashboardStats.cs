using System;
using System.Text.Json.Serialization;

namespace homemonitor.Models;

public class DashboardStats
{
    [JsonPropertyName("totalEvents")]
    public int TotalEvents { get; set; }

    [JsonPropertyName("eventsLast24h")]
    public int EventsLast24h { get; set; }

    [JsonPropertyName("eventsLastHour")]
    public int EventsLastHour { get; set; }

    [JsonPropertyName("activeSensors")]
    public int ActiveSensors { get; set; }

    [JsonPropertyName("connectedClients")]
    public int ConnectedClients { get; set; }
}