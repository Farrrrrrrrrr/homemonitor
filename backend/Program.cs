using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;
using System.Diagnostics.Tracing;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<MotionDb>(opt => opt.UseSqlite("Data Source=motion.db"));
builder.Services.AddSingleton<WebSocketManager>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MotionDb>();
    db.Database.EnsureCreated();
}

app.UseCors();

// Configure static files and default files in the correct order
app.UseDefaultFiles(new DefaultFilesOptions
{
    DefaultFileNames = { "index.html" }
});
app.UseStaticFiles(new StaticFileOptions
{
    ServeUnknownFileTypes = true,
    DefaultContentType = "application/octet-stream"
});

app.UseWebSockets();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
}

var wsManager = app.Services.GetRequiredService<WebSocketManager>();
app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var ws = await context.WebSockets.AcceptWebSocketAsync();
        var socketId = Guid.NewGuid().ToString();
        wsManager.AddSocket(socketId, ws);
        Console.WriteLine($"Klien Websocket: {socketId} Terhubung");
        await HandleWebSocketConnection(ws, socketId, wsManager);
        
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

app.MapPost("/api/motion-events", async (MotionEventDto dto, MotionDb db) =>
{
    var motionEvent = new MotionEvent
    {
        SensorId = dto.SensorId,
        DetectedAt = dto.DetectedAt.HasValue ? dto.DetectedAt.Value : DateTime.UtcNow,
        EventType = dto.EventType,
        Location = dto.Location ?? "unknown"
    };
    
    db.MotionEvents.Add(motionEvent);
    await db.SaveChangesAsync();
    Console.WriteLine($"Terjadi Pergerakan {dto.EventType} dari {dto.SensorId}");

    await wsManager.BroadcastMessage(new
    {
        type = "motion_alert",
        data = new
        {
            id = motionEvent.Id,
            sensorId = motionEvent.SensorId,
            eventType = motionEvent.EventType,
            detectedAt = motionEvent.DetectedAt,
            location = motionEvent.Location
        }
    });
    return Results.Created($"/api/motion-events/{motionEvent.Id}", motionEvent);
    
});

app.MapGet("/api/motion-events", async (MotionDb db, int? limit) =>
{
    var query = db.MotionEvents.OrderByDescending(e => e.DetectedAt);
    var events = limit.HasValue
        ? await query.Take(limit.Value).ToListAsync()
        : await query.ToListAsync();
    return Results.Ok(events);
});

app.MapGet("/api/motion-events/{id}", async (int id, MotionDb db) =>
{
    var evt = await db.MotionEvents.FindAsync(id);
    return evt is not null ? Results.Ok(evt) : Results.NotFound();
});

app.MapGet("/api/sensors", async (MotionDb db) =>
{
    var sensors = await db.MotionEvents
        .GroupBy(e => e.SensorId)
        .Select(g => new
        {
            SensorId = g.Key,
            LastSeen = g.Max(e => e.DetectedAt),
            EventCount =  g.Count()
        })
        .ToListAsync();
    return Results.Ok(sensors);
});

app.MapGet("/api/dashboard/stats", async (MotionDb db) =>
{
    var now = DateTime.UtcNow;
    var stats = new
    {
        TotalEvents = await db.MotionEvents.CountAsync(),
        EventsLast24h = await db.MotionEvents
            .Where(e => e.DetectedAt >= now.AddHours(-24))
            .CountAsync(),
        EventsLastHour = await db.MotionEvents
            .Where(e => e.DetectedAt >= now.AddHours(-1))
            .CountAsync(),
        ActiveSensors = await db.MotionEvents
            .GroupBy(e => e.SensorId)
            .CountAsync(),
        ConnectedClients = wsManager.GetConnectionCount()

    };
    return Results.Ok(stats);
});

app.MapDelete("/api/motion-events", async (MotionDb db) =>
{
    db.MotionEvents.RemoveRange(db.MotionEvents);
    await db.SaveChangesAsync();
    return Results.Ok(new { message = "Semua event dihapus" });
});

// Additional HTML routes
app.MapGet("/", () => Results.File("wwwroot/index.html", "text/html"));
app.MapGet("/dashboard", () => Results.Redirect("/"));
app.MapGet("/status", () => Results.File("wwwroot/status.html", "text/html"));
app.MapGet("/api", () => Results.Content(@"
<!DOCTYPE html>
<html>
<head>
    <title>API Documentation - IoT Motion Detection</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 40px; background: #f5f5f5; }
        .container { max-width: 800px; margin: 0 auto; background: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
        h1 { color: #1976D2; }
        .endpoint { background: #f8f9fa; padding: 15px; margin: 10px 0; border-radius: 5px; border-left: 4px solid #1976D2; }
        .method { display: inline-block; padding: 4px 8px; border-radius: 3px; color: white; font-weight: bold; margin-right: 10px; }
        .get { background: #1976D2; }
        .post { background: #4CAF50; }
        .delete { background: #f44336; }
        pre { background: #263238; color: #fff; padding: 15px; border-radius: 5px; overflow-x: auto; }
        a { color: #1976D2; text-decoration: none; }
        a:hover { text-decoration: underline; }
    </style>
</head>
<body>
    <div class='container'>
        <h1>🏠 IoT Motion Detection API</h1>
        <p><a href='/'>&larr; Back to Dashboard</a></p>
        
        <div class='endpoint'>
            <span class='method post'>POST</span><strong>/api/motion-events</strong>
            <p>Submit a new motion detection event from IoT devices</p>
            <pre>{
  ""sensorId"": ""ESP32_PIR_01"",
  ""eventType"": ""motion_detected"",
  ""location"": ""Living Room""
}</pre>
        </div>
        
        <div class='endpoint'>
            <span class='method get'>GET</span><strong>/api/motion-events</strong>
            <p>Retrieve motion event history (optional ?limit=10 parameter)</p>
        </div>
        
        <div class='endpoint'>
            <span class='method get'>GET</span><strong>/api/dashboard/stats</strong>
            <p>Get dashboard statistics and metrics</p>
        </div>
        
        <div class='endpoint'>
            <span class='method delete'>DELETE</span><strong>/api/motion-events</strong>
            <p>Clear all motion events from the database</p>
        </div>
        
        <div class='endpoint'>
            <strong>WebSocket: /ws</strong>
            <p>Real-time motion alerts and system updates</p>
        </div>
        
        <h2>Example ESP32 Arduino Code</h2>
        <pre>#include &lt;WiFi.h&gt;
#include &lt;HTTPClient.h&gt;
#include &lt;ArduinoJson.h&gt;

const char* serverURL = ""http://YOUR_SERVER_IP:5000/api/motion-events"";

void sendMotionEvent() {
    HTTPClient http;
    http.begin(serverURL);
    http.addHeader(""Content-Type"", ""application/json"");
    
    DynamicJsonDocument doc(200);
    doc[""sensorId""] = ""ESP32_PIR_01"";
    doc[""eventType""] = ""motion_detected"";
    doc[""location""] = ""Living Room"";
    
    String payload;
    serializeJson(doc, payload);
    
    int httpResponseCode = http.POST(payload);
    http.end();
}</pre>
    </div>
</body>
</html>", "text/html"));

Console.WriteLine("🏠 IoT Motion Detection System Started Successfully!");
Console.WriteLine("==========================================");
Console.WriteLine("📊 Web Dashboard: http://localhost:5000/");
Console.WriteLine("� System Status: http://localhost:5000/status");
Console.WriteLine("�📚 API Documentation: http://localhost:5000/api");
Console.WriteLine("🔌 WebSocket Endpoint: ws://localhost:5000/ws");
Console.WriteLine("🚀 Swagger UI: http://localhost:5000/swagger");
Console.WriteLine("==========================================");
Console.WriteLine("API Endpoints:");
Console.WriteLine("  POST /api/motion-events - Submit motion events");
Console.WriteLine("  GET  /api/motion-events - Get motion history");
Console.WriteLine("  GET  /api/dashboard/stats - Get statistics");
Console.WriteLine("==========================================");
app.Run();

async Task HandleWebSocketConnection(WebSocket ws, string socketId, WebSocketManager manager)
{
    var buffer = new byte[1024 * 4];
    try
    {
        while (ws.State == WebSocketState.Open)
        {
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                break;
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{ex.Message}");
    }
    finally
    {
        manager.RemoveSocket(socketId);
        Console.WriteLine($"{socketId}");
    }
}

class MotionEvent
{
    public int Id { get; set; }
    public string SensorId { get; set; } = "";
    public DateTime DetectedAt { get; set; }
    public string EventType { get; set; } = "terdeteksi";
    public string Location { get; set; } = "unknown";
}


class MotionEventDto
{
    public string SensorId { get; set; } = "";
    public DateTime? DetectedAt { get; set; } // ← ADD THE ? HERE
    public string EventType { get; set; } = "detected";
    public string? Location { get; set; }
}

class MotionDb : DbContext
{
    public MotionDb(DbContextOptions<MotionDb> options) : base(options) { }
    public DbSet<MotionEvent> MotionEvents => Set<MotionEvent>();
    
}

class WebSocketManager
{
    private readonly ConcurrentDictionary<string, WebSocket> _sockets = new();

    public void AddSocket(string id, WebSocket socket)
    {
        _sockets.TryAdd(id, socket);
    }

    public void RemoveSocket(string id)
    {
        _sockets.TryRemove(id, out _);
        
    }
    
    public int GetConnectionCount() => _sockets.Count;

    public async Task BroadcastMessage(object message)
    {
        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);
        var tasks = _sockets.Values
            .Where(s => s.State == WebSocketState.Open)
            .Select(s =>
                s.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None));
        await Task.WhenAll(tasks);
    }
}