Collector

```powershell
dapr run --app-id collector --app-port 5296 --dapr-http-port 52960 --dapr-grpc-port 52961 --metrics-port 52962 --resources-path ./resources -- dotnet run --project ./DaprWeatherCollector/DaprWeatherCollector.csproj
```

```powershell
dapr stop --app-id collector 
(lsof -iTCP -sTCP:LISTEN -P | grep :52960) | awk '{print $2}' | xargs  kill
```

Station

```powershell
dapr run --app-id station --app-port 5297 --dapr-http-port 52970 --dapr-grpc-port 52971 --metrics-port 52972 --resources-path ./resources -- dotnet run --project ./DaprWeatherStation/DaprWeatherStation.csproj
```

```powershell
dapr stop --app-id station
(lsof -iTCP -sTCP:LISTEN -P | grep :52970) | awk '{print $2}' | xargs  kill
```

how to debug:

https://docs.dapr.io/developing-applications/local-development/ides/vscode/vscode-how-to-debug-multiple-dapr-apps