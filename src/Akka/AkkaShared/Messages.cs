namespace AkkaShared;

public class RegisterStation
{
    public string StationId { get; }
    public string Location { get; }

    public RegisterStation(string stationId, string location)
    {
        StationId = stationId;
        Location = location;
    }
}

public class ReportWeatherReading
{
    public string StationId { get; }
    public float Temperature { get; }
    public float Pressure { get; }

    public ReportWeatherReading(string stationId, float temperature, float pressure)
    {
        StationId = stationId;
        Temperature = temperature;
        Pressure = pressure;
    }
}

public class ReportStationMalfunction
{
    public string StationId { get; }

    public ReportStationMalfunction(string stationId)
    {
        StationId = stationId;
    }
}