
using System.Runtime.Serialization;
using Shared;

// Dapr Serialization: https://docs.dapr.io/developing-applications/sdks/dotnet/dotnet-actors/dotnet-actors-serialization/

namespace DaprShared;

public interface IWeatherStationEvent : IMessage { }

[DataContract]
public class StationStarted : IWeatherStationEvent
{
    public string StationId { get; set; }
    public string Location { get; set;}

    public StationStarted(string stationId, string location)
    {
        StationId = stationId;
        Location = location;
    }
}

[DataContract]
public class StationRestarted: IWeatherStationEvent { }

[DataContract]
public class WeatherReadingDetected : IWeatherStationEvent
{
    public float Temperature { get; set; }
    public float Pressure { get; set; }

    public WeatherReadingDetected(float temperature, float pressure)
    {
        Temperature = temperature;
        Pressure = pressure;
    }
}

[DataContract]
public class StationMalfunctionDetected : IWeatherStationEvent { }