using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using homemonitor.Models;

namespace homemonitor.Services;

public class WebSocketService : IDisposable
{
    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isManualDisconnect;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    private const int InitialReconnectDelayMs = 2000;
    private const int MaxReconnectDelayMs = 30000;
    private int _currentReconnectDelay = InitialReconnectDelayMs;
    
    public ConnectionStatus Status { get; private set; } = ConnectionStatus.Disconnected;

    public string? CurrentUrl { get; private set; }

    public event EventHandler<string>? MessageReceived;

    public event EventHandler<ConnectionStatus>? ConnectionStateChanged;

    public async Task ConnectAsync(string url)
    {
        await _connectionLock.WaitAsync();
        try
        {
            if (_webSocket != null)
            {
                await DisconnectInternalAsync();
            }

            CurrentUrl = url;
            _isManualDisconnect = false;
            _cancellationTokenSource = new CancellationTokenSource();

            UpdateStatus(ConnectionStatus.Connecting);
            _webSocket = new ClientWebSocket();
            _webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
            await _webSocket.ConnectAsync(new Uri(url), _cancellationTokenSource.Token);

            UpdateStatus(ConnectionStatus.Connected);
            _currentReconnectDelay = InitialReconnectDelayMs;
            _ = Task.Run(() => ReceiveLoop(_cancellationTokenSource.Token));

        }
        catch (Exception ex)
        {
            UpdateStatus(ConnectionStatus.Error);
            OnMessageReceived($"Connection error: {ex.Message}");
            if (!_isManualDisconnect)
            {
                _ = Task.Run(() => ReconnectLoop());
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }
    public async Task DisconnectAsync()
    {
        await _connectionLock.WaitAsync();
        try
        {
            _isManualDisconnect = true;
            await DisconnectInternalAsync();
            UpdateStatus(ConnectionStatus.Disconnected);
        }
        finally
        {
            _connectionLock.Release();
        }
    }
    
    private async Task DisconnectInternalAsync()
    {
        _cancellationTokenSource?.Cancel();

        if (_webSocket != null)
        {
            try
            {
                if (_webSocket.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Client disconnecting",
                        CancellationToken.None);
                }
            }
            catch
            {
            }
            finally
            {
                _webSocket.Dispose();
                _webSocket = null;
            }
        }

        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }
    private async Task ReceiveLoop(CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        var messageBuilder = new StringBuilder();

        try
        {
            while (_webSocket?.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await _webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Server closing",
                        CancellationToken.None);
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var chunk = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    messageBuilder.Append(chunk);
                    if (result.EndOfMessage)
                    {
                        var message = messageBuilder.ToString();
                        messageBuilder.Clear();
                        OnMessageReceived(message);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            OnMessageReceived($"Receive error: {ex.Message}");
            UpdateStatus(ConnectionStatus.Error);
        }
        
        if (!_isManualDisconnect && !cancellationToken.IsCancellationRequested)
        {
            UpdateStatus(ConnectionStatus.Disconnected);
            _ = Task.Run(() => ReconnectLoop());
        }
    }
    private async Task ReconnectLoop()
    {
        while (!_isManualDisconnect && Status != ConnectionStatus.Connected)
        {
            OnMessageReceived($"🔄 Reconnecting in {_currentReconnectDelay / 1000} seconds...");
            await Task.Delay(_currentReconnectDelay);

            if (_isManualDisconnect)
                break;

            try
            {
                if (!string.IsNullOrEmpty(CurrentUrl))
                {
                    await ConnectAsync(CurrentUrl);

                    if (Status == ConnectionStatus.Connected)
                    {
                        OnMessageReceived("✅ Reconnected successfully!");
                        break;
                    }
                }
            }
            catch
            {
            }
            _currentReconnectDelay = Math.Min(_currentReconnectDelay * 2, MaxReconnectDelayMs);
        }
    }
    private void UpdateStatus(ConnectionStatus newStatus)
    {
        if (Status != newStatus)
        {
            Status = newStatus;
            ConnectionStateChanged?.Invoke(this, newStatus);
        }
    }
    private void OnMessageReceived(string message)
    {
        MessageReceived?.Invoke(this, message);
    }
    public void Dispose()
    {
        _isManualDisconnect = true;
        _cancellationTokenSource?.Cancel();
        _webSocket?.Dispose();
        _cancellationTokenSource?.Dispose();
        _connectionLock.Dispose();
    }
}