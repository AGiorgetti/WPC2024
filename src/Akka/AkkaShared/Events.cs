namespace AkkaShared;

public interface IWeatherStationEvent { }

public class StationStarted : IWeatherStationEvent
{
    public string StationId { get; }
    public string Location { get; }

    public StationStarted(string stationId, string location)
    {
        StationId = stationId;
        Location = location;
    }
}

public class StationRestarted: IWeatherStationEvent { }

public class WeatherReadingDetected : IWeatherStationEvent
{
    public float Temperature { get; }
    public float Pressure { get; }

    public WeatherReadingDetected(float temperature, float pressure)
    {
        Temperature = temperature;
        Pressure = pressure;
    }
}

public class StationMalfunctionDetected : IWeatherStationEvent { }
