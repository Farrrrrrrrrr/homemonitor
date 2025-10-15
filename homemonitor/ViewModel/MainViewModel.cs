using System;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using homemonitor.Models;
using homemonitor.ViewModel;
using homemonitor.Services;
using ReactiveUI;

namespace homemonitor.ViewModel;

public class MainViewModel : ReactiveObject, IDisposable
{
    private readonly WebSocketService _webSocketService;
    private readonly ApiService _apiService;
    private readonly LoggingService _loggingService;
    private readonly DispatcherTimer _statsTimer;

    private string _webSocketUrl = "ws://localhost:5000/ws";
    private ConnectionStatus _connectionStatus = ConnectionStatus.Disconnected;
    private int _totalMessages;
    private int _motionEvents;
    private bool _isConnected;
    private int _connectedClients;
    private int _activeSensors;
    private int _eventsLast24h;

    public MainViewModel()
    {
        _webSocketService = new WebSocketService();
        _apiService = new ApiService();
        _loggingService = new LoggingService();
        
        _webSocketService.MessageReceived += OnMessageReceived;
        _webSocketService.ConnectionStateChanged += OnConnectionStateChanged;

        // Create simple async commands to avoid threading issues
        ConnectCommand = new AsyncRelayCommand(ConnectAsync);
        DisconnectCommand = new AsyncRelayCommand(DisconnectAsync);
        ClearLogsCommand = new RelayCommand(ClearLogs);
        RefreshStatsCommand = new AsyncRelayCommand(FetchDashboardStatsAsync);

        _statsTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(10)
        };
        _statsTimer.Tick += async (s, e) =>
        {
            if (IsConnected)
            {
                await FetchDashboardStatsAsync();
            }
        };

        // Initialize with proper state
        IsConnected = false;
        ConnectionStatus = ConnectionStatus.Disconnected;

        _loggingService.AddLog("Aplikasi dimulai", LogLevel.Info);
    }
    #region Properties
    
    public string WebSocketUrl
    {
        get => _webSocketUrl;
        set => this.RaiseAndSetIfChanged(ref _webSocketUrl, value);
    }
    public ConnectionStatus ConnectionStatus
    {
        get => _connectionStatus;
        private set
        {
            this.RaiseAndSetIfChanged(ref _connectionStatus, value);
            this.RaisePropertyChanged(nameof(StatusText));
            this.RaisePropertyChanged(nameof(StatusColor));
        }
    }
    public bool IsConnected
    {
        get => _isConnected;
        private set => this.RaiseAndSetIfChanged(ref _isConnected, value);
    }
    
    public int TotalMessages
    {
        get => _totalMessages;
        private set => this.RaiseAndSetIfChanged(ref _totalMessages, value);
    }
    
    public int MotionEvents
    {
        get => _motionEvents;
        private set => this.RaiseAndSetIfChanged(ref _motionEvents, value);
    }
    
    public int ConnectedClients
    {
        get => _connectedClients;
        private set => this.RaiseAndSetIfChanged(ref _connectedClients, value);
    }
    
    public int ActiveSensors
    {
        get => _activeSensors;
        private set => this.RaiseAndSetIfChanged(ref _activeSensors, value);
    }
    
    public int EventsLast24h
    {
        get => _eventsLast24h;
        private set => this.RaiseAndSetIfChanged(ref _eventsLast24h, value);
    }
    
    public ObservableCollection<LogEntry> LogEntries => _loggingService.LogEntries;

    public string StatusText => ConnectionStatus switch
    {
        ConnectionStatus.Connected => " Connected",
        ConnectionStatus.Connecting => " Connecting...",
        ConnectionStatus.Disconnected => " Disconnected",
        ConnectionStatus.Error => " Error",
        _ => " Unknown"
    };
    
    public string StatusColor => ConnectionStatus switch
    {
        ConnectionStatus.Connected => "#4CAF50",    
        ConnectionStatus.Connecting => "#FFC107",   
        ConnectionStatus.Disconnected => "#9E9E9E", 
        ConnectionStatus.Error => "#F44336",        
        _ => "#9E9E9E"
    };

    #endregion

    #region Commands

    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand ClearLogsCommand { get; }
    public ICommand RefreshStatsCommand { get; }

    #endregion

    #region Command Implementations
    
    private async Task ConnectAsync()
    {
        // Add debug logging to see if this method is called
        Console.WriteLine("ConnectAsync called!");
        _loggingService.AddLog("🔗 Connect button clicked", LogLevel.Info);
        
        // Prevent connecting if already connected
        if (IsConnected)
        {
            _loggingService.AddLog("⚠ Already connected!", LogLevel.Warning);
            return;
        }
        
        if (string.IsNullOrWhiteSpace(WebSocketUrl))
        {
            _loggingService.AddLog("⚠ Please enter a valid WebSocket URL", LogLevel.Warning);
            return;
        }

        _loggingService.AddLog($"🔗 Connecting to {WebSocketUrl}...");

        //Extract http baseurl from websocket URL for API call
        var httpUrl = WebSocketUrl.Replace("ws://", "http://").Replace("wss://", "https://");
        try
        {
            var uri = new Uri(httpUrl);
            var baseUrl = $"{uri.Scheme}://{uri.Host}:{uri.Port}";
            _apiService.SetBaseUrl(baseUrl);
            _loggingService.AddLog($" API endpoint set to: {baseUrl}/api");
        }
        catch (Exception ex)
        {
            _loggingService.AddLog($" Invalid URL format: {ex.Message}", LogLevel.Error);
            return;
        }

        await _webSocketService.ConnectAsync(WebSocketUrl);
    }
    
    private async Task DisconnectAsync()
    {
        // Prevent disconnecting if not connected
        if (!IsConnected)
        {
            _loggingService.AddLog("⚠ Not connected!", LogLevel.Warning);
            return;
        }
        
        _loggingService.AddLog("🔌 Disconnecting...");
        _statsTimer.Stop();
        await _webSocketService.DisconnectAsync();
    }
    
    private void ClearLogs()
    {
        _loggingService.ClearLogs();
        TotalMessages = 0;
        MotionEvents = 0;
        _loggingService.AddLog("🗑️ Logs cleared", LogLevel.Info);
    }

    #endregion

    #region Event Handlers
    
    private void OnMessageReceived(object? sender, string message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                var alertMessage = JsonSerializer.Deserialize<MotionAlertMessage>(message);

                if (alertMessage?.Type == "motion_alert" && alertMessage.Data != null)
                {
                    TotalMessages++;
                    MotionEvents++;

                    var data = alertMessage.Data;
                    var location = !string.IsNullOrEmpty(data.Location) && data.Location != "unknown"
                        ? $" at {data.Location}"
                        : "";

                    _loggingService.AddLog(
                        $" Motion detected! Sensor: {data.SensorId}{location}",
                        LogLevel.Motion,
                        isMotionEvent: true,
                        sensorId: data.SensorId,
                        location: data.Location);

                    _loggingService.AddLog(
                        $" Event Type: {data.EventType} | Time: {data.DetectedAt:HH:mm:ss}",
                        LogLevel.Info);
                }
                else
                {
                    TotalMessages++;
                    _loggingService.AddLog(message, LogLevel.Info);
                }
            }
            catch
            {
                TotalMessages++;
                _loggingService.AddLog(message, LogLevel.Info);
            }
        });
    }
    
    private void OnConnectionStateChanged(object? sender, ConnectionStatus status)
    {
        Dispatcher.UIThread.Post(() =>
        {
            ConnectionStatus = status;
            IsConnected = status == ConnectionStatus.Connected;

            var (statusMessage, logLevel) = status switch
            {
                ConnectionStatus.Connected => (" sukses terhubung dengan server", LogLevel.Success),
                ConnectionStatus.Connecting => (" mencoba koneksi...", LogLevel.Info),
                ConnectionStatus.Disconnected => (" terputus dari server", LogLevel.Warning),
                ConnectionStatus.Error => (" error dalam koneksi", LogLevel.Error),
                _ => (" Unknown status", LogLevel.Warning)
            };

            _loggingService.AddLog(statusMessage, logLevel);
            if (status == ConnectionStatus.Connected)
            {
                _ = FetchDashboardStatsAsync();
                _statsTimer.Start();
            }
            else
            {
                _statsTimer.Stop();
            }
        });
    }
    private async Task FetchDashboardStatsAsync()
    {
        try
        {
            var stats = await _apiService.GetDashboardStatsAsync();
            if (stats != null)
            {
                ConnectedClients = stats.ConnectedClients;
                ActiveSensors = stats.ActiveSensors;
                EventsLast24h = stats.EventsLast24h;

                _loggingService.AddLog(
                    $" Server Stats since last connection- Total: {stats.TotalEvents} | 24h: {stats.EventsLast24h} | " +
                    $"Hour: {stats.EventsLastHour} | Sensors: {stats.ActiveSensors} | Clients: {stats.ConnectedClients}",
                    LogLevel.Info);
            }
        }
        catch (Exception ex)
        {
            _loggingService.AddLog($"⚠ Failed to fetch server stats: {ex.Message}", LogLevel.Warning);
        }
    }

    #endregion
    
    public void Dispose()
    {
        _statsTimer.Stop();
        _webSocketService.MessageReceived -= OnMessageReceived;
        _webSocketService.ConnectionStateChanged -= OnConnectionStateChanged;
        _webSocketService.Dispose();
        _apiService.Dispose();
    }
}

// Simple command implementations to avoid ReactiveUI threading issues
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public class AsyncRelayCommand : ICommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;
    private bool _isExecuting;

    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => !_isExecuting && (_canExecute?.Invoke() ?? true);

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
            return;

        _isExecuting = true;
        RaiseCanExecuteChanged();

        try
        {
            await _execute();
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}