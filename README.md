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

then refer to the readme in /homemonitor for the frontend app, and /backend for the backend set up

#ESP32 Codebase

use arduino IDE or other to connect to your esp IoT device, connect the censors and use the file PIR_sensor.ino file, adjust the file to your configuration (check your backend's IP Address first then you can adjust it in the arduino IDE, verify it, then compile and deploy it!)