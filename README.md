# IoT Motion Monitor with Linux program support
important: This project use only dotnet 9.0

to install .NET in Ubuntu/MintOS:

``` 
    sudo apt update
    sudo apt install dotnet-sdk-9.0
```

Install .NET in Fedora:
```
    sudo dnf install dotnet-sdk-9.0
```
Install .NET in Arch:
```
sudo pacman -S dotnet-sdk-9.0
```

Then refer to the readme in `/homemonitor` for the frontend app, and `/backend` for the backend setup.

# ESP32 Codebase

Use the Arduino IDE (or similar) to connect to your ESP device. Connect the sensors and use `PIR_sensor.ino`. Update the backend IP address in the sketch before uploading.

---

Running the backend easily
-------------------------

A small helper script is included at the repo root to run the backend with `ASPNETCORE_URLS` already set.

From the repository root:

```bash
# make executable once
chmod +x ./run-backend.sh

# run on default port 5000
./run-backend.sh

# or supply a port, e.g. 8080
./run-backend.sh 8080
```

This runs `dotnet run --project backend/Backend.csproj` with `ASPNETCORE_URLS` configured. You can still run the backend manually from the `backend` folder if you prefer:

```bash
cd backend
ASPNETCORE_URLS="http://0.0.0.0:5000" dotnet run
```