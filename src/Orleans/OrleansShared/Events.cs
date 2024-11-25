
namespace OrleansShared;

public interface IWeatherStationEvent { }

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

public class StationRestarted: IWeatherStationEvent { }

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

public class StationMalfunctionDetected : IWeatherStationEvent { }