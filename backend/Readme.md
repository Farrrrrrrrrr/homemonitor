# IoT Motion Detection Backend

Simple backend server for ESP32 motion sensors.

## Quick Start

```bash
# Run the backend server
ASPNETCORE_URLS="http://0.0.0.0:5000" dotnet run
```
or
```bash
ASPNETCORE_URLS="http://0.0.0.0:5000" ~/.dotnet/dotnet run --project Backend.csproj
```
accessed on this backend folder
## Access

- **Web Dashboard**: http://localhost:5000
- **API**: http://localhost:5000/api/motion-events
- **For ESP32**: Use your computer's IP address (e.g., `http://192.168.1.1:5000`)

## Find Your IP

```bash
# macOS/Linux
ifconfig | grep "inet " | grep -v 127.0.0.1

# Windows
ipconfig
```

## ESP32 Setup

Configure your ESP32 with your computer's IP:
```cpp
const char* serverHost = "192.168.1.1";  // Replace with your IP
```