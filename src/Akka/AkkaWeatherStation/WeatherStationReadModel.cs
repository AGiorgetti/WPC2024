using AkkaShared;

public class WeatherStationReadModel
{
    public required string Id { get; set; }
    public required string StationId { get; set; }
    public required string Location { get; set; }
    public List<WeatherReadingDetected> Readings { get; set; } = new List<WeatherReadingDetected>();
    public bool IsMalfunctioning { get; set; } = false;
}